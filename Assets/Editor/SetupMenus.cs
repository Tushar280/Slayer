#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SetupMenus
{
    private const string GameScenePath = "Assets/Scenes/Main Scene.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

    // Picked from Assets/Skills/icons_full - swap these to use different art.
    private const string FireIconPath = "Assets/Skills/icons_full/103.png"; // orange / flame
    private const string TailIconPath = "Assets/Skills/icons_full/1012.png"; // green / claw sweep
    private const string FlyIconPath = "Assets/Skills/icons_full/1013.png"; // blue / sky dive

    [MenuItem("Tools/Setup Menus")]
    public static void Run()
    {
        CreateMainMenuScene();
        SetupGameScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log("SetupMenus complete.");
    }

    // ---------------------------------------------------------------
    // Main Menu scene
    // ---------------------------------------------------------------
    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var canvas = CreateCanvas("Canvas");
        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();

        var title = MakeText(canvas.transform, "Title", "DRAGON SLAYER", 64, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(800, 120));
        title.color = new Color(1f, 0.85f, 0.3f);

        var playBtn = MakeButton(canvas.transform, "Play Button", "PLAY", new Vector2(0.5f, 0.5f), new Vector2(260, 70));
        UnityEventTools.AddPersistentListener(playBtn.onClick, menu.PlayGame);

        var quitBtn = MakeButton(canvas.transform, "Quit Button", "QUIT", new Vector2(0.5f, 0.38f), new Vector2(260, 70));
        UnityEventTools.AddPersistentListener(quitBtn.onClick, menu.QuitGame);

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvas.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.transform.SetAsFirstSibling();

        EnsureEventSystem();

        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ---------------------------------------------------------------
    // Game scene
    // ---------------------------------------------------------------
    private static void SetupGameScene()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        var canvas = FindCanvas();

        // Remove previously created panels so re-running the setup stays clean
        foreach (var name in new[] { "WinPanel", "LosePanel", "PausePanel", "AbilityBar", "ControlsHint" })
        {
            var t = canvas.transform.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        // GameManager (create if missing)
        var gmObj = GameObject.Find("GameManager");
        if (gmObj == null) gmObj = new GameObject("GameManager");
        var gameManager = gmObj.GetComponent<GameManager>();
        if (gameManager == null) gameManager = gmObj.AddComponent<GameManager>();
        var pauseMenu = gmObj.GetComponent<PauseMenu>();
        if (pauseMenu == null) pauseMenu = gmObj.AddComponent<PauseMenu>();

        // Panels
        var winPanel = CreatePanel(canvas.transform, "WinPanel", new Color(0f, 0.35f, 0f, 0.75f));
        var losePanel = CreatePanel(canvas.transform, "LosePanel", new Color(0.4f, 0f, 0f, 0.75f));
        var pausePanel = CreatePanel(canvas.transform, "PausePanel", new Color(0f, 0f, 0f, 0.7f));

        var winText = MakeText(winPanel.transform, "WinText", "VICTORY!", 56, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(700, 120));
        var loseText = MakeText(losePanel.transform, "LoseText", "DEFEAT!", 56, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(700, 120));
        MakeText(pausePanel.transform, "PauseText", "PAUSED", 64, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(700, 120));

        var winRestart = MakeButton(winPanel.transform, "RestartButton", "RESTART", new Vector2(0.5f, 0.45f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(winRestart.onClick, gameManager.RestartBattle);
        var winMenu = MakeButton(winPanel.transform, "MenuButton", "MAIN MENU", new Vector2(0.5f, 0.33f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(winMenu.onClick, gameManager.LoadMainMenu);

        var loseRestart = MakeButton(losePanel.transform, "RestartButton", "RESTART", new Vector2(0.5f, 0.45f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(loseRestart.onClick, gameManager.RestartBattle);
        var loseMenu = MakeButton(losePanel.transform, "MenuButton", "MAIN MENU", new Vector2(0.5f, 0.33f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(loseMenu.onClick, gameManager.LoadMainMenu);

        var resumeBtn = MakeButton(pausePanel.transform, "ResumeButton", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(resumeBtn.onClick, pauseMenu.Resume);
        var pauseRestart = MakeButton(pausePanel.transform, "RestartButton", "RESTART", new Vector2(0.5f, 0.38f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(pauseRestart.onClick, pauseMenu.Restart);
        var pauseMenuBtn = MakeButton(pausePanel.transform, "MenuButton", "MAIN MENU", new Vector2(0.5f, 0.26f), new Vector2(260, 64));
        UnityEventTools.AddPersistentListener(pauseMenuBtn.onClick, pauseMenu.LoadMainMenu);

        winPanel.SetActive(false);
        losePanel.SetActive(false);
        pausePanel.SetActive(false);

        // Wire serialized fields
        var so = new SerializedObject(gameManager);
        so.FindProperty("winPanel").objectReferenceValue = winPanel;
        so.FindProperty("losePanel").objectReferenceValue = losePanel;
        so.FindProperty("winText").objectReferenceValue = winText;
        so.FindProperty("loseText").objectReferenceValue = loseText;
        so.FindProperty("player").objectReferenceValue = Object.FindAnyObjectByType<PlayerHealth>();
        so.FindProperty("enemy").objectReferenceValue = Object.FindAnyObjectByType<EnemyHealth>();
        so.ApplyModifiedProperties();

        var soPause = new SerializedObject(pauseMenu);
        soPause.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        soPause.ApplyModifiedProperties();

        // Hook up hit feedback + balance health
        var playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        var enemyHealth = Object.FindAnyObjectByType<EnemyHealth>();
        if (playerHealth != null)
        {
            if (playerHealth.GetComponent<HitFeedback>() == null)
                playerHealth.gameObject.AddComponent<HitFeedback>();
            playerHealth.maxHealth = 1000;

            var pcc = playerHealth.GetComponent<CharacterController>();
            if (pcc != null)
            {
                pcc.height = 3f;
                pcc.radius = 1f;
                pcc.center = new Vector3(0, 1.5f, 0);
            }

            TuneAbilities(playerHealth.GetComponent<DragonCombat>(), player: true);
        }
        if (enemyHealth != null)
        {
            if (enemyHealth.GetComponent<HitFeedback>() == null)
                enemyHealth.gameObject.AddComponent<HitFeedback>();
            enemyHealth.maxHealth = 1000;

            // The AI now moves with the transform, so a CharacterController is no longer needed -
            // and two of them shoving each other was what pushed the player around.
            var ecc = enemyHealth.GetComponent<CharacterController>();
            if (ecc != null) Object.DestroyImmediate(ecc);

            // Trim the long hitbox so the player can actually walk into melee range
            var enemyBox = enemyHealth.GetComponent<BoxCollider>();
            if (enemyBox != null)
            {
                enemyBox.size = new Vector3(3.73f, 4.01f, 4.5f);
                enemyBox.center = new Vector3(0f, 2.19f, 0f);
            }

            var ai = enemyHealth.GetComponent<EnemyDragonAI>();
            if (ai != null)
            {
                var aiSo = new SerializedObject(ai);
                SetFloat(aiSo, "moveSpeed", 2.5f);
                SetFloat(aiSo, "stopDistance", 6f);
                SetFloat(aiSo, "minDistance", 4.5f);
                SetFloat(aiSo, "decisionInterval", 2f);
                SetFloat(aiSo, "recoverDuration", 1.2f);
                aiSo.ApplyModifiedProperties();
            }

            TuneAbilities(enemyHealth.GetComponent<DragonCombat>(), player: false);
        }

        // Ability HUD with cooldown timers and key hints
        NormalizeCanvas(canvas);
        if (playerHealth != null) BuildAbilityHud(canvas, playerHealth.GetComponent<DragonCombat>());
        LayoutHealthbar(playerHealth, true);
        LayoutHealthbar(enemyHealth, false);

        // Attack hitboxes - the attack clips fire EnableHitboxEvent / DisableHitboxEvent
        if (playerHealth != null) SetupHitbox(playerHealth.gameObject);
        if (enemyHealth != null) SetupHitbox(enemyHealth.gameObject);

        RemoveStrayAnimators();

        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Creates the trigger volume in front of a dragon plus the component that receives the
    /// EnableHitboxEvent / DisableHitboxEvent animation events baked into the attack clips.
    /// </summary>
    private static void SetupHitbox(GameObject dragonRoot)
    {
        const string boxName = "AttackHitbox";

        var animator = dragonRoot.GetComponentInChildren<Animator>(includeInactive: true);
        if (animator == null)
        {
            Debug.LogWarning("SetupMenus: " + dragonRoot.name + " has no Animator, skipping hitbox");
            return;
        }

        // Unity delivers animation events only to components next to the Animator,
        // so the receiver lives on the model while the collider sits unscaled on the root.
        var receiver = animator.gameObject;

        var existing = dragonRoot.transform.Find(boxName);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Older versions put the receiver on the root, where animation events never reached it
        var staleReceiver = dragonRoot.GetComponent<AnimationHitbox>();
        if (staleReceiver != null) Object.DestroyImmediate(staleReceiver);

        var boxGo = new GameObject(boxName);
        boxGo.transform.SetParent(dragonRoot.transform, false);
        boxGo.transform.localPosition = new Vector3(0f, 1.8f, 2.8f);

        var box = boxGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.enabled = false;
        box.size = new Vector3(4f, 4f, 5f);

        var hitbox = receiver.GetComponent<AnimationHitbox>();
        if (hitbox == null) hitbox = receiver.AddComponent<AnimationHitbox>();

        var so = new SerializedObject(hitbox);
        so.FindProperty("hitbox").objectReferenceValue = box;
        so.FindProperty("owner").objectReferenceValue = dragonRoot.GetComponent<DragonHealth>();
        so.ApplyModifiedProperties();

        // Let DragonCombat drive the hitbox
        var combat = dragonRoot.GetComponent<DragonCombat>();
        if (combat != null)
        {
            var combatSo = new SerializedObject(combat);
            var hitboxProperty = combatSo.FindProperty("hitbox");
            if (hitboxProperty != null) hitboxProperty.objectReferenceValue = hitbox;
            combatSo.ApplyModifiedProperties();
        }
    }

    /// <summary>Pins the UI to a 16:9 reference resolution so the HUD layout is predictable.</summary>
    private static void NormalizeCanvas(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    /// <summary>Positions a dragon's health bar in a screen corner and syncs the slider range.</summary>
    private static void LayoutHealthbar(DragonHealth health, bool topLeft)
    {
        if (health == null) return;

        var healthSo = new SerializedObject(health);
        var barProperty = healthSo.FindProperty("healthBar");
        var healthbar = barProperty != null ? barProperty.objectReferenceValue as Healthbar : null;
        if (healthbar == null) return;

        var rt = healthbar.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(430f, 48f);
        rt.anchorMin = rt.anchorMax = topLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rt.pivot = topLeft ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rt.anchoredPosition = topLeft ? new Vector2(40f, -36f) : new Vector2(-40f, -36f);

        var barSo = new SerializedObject(healthbar);
        var sliderProperty = barSo.FindProperty("slider");
        var slider = sliderProperty != null ? sliderProperty.objectReferenceValue as Slider : null;
        if (slider == null) slider = healthbar.GetComponent<Slider>();
        if (slider == null) return;

        sliderProperty.objectReferenceValue = slider;
        barSo.ApplyModifiedProperties();

        slider.minValue = 0f;
        slider.maxValue = health.maxHealth;
        slider.value = health.maxHealth;
        slider.interactable = false;
        slider.targetGraphic = null;

        if (slider.fillRect != null)
        {
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill != null) fill.color = topLeft ? new Color(0.3f, 0.85f, 0.35f) : new Color(0.9f, 0.25f, 0.2f);
        }
    }

    /// <summary>
    /// Applies the combat balance. The boss hits softer and slower than the player.
    /// </summary>
    private static void TuneAbilities(DragonCombat combat, bool player)
    {
        if (combat == null) return;

        var so = new SerializedObject(combat);

        SetFloat(so, "damageMultiplier", player ? 1f : 0.6f);

        // Ability stats (damage / cooldown / range / animator trigger)
        SetAbility(so, "fireAttack", player ? 60 : 40, player ? 4f : 5f, player ? 12f : 14f);
        SetAbility(so, "tailAttack", player ? 35 : 25, player ? 2.5f : 3.5f, player ? 6f : 6f);
        SetAbility(so, "flyAttack", player ? 90 : 70, player ? 8f : 10f, player ? 9f : 10f);

        so.ApplyModifiedProperties();
    }

    private static void SetAbility(SerializedObject so, string ability, int damage, float cooldown, float range)
    {
        SetInt(so, ability + ".damage", damage);
        SetFloat(so, ability + ".cooldown", cooldown);
        SetFloat(so, ability + ".range", range);
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var property = so.FindProperty(path);
        if (property != null) property.floatValue = value;
        else Debug.LogWarning("SetupMenus: missing float property '" + path + "'");
    }

    private static void SetInt(SerializedObject so, string path, int value)
    {
        var property = so.FindProperty(path);
        if (property != null) property.intValue = value;
        else Debug.LogWarning("SetupMenus: missing int property '" + path + "'");
    }

    // ---------------------------------------------------------------
    // Ability HUD
    // ---------------------------------------------------------------
    private static void BuildAbilityHud(Canvas canvas, DragonCombat owner)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var bar = new GameObject("AbilityBar", typeof(RectTransform));
        bar.transform.SetParent(canvas.transform, false);
        var barRt = bar.GetComponent<RectTransform>();
        barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0f);
        barRt.pivot = new Vector2(0.5f, 0f);
        barRt.anchoredPosition = new Vector2(0f, 24f);
        barRt.sizeDelta = new Vector2(600f, 200f);
        var barUi = bar.AddComponent<AbilityBar>();

        var fire = CreateAbilitySlot(barRt, font, "Slot_Fire", "FIRE ATTACK", "1", -200f, LoadIcon(FireIconPath));
        var tail = CreateAbilitySlot(barRt, font, "Slot_Tail", "TAIL ATTACK", "2", 0f, LoadIcon(TailIconPath));
        var fly = CreateAbilitySlot(barRt, font, "Slot_Fly", "FLY ATTACK", "3", 200f, LoadIcon(FlyIconPath));

        var barSo = new SerializedObject(barUi);
        barSo.FindProperty("owner").objectReferenceValue = owner;
        barSo.FindProperty("fireSlot").objectReferenceValue = fire;
        barSo.FindProperty("tailSlot").objectReferenceValue = tail;
        barSo.FindProperty("flySlot").objectReferenceValue = fly;
        barSo.FindProperty("fireIcon").objectReferenceValue = LoadIcon(FireIconPath);
        barSo.FindProperty("tailIcon").objectReferenceValue = LoadIcon(TailIconPath);
        barSo.FindProperty("flyIcon").objectReferenceValue = LoadIcon(FlyIconPath);
        barSo.ApplyModifiedProperties();

        // Control hint in the bottom-left corner
        var hint = MakeText(canvas.transform, "ControlsHint",
            "WASD  Move        SPACE  Jump\n1 / 2 / 3  Attacks        ESC  Pause",
            20, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(520f, 60f));
        hint.alignment = TextAnchor.LowerLeft;
        hint.color = new Color(1f, 1f, 1f, 0.8f);
        var hintRt = hint.GetComponent<RectTransform>();
        hintRt.pivot = Vector2.zero;
        hintRt.anchoredPosition = new Vector2(20f, 20f);
    }

    private static AbilitySlotUI CreateAbilitySlot(RectTransform parent, Font font, string name,
                                                   string label, string key, float xOffset, Sprite icon)
    {
        const float slotWidth = 140f;
        const float slotHeight = 176f;
        const float plateInset = 5f;

        var slot = new GameObject(name, typeof(RectTransform));
        slot.transform.SetParent(parent, false);
        var rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(xOffset, 0f);
        rt.sizeDelta = new Vector2(slotWidth, slotHeight);

        // The slot itself is the dark plate behind the icon
        var background = slot.AddComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.09f, 0.85f);
        background.raycastTarget = false;

        // Icon fills the plate, leaving the bottom strip for the caption
        var iconImg = NewImage(rt, "Icon", Color.white);
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        var iconRt = iconImg.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 1f);
        iconRt.anchorMax = Vector2.one;
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.offsetMin = new Vector2(plateInset, 0f);
        iconRt.offsetMax = new Vector2(-plateInset, -plateInset);
        iconRt.anchoredPosition = Vector2.zero;

        // Radial sweep that drains away while the skill is on cooldown
        var overlay = NewImage(rt, "CooldownOverlay", new Color(0f, 0f, 0f, 0.65f));
        var overlayRt = overlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = iconRt.anchorMin;
        overlayRt.anchorMax = iconRt.anchorMax;
        overlayRt.pivot = iconRt.pivot;
        overlayRt.offsetMin = iconRt.offsetMin;
        overlayRt.offsetMax = iconRt.offsetMax;
        overlayRt.anchoredPosition = Vector2.zero;
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = false;
        overlay.fillAmount = 0f;
        overlay.enabled = false;

        // Countdown in the middle of the icon
        var seconds = MakeText(rt, "CooldownText", "", 32, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(slotWidth, 40f));
        seconds.font = font;
        seconds.fontStyle = FontStyle.Bold;
        seconds.raycastTarget = false;
        seconds.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -72f);
        seconds.gameObject.SetActive(false);

        // Key hint in the top-left corner of the plate
        var keyText = MakeText(rt, "KeyLabel", key, 22, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(34f, 34f));
        keyText.font = font;
        keyText.fontStyle = FontStyle.Bold;
        keyText.alignment = TextAnchor.UpperLeft;
        keyText.color = new Color(1f, 0.85f, 0.3f);
        keyText.raycastTarget = false;
        var keyRt = keyText.GetComponent<RectTransform>();
        keyRt.pivot = new Vector2(0f, 1f);
        keyRt.anchoredPosition = new Vector2(10f, -10f);

        // Ability name underneath
        var nameText = MakeText(rt, "NameLabel", label, 17, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(slotWidth, 24f));
        nameText.font = font;
        nameText.color = new Color(1f, 1f, 1f, 0.85f);
        nameText.raycastTarget = false;
        nameText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 6f);

        return slot.AddComponent<AbilitySlotUI>();
    }

    private static Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    /// <summary>Loads a skill icon, flipping the texture import settings to Sprite if needed.</summary>
    private static Sprite LoadIcon(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
        {
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("SetupMenus: could not load skill icon at " + path);
        return sprite;
    }

    /// <summary>
    /// Deletes empty Animator components that were auto-added to the dragon roots.
    /// The real animator lives on the model child, so a second one on the root is dead weight.
    /// </summary>
    private static void RemoveStrayAnimators()
    {
        foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include))
        {
            if (animator.runtimeAnimatorController != null) continue;

            // Only remove it if a child already provides a working animator
            bool childHasWorkingAnimator = false;
            foreach (var child in animator.GetComponentsInChildren<Animator>(includeInactive: true))
            {
                if (child != animator && child.runtimeAnimatorController != null)
                {
                    childHasWorkingAnimator = true;
                    break;
                }
            }
            if (!childHasWorkingAnimator) continue;

            Object.DestroyImmediate(animator);
        }
    }

    // ---------------------------------------------------------------
    // Build settings
    // ---------------------------------------------------------------
    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------
    private static Canvas FindCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>();
        if (canvases.Length == 0)
        {
            CreateCanvas("Canvas");
            canvases = Object.FindObjectsByType<Canvas>();
        }
        return canvases[0];
    }

    private static Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        var existing = Object.FindAnyObjectByType<EventSystem>();
        if (existing != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        StretchFull(go.GetComponent<RectTransform>());
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Text MakeText(Transform parent, string name, string content, int size, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = (anchorMin + anchorMax) * 0.5f;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;

        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }

    private static Button MakeButton(Transform parent, string name, string caption, Vector2 anchor, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text = caption;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return button;
    }
}
#endif

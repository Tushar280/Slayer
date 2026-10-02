#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-off utility that rebuilds the dragon animator controllers so they expose the
/// parameters the gameplay scripts drive (IsWalking, attack triggers, GetHit, Die).
/// </summary>
public static class AnimatorFix
{
    public static void Run()
    {
        RebuildController("Assets/FourEvilDragonsPBR/Animators/NightmareCTRL.controller", knightmareMapping: true);
        RebuildController("Assets/FourEvilDragonsPBR/Animators/SouleaterCTRL.controller", knightmareMapping: false);
        AssetDatabase.SaveAssets();
        Debug.Log("AnimatorFix complete.");
    }

    private static void RebuildController(string path, bool knightmareMapping)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            Debug.LogError("Controller not found: " + path);
            return;
        }

        // Collect existing state -> clip mapping
        var clipByState = new Dictionary<string, AnimationClip>();
        foreach (var layer in controller.layers)
        {
            foreach (var cs in layer.stateMachine.states)
            {
                if (cs.state != null && cs.state.motion is AnimationClip clip && !string.IsNullOrEmpty(cs.state.name))
                {
                    clipByState[cs.state.name] = clip;
                }
            }
        }

        // Reset parameters and state machine
        while (controller.parameters.Length > 0)
        {
            controller.RemoveParameter(0);
        }
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("BasicAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ClawAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("HornAttack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("GetHit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
        foreach (var t in sm.entryTransitions) sm.RemoveEntryTransition(t);
        var existingStates = new List<AnimatorState>();
        foreach (var child in sm.states) existingStates.Add(child.state);
        foreach (var state in existingStates) sm.RemoveState(state);

        var idle = AddState(sm, clipByState, "idle01", "Idle");
        if (idle == null) idle = AddState(sm, clipByState, "Idle", "Idle");
        if (idle == null) idle = AddState(sm, clipByState, "Idle 2", "Idle");
        sm.defaultState = idle;

        AnimatorState walk = AddState(sm, clipByState, "walk", "Walk") ?? AddState(sm, clipByState, "Walk", "Walk");
        AnimatorState basic = AddState(sm, clipByState, "Basic Attack", "BasicAttackState");
        AnimatorState claw = knightmareMapping
            ? (AddState(sm, clipByState, "Claw Attack", "ClawAttackState") ?? AddState(sm, clipByState, "Horn Attack", "HornAttackState"))
            : (AddState(sm, clipByState, "Tail Attack", "TailAttackState") ?? AddState(sm, clipByState, "Fireball Shoot", "FireballShootState"));
        AnimatorState horn = knightmareMapping
            ? AddState(sm, clipByState, "Horn Attack", "HornAttackState")
            : AddState(sm, clipByState, "Fireball Shoot", "FireballShootState");
        AnimatorState gethit = AddState(sm, clipByState, "Get Hit", "GetHitState") ?? AddState(sm, clipByState, "getHit", "GetHitState");
        AnimatorState die = AddState(sm, clipByState, "Die", "DieState") ?? AddState(sm, clipByState, "die", "DieState");

        AddBoolTransition(idle, walk, "IsWalking", whenTrue: true);
        AddBoolTransition(walk, idle, "IsWalking", whenTrue: false);

        AddTriggerTransitions(idle, basic, "BasicAttack");
        AddTriggerTransitions(idle, claw, "ClawAttack");
        if (horn != null && horn != claw) AddTriggerTransitions(idle, horn, "HornAttack");
        if (walk != null)
        {
            AddTriggerTransitions(walk, basic, "BasicAttack");
            AddTriggerTransitions(walk, claw, "ClawAttack");
            if (horn != null && horn != claw) AddTriggerTransitions(walk, horn, "HornAttack");
        }

        foreach (var st in new[] { basic, claw, horn })
        {
            if (st != null && idle != null)
            {
                var back = st.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 0.85f;
                back.duration = 0.2f;
            }
        }

        if (gethit != null)
        {
            var fromAny = sm.AddAnyStateTransition(gethit);
            fromAny.AddCondition(AnimatorConditionMode.If, 0, "GetHit");
            fromAny.hasExitTime = false;
            fromAny.duration = 0.05f;
            if (idle != null)
            {
                var back = gethit.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 0.9f;
                back.duration = 0.2f;
            }
        }

        if (die != null)
        {
            var fromAny = sm.AddAnyStateTransition(die);
            fromAny.AddCondition(AnimatorConditionMode.If, 0, "Die");
            fromAny.hasExitTime = false;
            fromAny.duration = 0.05f;
        }

        EditorUtility.SetDirty(controller);
    }

    /// <summary>Finds a clip by name (case insensitive) and adds it as a new state.</summary>
    private static AnimatorState AddState(AnimatorStateMachine sm, Dictionary<string, AnimationClip> clips, string clipName, string stateName)
    {
        AnimationClip clip = null;
        foreach (var kv in clips)
        {
            if (kv.Key.IndexOf(clipName, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                kv.Value.name.IndexOf(clipName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                clip = kv.Value;
                break;
            }
        }
        if (clip == null) return null;

        var st = sm.AddState(stateName);
        st.motion = clip;
        return st;
    }

    /// <summary>Adds an IsWalking-style bool transition with no exit time.</summary>
    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool whenTrue)
    {
        if (from == null || to == null) return;
        var t = from.AddTransition(to);
        t.AddCondition(whenTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
        t.hasExitTime = false;
        t.duration = 0.15f;
    }

    private static void AddTriggerTransitions(AnimatorState from, AnimatorState to, string trigger)
    {
        if (from == null || to == null) return;
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false;
        t.duration = 0.1f;
    }
}
#endif

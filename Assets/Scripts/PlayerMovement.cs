using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the player's input actions and moves the dragon with a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 0.2f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector2 moveInput;
    private bool isWalking;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && controller != null && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void Update()
    {
        if (controller == null) return;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Only rotate while actually moving (avoids rotating toward Vector3.zero)
        if (move.sqrMagnitude > 0.001f)
        {
            transform.forward = Vector3.Slerp(transform.forward, move.normalized, rotationSpeed);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        isWalking = move.sqrMagnitude > 0.001f;
    }

    public bool IsWalking() => isWalking;
}
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpForce = 1.5f;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        isGrounded = IsGrounded();

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 moveInput = ReadMoveInput();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);

        if (WasJumpPressed() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    bool IsGrounded()
    {
        if (groundCheck != null && groundMask.value != 0)
        {
            return Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        return controller.isGrounded;
    }

    Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input += gamepad.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    bool WasJumpPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
    }
}

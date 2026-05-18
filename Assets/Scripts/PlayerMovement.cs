using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float sprintMultiplier = 1.5f;
    public float gravity = -9.81f;

    [Header("Jump")]
    public float jumpForce = 1.5f;
    public float coyoteTime = 0.5f;
    public float jumpBufferTime = 0.5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Ladder")]
    public LayerMask ladderMask;
    public float ladderCheckDistance = 0.9f;
    public float ladderClimbSpeed = 5f;
    public float ladderStickForce = 2f;
    public float ladderJumpBackForce = 3f;

    [Header("Wall Run")]
    public LayerMask wallMask;
    public float wallCheckDistance = 0.85f;
    public float wallRunSpeedMultiplier = 1.1f;
    public float wallRunGravity = -1f;
    public float wallRunMinForwardInput = 0.25f;
    public float wallRunMaxSideInput = 0.45f;
    public float wallJumpUpForce = 4f;
    public float wallJumpSideForce = 6f;
    public float wallLookAssistStrength = 5f;

    [Header("Wall Run Camera")]
    public float wallCameraTilt = 18f;
    public float wallRunFov = 100f;
    public float fovLerpSpeed = 8f;

    [Header("Camera Bob")]
    public Transform cameraBobTarget;
    public float walkBobAmount = 0.035f;
    public float walkBobSpeed = 7f;
    public float sprintBobAmount = 0.065f;
    public float sprintBobSpeed = 11f;
    public float wallRunBobAmount = 0.06f;
    public float wallRunBobSpeed = 14f;
    public float bobReturnSpeed = 10f;

    private CharacterController controller;
    private MouseLook mouseLook;
    private Camera playerCamera;
    private Vector3 velocity;
    private Vector3 externalVelocity;
    private Vector3 cameraStartLocalPosition;
    private Vector3 wallNormal;
    private Vector3 wallRunDirection;
    private Vector3 ladderNormal;
    private Collider currentLadder;
    private bool isGrounded;
    private bool isClimbing;
    private bool isWallRunning;
    private int wallSide;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float bobTimer;
    private float defaultFov;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        mouseLook = GetComponent<MouseLook>();
        playerCamera = GetComponentInChildren<Camera>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.freezeRotation = true;
        }

        if (cameraBobTarget == null)
        {
            cameraBobTarget = transform.Find("Camera pivot");
        }

        if (cameraBobTarget != null)
        {
            cameraStartLocalPosition = cameraBobTarget.localPosition;
        }

        if (playerCamera != null)
        {
            defaultFov = playerCamera.fieldOfView;
        }
    }

    void Update()
    {
        isGrounded = IsGrounded();
        UpdateJumpTimers();

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        Vector2 moveInput = ReadMoveInput();
        bool isSprinting = IsSprinting() && moveInput.y > 0.01f;

        bool hasLadder = TryFindLadder(out RaycastHit ladderHit);
        if (hasLadder && (isClimbing || Mathf.Abs(moveInput.y) > 0.01f))
        {
            currentLadder = ladderHit.collider;
            ladderNormal = ladderHit.normal;
            UpdateLadderClimb(moveInput);
        }
        else if (isClimbing && IsStillOnCurrentLadder())
        {
            UpdateLadderClimb(moveInput);
        }
        else
        {
            isClimbing = false;
            currentLadder = null;
            isWallRunning = TryGetWallRun(moveInput, isSprinting, out wallNormal, out wallRunDirection, out wallSide);

            if (isWallRunning)
            {
                UpdateWallRun(moveInput);
            }
            else
            {
                UpdateGroundAirMovement(moveInput, isSprinting);
            }
        }

        UpdateCameraEffects(moveInput, isSprinting);
    }

    void UpdateLadderClimb(Vector2 moveInput)
    {
        isClimbing = true;
        isWallRunning = false;
        velocity = Vector3.zero;
        externalVelocity = Vector3.zero;

        float climbInput = moveInput.y;
        if (currentLadder != null && climbInput > 0f && GetCharacterFeetY() >= currentLadder.bounds.max.y)
        {
            climbInput = 0f;
        }

        Vector3 climbMove = Vector3.up * climbInput * ladderClimbSpeed;
        Vector3 stickMove = -ladderNormal * ladderStickForce;
        controller.Move((climbMove + stickMove) * Time.deltaTime);

        if (currentLadder != null && GetCharacterFeetY() >= currentLadder.bounds.max.y)
        {
            isClimbing = false;
            currentLadder = null;
        }

        if (WasJumpPressed())
        {
            isClimbing = false;
            currentLadder = null;
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            externalVelocity = ladderNormal * ladderJumpBackForce;
            jumpBufferTimer = 0f;
        }
    }

    void UpdateGroundAirMovement(Vector2 moveInput, bool isSprinting)
    {
        float currentSpeed = isSprinting ? speed * sprintMultiplier : speed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move((move * currentSpeed + externalVelocity) * Time.deltaTime);
        externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, 18f * Time.deltaTime);

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateWallRun(Vector2 moveInput)
    {
        if (Vector3.Dot(wallRunDirection, transform.forward) < 0f)
        {
            wallRunDirection = -wallRunDirection;
        }

        controller.Move(wallRunDirection * speed * sprintMultiplier * wallRunSpeedMultiplier * Time.deltaTime);
        velocity.y = wallRunGravity;
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);

        if (jumpBufferTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(wallJumpUpForce * -2f * gravity);
            externalVelocity = wallNormal * wallJumpSideForce;
            jumpBufferTimer = 0f;
            isWallRunning = false;
        }

        ApplyWallLookAssist();
    }

    bool TryGetWallRun(Vector2 moveInput, bool isSprinting, out Vector3 normal, out Vector3 runDirection, out int side)
    {
        normal = Vector3.zero;
        runDirection = Vector3.zero;
        side = 0;

        if (isGrounded || !isSprinting || moveInput.y < wallRunMinForwardInput)
        {
            return false;
        }

        bool hasWall = TryFindWall(out RaycastHit hit, out side);
        if (!hasWall)
        {
            return false;
        }

        Vector3 desiredMove = transform.right * moveInput.x + transform.forward * moveInput.y;
        desiredMove.y = 0f;

        if (desiredMove.sqrMagnitude < 0.01f)
        {
            return false;
        }

        desiredMove.Normalize();
        normal = hit.normal;
        normal.y = 0f;
        normal.Normalize();

        float sideAmount = Mathf.Abs(Vector3.Dot(desiredMove, normal));
        if (sideAmount > wallRunMaxSideInput)
        {
            return false;
        }

        runDirection = Vector3.ProjectOnPlane(desiredMove, normal);
        runDirection.y = 0f;

        if (runDirection.sqrMagnitude < 0.01f)
        {
            return false;
        }

        runDirection.Normalize();
        return true;
    }

    bool TryFindWall(out RaycastHit bestHit, out int side)
    {
        Vector3 origin = transform.TransformPoint(controller.center);
        int mask = wallMask.value != 0 ? wallMask.value : Physics.DefaultRaycastLayers;

        bool rightHit = Physics.Raycast(origin, transform.right, out RaycastHit right, wallCheckDistance, mask, QueryTriggerInteraction.Ignore);
        bool leftHit = Physics.Raycast(origin, -transform.right, out RaycastHit left, wallCheckDistance, mask, QueryTriggerInteraction.Ignore);

        if (rightHit && (!leftHit || right.distance <= left.distance))
        {
            bestHit = right;
            side = 1;
            return true;
        }

        if (leftHit)
        {
            bestHit = left;
            side = -1;
            return true;
        }

        bestHit = default;
        side = 0;
        return false;
    }

    bool TryFindLadder(out RaycastHit hit)
    {
        hit = default;

        if (ladderMask.value == 0)
        {
            return false;
        }

        Vector3 origin = transform.TransformPoint(controller.center);
        return Physics.Raycast(origin, transform.forward, out hit, ladderCheckDistance, ladderMask, QueryTriggerInteraction.Ignore);
    }

    bool IsStillOnCurrentLadder()
    {
        if (currentLadder == null)
        {
            return false;
        }

        Bounds bounds = currentLadder.bounds;
        float feetY = GetCharacterFeetY();
        float headY = GetCharacterHeadY();

        if (headY < bounds.min.y || feetY > bounds.max.y)
        {
            return false;
        }

        Vector3 position = transform.position;
        float closestX = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        float closestZ = Mathf.Clamp(position.z, bounds.min.z, bounds.max.z);
        Vector2 playerXZ = new Vector2(position.x, position.z);
        Vector2 ladderXZ = new Vector2(closestX, closestZ);

        return Vector2.Distance(playerXZ, ladderXZ) <= ladderCheckDistance + controller.radius;
    }

    float GetCharacterFeetY()
    {
        return transform.position.y + controller.center.y - controller.height * 0.5f;
    }

    float GetCharacterHeadY()
    {
        return transform.position.y + controller.center.y + controller.height * 0.5f;
    }

    void ApplyWallLookAssist()
    {
        if (mouseLook == null)
        {
            return;
        }

        Vector3 currentForward = transform.forward;
        currentForward.y = 0f;
        currentForward.Normalize();

        float yawDelta = Vector3.SignedAngle(currentForward, wallRunDirection, Vector3.up);
        float assist = Mathf.Clamp(yawDelta, -wallLookAssistStrength, wallLookAssistStrength) * Time.deltaTime;
        mouseLook.AddYawAssist(assist);
    }

    void UpdateJumpTimers()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (WasJumpPressed())
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    void UpdateCameraEffects(Vector2 moveInput, bool isSprinting)
    {
        if (mouseLook != null)
        {
            float roll = isWallRunning ? -wallSide * wallCameraTilt : 0f;
            mouseLook.SetCameraRoll(roll);
        }

        if (playerCamera != null)
        {
            float targetFov = isWallRunning ? wallRunFov : defaultFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, fovLerpSpeed * Time.deltaTime);
        }

        UpdateCameraBob(moveInput, isSprinting);
    }

    void UpdateCameraBob(Vector2 moveInput, bool isSprinting)
    {
        if (cameraBobTarget == null)
        {
            return;
        }

        if (isWallRunning)
        {
            bobTimer += Time.deltaTime * wallRunBobSpeed;
            Vector3 wallBobOffset = new Vector3(Mathf.Sin(bobTimer) * wallRunBobAmount, Mathf.Cos(bobTimer * 2f) * wallRunBobAmount * 0.25f, 0f);
            cameraBobTarget.localPosition = cameraStartLocalPosition + wallBobOffset;
            return;
        }

        bool shouldBob = !isClimbing && isGrounded && moveInput.sqrMagnitude > 0.01f;
        if (!shouldBob)
        {
            bobTimer = 0f;
            cameraBobTarget.localPosition = Vector3.Lerp(
                cameraBobTarget.localPosition,
                cameraStartLocalPosition,
                bobReturnSpeed * Time.deltaTime);
            return;
        }

        float bobAmount = isSprinting ? sprintBobAmount : walkBobAmount;
        float bobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;

        bobTimer += Time.deltaTime * bobSpeed;
        Vector3 bobOffset = new Vector3(
            Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.45f,
            Mathf.Sin(bobTimer) * bobAmount,
            0f);

        cameraBobTarget.localPosition = cameraStartLocalPosition + bobOffset;
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

    bool IsSprinting()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        return (keyboard != null && keyboard.leftShiftKey.isPressed)
            || (gamepad != null && gamepad.leftStickButton.isPressed);
    }
}



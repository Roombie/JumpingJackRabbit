using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class JackRabbitController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float sprintSpeed = 14f;
    public float acceleration = 50f;

    [Header("Jump")]
    public float jumpForce = 10f;
    public int maxJumps = 2;

    [Header("Gravity")]
    public float gravityScale = 10f;
    public float fallMultiplier = 2f;
    public float maxFallSpeed = -20f;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Jump Buffer")]
    public float jumpBufferTime = 0.2f;

    [Header("Coyote Time")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter = 0f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform capsuleTransform;

    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerInput playerInput;
    private StateMachine stateMachine;

    private Vector2 moveInput;
    private bool jumpInput;
    private bool sprintInput;

    private bool jumpInputReleased = true;
    private float jumpBufferCounter = -1f;

    [HideInInspector] public int jumpsRemaining;
    private Vector3 velocity;
    private Vector3 gravityVelocity;

    private bool isGrounded;

    public Vector2 MoveInput => moveInput;
    public bool SprintInput => sprintInput;
    public bool JumpInput => jumpInput;
    public Vector3 CurrentVelocity => velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        stateMachine = new StateMachine();
        stateMachine.ChangeState(new IdleState(stateMachine, this));
    }

    private void Update()
    {
        stateMachine.Update();

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            jumpsRemaining = maxJumps;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;

            if (coyoteTimeCounter <= 0f && jumpsRemaining == maxJumps)
            {
                jumpsRemaining = maxJumps - 1;
            }
        }

        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        GroundCheck();
        ApplyGravity();
        characterController.Move(velocity * Time.fixedDeltaTime);
    }

    #region Input System
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && jumpInputReleased)
        {
            jumpInput = true;
            jumpInputReleased = false;
            jumpBufferCounter = jumpBufferTime;
        }

        if (context.canceled)
        {
            jumpInputReleased = true;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        sprintInput = context.ReadValueAsButton();
    }
    #endregion

    #region Jump Logic
    public void PerformJump()
    {
        float appliedJumpForce = jumpForce;

        // Reduce jump force for extra jumps
        if (!IsGrounded() && !IsInCoyoteTime())
        {
            int jumpsUsed = maxJumps - jumpsRemaining;
            if (jumpsUsed >= 1)
                appliedJumpForce *= 0.7f;
        }

        gravityVelocity.y = appliedJumpForce;
        velocity.y = gravityVelocity.y;

        Debug.Log($"Jump performed: appliedJumpForce={appliedJumpForce}, jumpsRemaining={jumpsRemaining}");

        if (IsGrounded() || IsInCoyoteTime())
        {
            jumpsRemaining = maxJumps - 1;
        }
        else
        {
            jumpsRemaining--;
        }

        coyoteTimeCounter = 0f;
    }

    public void ConsumeJumpInput()
    {
        jumpInput = false;
        jumpBufferCounter = -1f;
    }

    public bool HasBufferedJump()
    {
        return jumpBufferCounter > 0f;
    }

    public bool IsInCoyoteTime()
    {
        return coyoteTimeCounter > 0f;
    }

    public void ConsumeBufferedJump()
    {
        jumpBufferCounter = -1f;
        jumpInput = false;
    }
    #endregion

    #region Movement & Rotation
    /// <summary>
    /// Gets movement direction relative to camera orientation.
    /// </summary>
    public Vector3 GetMoveDirection()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera Transform not assigned!");
            return Vector3.zero;
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * moveInput.y + right * moveInput.x;

        return moveDir.normalized;
    }

    /// <summary>
    /// Rotates the player towards movement direction.
    /// </summary>
    public void RotateTowardsMoveDirection()
    {
        Vector3 moveDir = GetMoveDirection();

        if (moveDir.magnitude < 0.1f)
            return;

        moveDir.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);

        capsuleTransform.rotation = Quaternion.RotateTowards(
            capsuleTransform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Updates horizontal velocity based on movement.
    /// </summary>
    public void SetHorizontalVelocity(Vector3 horizontal)
    {
        velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
    }

    /// <summary>
    /// Applies gravity to vertical velocity.
    /// </summary>
    public void ApplyGravity()
    {
        if (isGrounded)
        {
            if (gravityVelocity.y <= 0)
            {
                gravityVelocity.y = 0;
                velocity.y = 0;
            }

            return;
        }

        float gravity = gravityScale;

        if (gravityVelocity.y < 0)
        {
            gravity *= fallMultiplier;
        }

        gravityVelocity.y -= gravity * Time.fixedDeltaTime;

        if (gravityVelocity.y < maxFallSpeed)
        {
            gravityVelocity.y = maxFallSpeed;
        }

        velocity.y = gravityVelocity.y;
    }
    #endregion

    #region Ground Check
    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            jumpsRemaining = maxJumps;

            if (!HasBufferedJump() && !JumpInput && gravityVelocity.y <= 0)
            {
                gravityVelocity.y = 0;
                velocity.y = 0;
            }
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }
    #endregion

    #region Debug
    public string GetCurrentStateName()
    {
        return stateMachine.GetCurrentStateName();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 moveDir = GetMoveDirection();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, moveDir * 2);

        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
    #endregion
}
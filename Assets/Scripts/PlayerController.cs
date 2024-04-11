using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    // [SerializeField] private float slowDownForce = 5f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpMultiplier = 1f;
    [SerializeField] private float jumpDelay = 0.25f;
    [SerializeField] private int jumpCount = 0;
    [SerializeField] private int maxJumpCount = 2;
    [Tooltip("How long I should buffer your jump input for (seconds)")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    [Tooltip("How long you have to jump after leaving a ledge (seconds)")]
    [SerializeField] private float coyoteTime = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float riseGravity = 0.25f;
    [SerializeField] private float fallMultiplier = 1.5f;

    [Header("Physics")]
    [SerializeField] private float linearDrag = 4f;
    // [SerializeField] private float runlinearDrag = 2f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private Vector3 boxCastSize = new(0.5f, 0.05f, 0.5f);

    private Vector2 currentMovementInput;
    public bool isJumping;
    public bool jumpButtonPressed = false;
    private bool isSprinting;

    private Rigidbody rb;
    private AudioSource audioSource;

    private bool inAirFromJump = false;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        Jump();
        CheckJumpBuffer();
        CheckCoyoteTime();
        jumpButtonPressed = false;
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
        ModifyPhysics();
    }

    private void HandleMovementInput()
    {
        Vector3 move = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;
        MovePlayer(move);
        RotatePlayer(move);
    }

    #region MoveLogic
    private void MovePlayer(Vector3 moveDirection)
    {
        Vector3 velocity = moveDirection * (isSprinting ? sprintSpeed : walkSpeed);
        velocity.y = rb.velocity.y;
        rb.velocity = velocity;
    }

    private void RotatePlayer(Vector3 moveDirection)
    {
        if (moveDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    #endregion

    private void Jump()
    {
        if ((jumpButtonPressed && coyoteTimeCounter > 0f && !inAirFromJump && !isJumping) || (jumpBufferCounter > 0f && IsGrounded()))
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce * jumpMultiplier, rb.velocity.z);
            isJumping = true;
            inAirFromJump = true;
        }
    }

    // Update the CheckCoyoteTime method to reset jumpTimer when grounded
    private void CheckCoyoteTime()
    {
        if (!IsGrounded()) // If the player isn't on the ground
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        else
        {
            coyoteTimeCounter = coyoteTime;
            inAirFromJump = false;
        }
    }

    private void CheckJumpBuffer()
    {
        if (jumpButtonPressed) // When the player initiates a jump
        {
            jumpBufferCounter = jumpBufferTime; // Set jump buffer counter when jump is initiated
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime; // Decrement jump buffer counter over time
        }
    }

    private void ModifyPhysics()
    {
        if (rb.velocity.y > 0 && isJumping) // Less or no gravity while jumping up
        {
            rb.velocity += riseGravity * Time.fixedDeltaTime * Vector3.up;
        }
        else // Normal gravity when jumping down
        {
            rb.velocity += fallMultiplier * Physics.gravity.y * Time.fixedDeltaTime * Vector3.up;
        }

        if (IsGrounded()) // When it's on the ground
        {
            if (currentMovementInput.magnitude < 0.4f) // And it's moving horizontally
            {
                rb.drag = linearDrag;
            }
            else
            {
                rb.drag = 0f;
            }
            rb.useGravity = false;
        }
        else // When it's on the air
        {
            rb.useGravity = true; // Enabling gravity when not grounded
            rb.drag = linearDrag * 0.15f;
            if (rb.velocity.y < 0)
            {
                rb.velocity += fallMultiplier * fallMultiplier * Time.fixedDeltaTime * Vector3.up; // Increasing fall speed
            }
            else if (rb.velocity.y > 0 && !isJumping)
            {
                rb.velocity += fallMultiplier * (fallMultiplier / 2) * Time.fixedDeltaTime * Vector3.up; // Applying half of the gravity
            }
        }
    }

    private bool IsGrounded()
    {
        return Physics.BoxCast(transform.position, boxCastSize, Vector3.down, Quaternion.identity, groundRayLength, groundLayer) && rb.velocity.y <= 0.1f;
    }

    #region Input
    public void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnJumpPressed();
        }
        if (context.canceled)
        {
            OnJumpReleased();
        }
    }

    public void OnJumpPressed()
    {
        if (!canJump) return;
        jumpButtonPressed = true;
    }

    public void OnJumpReleased()
    {
        isJumping = false;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnRunPressed(context);
        }
        if (context.canceled)
        {
            OnRunReleased();
        }
    }

    public void OnRunPressed(InputAction.CallbackContext context)
    {
        if (IsGrounded() && canSprint)
        {
            isSprinting = context.ReadValueAsButton();
        }
    }

    public void OnRunReleased()
    {
        isSprinting = false;
    }
    #endregion
}
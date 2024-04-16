using UnityEditor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody),typeof(CapsuleCollider),typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("Functional Options")]
    [Tooltip("Enables or disables sprinting functionality.")]
    [SerializeField] private bool canSprint = true;
    [Tooltip("Enables or disables jumping functionality.")]
    [SerializeField] private bool canJump = true;
    [Tooltip("Enables or disables crouching functionality.")]
    [SerializeField] private bool canCrouch = true;
    [Tooltip("Enables or disables wall jumping functionality.")]
    [SerializeField] private bool canWallJump = true;
    [Tooltip("Enables or disables wall sliding functionality.")]
    [SerializeField] private bool canWallSliding = true;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    // [SerializeField] private float slowDownForce = 5f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float jumpMultiplier = 1f;
    [SerializeField] private float lowjumpMultiplier = 1.25f;
    [SerializeField] private int maxJumpCount = 2;
    [Tooltip("How long I should buffer your jump input for (seconds)")]
    [SerializeField] private float jumpBufferTime = 0.125f;
    [Tooltip("How long you have to jump after leaving a ledge (seconds)")]
    [SerializeField] private float coyoteTime = 0.125f;
    // [SerializeField] private float terminalVelocity = 10f;
    // [SerializeField] private float airAcceleration = 5f;
    // [SerializeField] private float maxAirSpeed = 7f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeed = 2.5f;
    [Tooltip("Vertical offset between the collider's default center and crouching center.")]
    [SerializeField] private float crouchColOffset = -0.5f;
    [Tooltip("Height of the collider while crouching.")]
    [SerializeField] private float crouchColHeight = 0.5f;
    [Tooltip("Distance used for detecting obstacles when transitioning from standing to crouching.")]
    [SerializeField] private float crouchObstacleDetection = 0.25f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpForce = 8f;
    [SerializeField] private float wallJumpDetectionRange = 0.6f;

    [Header("Wall Sliding")]
    [SerializeField] private float wallSlidingSpeed = 2f;

    [Header("Gravity")]
    [SerializeField] private float riseGravity = 2.5f;
    [SerializeField] private float fallMultiplier = 3.5f;

    [Header("Physics")]
    [Tooltip("Slows down the player's movement. Higher values make the player stop faster.")]
    [SerializeField] private float linearDrag = 4f;
    // [SerializeField] private float runlinearDrag = 2f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private Vector3 boxCastSize = new(0.5f, 0.05f, 0.5f);

    private Vector2 currentMovementInput;
    private int extraJumpCount;
    private bool isJumping; // indicates whether the player is CURRENTLY in the PROCESS of JUMPING
    private bool jumpButtonPressed = false; // indicates whether the JUMP button is CURRENTLY PRESSED
    private bool isSprinting; // indicates whether the player is CURRENTLY in the PROCESS of SPRINTING
    private bool crouchPressed = false; // indicates whether the CROUCH button is CURRENTLY PRESSED
    private bool isCrouching = false; // indicates whether the player is CURRENTLY in the process of CROUCHING
    private float defaultStandingHeight;
    private bool isWallSliding;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private AudioSource audioSource;

    private bool inAirFromJump = false;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        audioSource = GetComponent<AudioSource>();

        defaultStandingHeight = capsuleCollider.height;
    }

    private void Update()
    {
        WallSlide();
        WallJump();
        Jump();
        CheckJumpBuffer();
        CheckCoyoteTime();
        Crouch();
        jumpButtonPressed = false;
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
        ModifyPhysics();
    }

    #region Collision Detection
    private bool IsGrounded()
    {
        return Physics.BoxCast(transform.position, boxCastSize, Vector3.down, Quaternion.identity, groundRayLength, groundLayer) && rb.velocity.y <= 0.1f;
    }

    private bool IsTouchingWall()
    {
        // Calculate the forward direction vector with y-component set to 0
        Vector3 forwardDirection = new Vector3(transform.forward.x, transform.forward.y, transform.forward.z).normalized;

        // Cast a ray in the direction the player is facing to check for wall contact
        if (Physics.Raycast(transform.position, forwardDirection, out RaycastHit hit, wallJumpDetectionRange, groundLayer))
        {
            Debug.Log("Wall detected");
            // Check if the hit surface is a wall
            return hit.normal.y < 0.1f; // Adjust this threshold as needed
        }
        return false;
    }
    #endregion

    #region Movement
    private void HandleMovementInput()
    {
        Vector3 move = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;
        MovePlayer(move);
        RotatePlayer(move);
    }

    private void MovePlayer(Vector3 moveDirection)
    {
        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);
        Vector3 velocity = moveDirection * speed;
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

    #region Jump
    private void Jump()
    {
        if (IsGrounded())
        {
            extraJumpCount = 0;
        }

        // JUMPING
        // If the jump button is pressed, there's still remaining coyote time, the player is not already in the air from a jump, and is not currently jumping, and not crouching
        // OR If there's a buffered jump input and the player is currently grounded and not crouching.
        if ((jumpButtonPressed && coyoteTimeCounter > 0f && !inAirFromJump && !isJumping && !isCrouching) || (jumpBufferCounter > 0f && IsGrounded() && !isCrouching)) {
            Debug.Log("Jump!");
            rb.velocity = new Vector3(rb.velocity.x, jumpForce * jumpMultiplier, rb.velocity.z); // Make a jump
            // Mark the player as jumping and in the air from a jump
            isJumping = true; 
            inAirFromJump = true;
        }
        // DOUBLE JUMPING OR EXTRA JUMP IF FALLING BEFORE JUMPING
        // Checks whether the jump button is pressed AND the player is not grounded (in the air), AND the player has not exceeded the maximum allowed extra jumps minus one.
        else if (jumpButtonPressed && !IsGrounded() && extraJumpCount < maxJumpCount - 1)
        {
            Debug.Log("Double jumping!");
            rb.velocity = new Vector3(rb.velocity.x, jumpForce * lowjumpMultiplier, rb.velocity.z); // Make a lower jump
            extraJumpCount++; // Iterate one to extraJumpCount, meaning you used one of your extra jumps
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
    #endregion

    #region Crouch
    private void Crouch()
    {
        if (!canCrouch) return;

        if (crouchPressed)
        {
            Debug.Log("Crouching started!");
            isCrouching = true;
            capsuleCollider.height = crouchColHeight;
            capsuleCollider.center = new Vector3(0f, crouchColOffset, 0f);
        }
        else
        {
            // Check if there's an obstruction above the player using a raycast
            if (Physics.Raycast(transform.position, Vector3.up, defaultStandingHeight - capsuleCollider.height + crouchObstacleDetection))
            {
                // If there's an obstruction, remain crouched
                Debug.Log("Obstruction detected above, remaining crouched!");
                isCrouching = true;
                return;
            }

            // If no obstruction, return to standing height
            Debug.Log("Not crouching!");
            isCrouching = false;
            capsuleCollider.height = defaultStandingHeight;
            capsuleCollider.center = Vector3.zero; // Assuming default center
        }
    }
    #endregion

    #region Wall Jump and Wall Sliding
    private void WallJump()
    {
        if (!canWallJump) return;
    }

    private void WallSlide()
    {
        if (!canWallSliding) return;

        if (IsTouchingWall() && !IsGrounded() && currentMovementInput.y != 0) {
            Debug.Log("Is wall sliding");
            isWallSliding = true;
            rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        } else {
            isWallSliding = false;
        } 
    }
    #endregion

    #region Physics
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
    #endregion 

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
        if (IsGrounded() && canSprint) // When the player is on the ground and canSprint is available
        {
            isSprinting = context.ReadValueAsButton();
        }
    }

    public void OnRunReleased()
    {
        isSprinting = false;
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCrouchPressed();
        }
        if (context.canceled)
        {
            OnCrouchReleased();
        }
    }

    public void OnCrouchPressed()
    {
        crouchPressed = true;
    }

    public void OnCrouchReleased()
    {
        crouchPressed = false;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        // Set color based on whether the player is grounded or not
        if (IsGrounded()) {
            Gizmos.color = Color.green; // Grounded
        } else {
            Gizmos.color = Color.red; // In the air
        }

        // Draw the boxcast
        Gizmos.DrawWireCube(transform.position + Vector3.down * groundRayLength, boxCastSize * 2);

        // Visualize crouchColOffset
        Gizmos.color = new Color(0, 0, 1, 0.5f);
        Vector3 crouchOffsetPos = transform.position + Vector3.up * crouchColOffset;
        Gizmos.DrawSphere(crouchOffsetPos, 0.1f);
        Gizmos.DrawLine(transform.position, crouchOffsetPos);

#if UNITY_EDITOR
        // Display label
        Handles.Label(crouchOffsetPos, "Crouch Offset");
#endif

        // Crouching Obstruction
        // If it detects both transform and capsule collider
        if (transform != null && capsuleCollider != null)
        {
            // Visualize the crouching obstruction raycast
            if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, defaultStandingHeight - capsuleCollider.height + crouchObstacleDetection))
            {
                Gizmos.color = Color.red; // Set color to red if there's an obstruction
                Gizmos.DrawLine(transform.position, hit.point); // Draw a line from player to the hit point
            }
            else
            {
                Gizmos.color = Color.green; // Set color to green if there's no obstruction
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * (defaultStandingHeight - capsuleCollider.height + crouchObstacleDetection)); // Draw a line from player upwards
            }
        }

        // Visualize touching wall
        Gizmos.color = new Color(0, 1, 1, 0.5f);
        Vector3 endPosition = transform.position + transform.forward * wallJumpDetectionRange;
        Gizmos.DrawLine(transform.position, endPosition); // Draw a line in the direction the player is facing

        // Draw a sphere at the end of the line to represent the detection point
        Gizmos.DrawSphere(endPosition, 0.1f);

        // Display label for wall detection
        Handles.Label(endPosition, "Wall Detection");
    }
    #endregion
}
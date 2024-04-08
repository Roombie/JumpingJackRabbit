using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;

    [Header("Player Input")]
    private Vector2 currentMovementInput;
    private bool isJumping;
    private bool isSprinting;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float rotationSpeed = 25f;
    private Rigidbody rb;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundRayLength = 0.1f;
    [SerializeField] private Vector3 boxCastSize = new Vector3(0.5f, 0.05f, 0.5f);
    private Color groundRayColor = Color.green;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 10f;
    [Tooltip("How long I should buffer your jump input for (seconds)")]
    [SerializeField] private float jumpBuffer = 0.1f;
    [Tooltip("How long you have to jump after leaving a ledge (seconds)")]
    private float jumpBufferCounter;
    [SerializeField] private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    [Header("Gravity")]
    [SerializeField] private float gravity = 12f;
    [SerializeField] private float fallMultiplier = 2.5f;

    [Header("Components")]
    public Animator animator;
    private AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Dynamically change ground ray color during runtime
        groundRayColor = IsGrounded() ? Color.green : Color.red;
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
        HandleJumpInput();
        CheckJumpBuffer();
        CheckCoyoteTime();

        // Faster fall curve
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void HandleMovementInput()
    {
        Vector3 move = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;
        MovePlayer(move);
        RotatePlayer(move);
    }

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

    private void HandleJumpInput()
    {
        // Check if the jump button is pressed
        if (isJumping)
        {
            // If the player is grounded or within coyote time, jump immediately
            if (IsGrounded() || coyoteTimeCounter > 0f)
            {
                Jump();
            }
            else
            {
                // Otherwise, start the jump buffer countdown
                jumpBufferCounter = jumpBuffer;
            }
        }
    }

    private void Jump()
    {
        // If the player is grounded or within coyote time, allow jumping
        if (IsGrounded() || coyoteTimeCounter > 0f)
        {
            // Set the y component of the velocity to the jump force
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);

            // Reset jump flag and counters
            isJumping = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
    }

    private void CheckJumpBuffer()
    {
        // If the player pressed the jump button recently and is still within the buffer time, jump
        if (jumpBufferCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f; // Reset the buffer counter after jumping
        }
        else
        {
            jumpBufferCounter = 0f; // Reset the buffer counter if the buffer time is over
        }
    }

    private void CheckCoyoteTime()
    {
        if (IsGrounded())
        {
            // Reset coyote time counter when the player is grounded
            coyoteTimeCounter = coyoteTime;
        }
        else if (coyoteTimeCounter > 0f)
        {
            // Decrement coyote time counter if not grounded but still within coyote time
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }


    private bool IsGrounded()
    {
        // Perform a boxcast or spherecast to check for ground
        bool isHit = Physics.BoxCast(transform.position, boxCastSize, Vector3.down, out RaycastHit hit, Quaternion.identity, groundRayLength, groundLayer);
        // bool isHit = Physics.SphereCast(transform.position, castRadius, Vector3.down, out RaycastHit hit, groundRayLength, groundLayer);

        return isHit;
    }


    #region Input
    // Method to handle player input for movement
    public void OnMove(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        Debug.Log("You're moving. Your input is: " + currentMovementInput);
    }

    // Method to handle player input for jumping
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isJumping = true;
            Debug.Log("Jump button pressed");
        }
        else if (context.canceled)
        {
            isJumping = false;
            Debug.Log("Jump button canceled");
        }
    }

    // Method to handle player input for sprinting
    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
        Debug.Log("You're sprinting now!");
    }
    #endregion

    private void OnDrawGizmos()
    {
        // Draw a box or sphere representing the cast
        Gizmos.color = groundRayColor;
        // Draw the box cast at the player's position
        Gizmos.DrawWireCube(transform.position - new Vector3(0, groundRayLength, 0), boxCastSize); // Adjust position for box cast
    }
}

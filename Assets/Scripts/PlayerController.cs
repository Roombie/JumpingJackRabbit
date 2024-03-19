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
    [SerializeField] private float groundRayLength = 0.1f;
    private Color groundRayColor = Color.green;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Gravity")]
    [SerializeField] private float gravity = 12f;

    [Header("Components")]
    public Animator animator;
    private AudioSource audioSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
        HandleJumpInput();

        // Dynamically change ground ray color during runtime
        groundRayColor = IsGrounded() ? Color.green : Color.red;
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
        if (canJump && isJumping)
        {
            Jump();
        }
    }

    private void Jump()
    {
        if (IsGrounded())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = false;
        }
    }

    private bool IsGrounded()
    {
        RaycastHit hit;
        bool isHit = Physics.Raycast(transform.position, Vector3.down, out hit, groundRayLength);

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
        // Draw a raycast from the player's position downwards with dynamically adjustable parameters
        Debug.DrawRay(transform.position, Vector3.down * groundRayLength, groundRayColor);
    }
}

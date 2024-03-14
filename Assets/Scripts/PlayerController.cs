using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;

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
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    private Vector3 moveDirection;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private Vector3 gravity = new(0f, -12f, 0f);

    [Header("Looking Parameters")]
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    [Header("Components")]
    public Animator animator;
    private CharacterController cController;
    private AudioSource audioSource;

    private void Awake()
    {
        cController = GetComponent<CharacterController>();
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
        if (CanMove)
        {
            // Handle movement input
            Vector3 move = new Vector3(currentMovementInput.x, 0f, currentMovementInput.y).normalized;

            // Rotate player based on movement direction
            if (move.magnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }

            // Apply speed based on movement direction
            moveDirection = move * (isSprinting ? sprintSpeed : walkSpeed);

            ApplyGravity();

            // Handle jumping
            if (canJump && isJumping && IsGrounded())
            {
                moveDirection.y = jumpForce;
            }

            // Move the character controller
            cController.Move(moveDirection * Time.deltaTime);
        }
    }

    private bool IsGrounded()
    {
        return cController.isGrounded; // Check if the player is touching the ground
    }

    private void ApplyGravity()
    {
        if (!IsGrounded()) // Apply gravity only if the player is not grounded
        {
            moveDirection.y += gravity.y * Time.deltaTime;
        }
        else if (moveDirection.y < 0) // Ensure that the y-component of moveDirection is zero when grounded to prevent accumulating gravity
        {
            moveDirection.y = 0f;
        }
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
}

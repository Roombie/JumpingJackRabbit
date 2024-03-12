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

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeed = 1.5f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 30f;

    [Header("Looking Parameters")]
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    [Header("Components")]
    public Animator animator;
    private CharacterController cController;
    private AudioSource audioSource;

    private Vector3 moveDirection;
    private Vector2 currentInput;
    [SerializeField] private float smoothTime = 0.05f;
    private float currentVelocity;
    private bool isJumping;
    private bool isSprinting;

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
            moveDirection = new Vector3(currentInput.x, 0f, currentInput.y);
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= isSprinting ? sprintSpeed : walkSpeed;

            // Apply gravity
            moveDirection.y -= gravity * Time.deltaTime;

            /*var targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentVelocity, smoothTime);
            transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);*/

            // Move the character controller
            cController.Move(moveDirection * Time.deltaTime);

            // Handle jumping
            if (canJump && isJumping)
            {
                moveDirection.y = jumpForce;
            }
        }
    }

    // Method to handle player input for movement
    public void OnMove(InputAction.CallbackContext context)
    {
        currentInput = context.ReadValue<Vector2>();
        Debug.Log("You're moving. Your input is: " + currentInput);
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
}

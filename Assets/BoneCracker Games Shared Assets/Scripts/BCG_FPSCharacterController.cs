using UnityEngine;
using UnityEngine.InputSystem; // Important for the new Input System

[RequireComponent(typeof(CharacterController))]
public class BCG_FPSCharacterController : MonoBehaviour {

    /// <summary>
    /// Inputs to control the character by feeding inputMovementX and inputMovementY.
    /// </summary>
    public BCG_Inputs inputs;

    [Header("References")]
    [Tooltip("Assign the main camera for looking up/down.")]
    public Camera playerCamera;

    public bool canControl = true;

    private CharacterController characterController;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float lookSensitivity = 2f;
    [Tooltip("Clamp the up/down look rotation (in degrees).")]
    public float maxLookAngle = 90f;

    // Internal variables
    private Vector2 moveInput;      // Stores input from OnMove
    private Vector2 lookInput;      // Stores input from OnLook
    private float xRotation = 0f;   // Current camera rotation around X-axis
    private float verticalVelocity; // For handling gravity/falling

    private void Awake() {

        characterController = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

    }

    private void Update() {

        inputs = BCG_InputManager.Instance.GetInputs();

        if (canControl) {

            //  Receive keyboard inputs if controller type is not mobile. If controller type is mobile, inputs will be received by BCG_MobileCharacterController component attached to FPS/ TPS Controller UI Canvas.
            if (!BCG_EnterExitSettings.Instance.mobileController) {

                //	X and Y inputs based "Vertical" and "Horizontal" axes.
                moveInput.x = inputs.horizonalInput;
                moveInput.y = inputs.verticalInput;

                // Handle mouse look
                lookInput.x = inputs.aim.x * lookSensitivity;
                lookInput.y = inputs.aim.y * lookSensitivity;

            } else {

                //	X and Y inputs based "Vertical" and "Horizontal" axes.
                moveInput.x = BCG_MobileCharacterController.move.x;
                moveInput.y = BCG_MobileCharacterController.move.y;

                // Handle mouse look
                lookInput.x = BCG_MobileCharacterController.mouse.x * lookSensitivity;
                lookInput.y = BCG_MobileCharacterController.mouse.y * lookSensitivity;

            }

        } else {

            moveInput = Vector2.zero;
            lookInput = Vector2.zero;

        }

        HandleLook();
        HandleMovement();

    }

    /// <summary>
    /// Handle camera rotation from look input.
    /// </summary>
    private void HandleLook() {

        // Horizontal rotation (y-axis) – rotate the player
        transform.Rotate(Vector3.up * (lookInput.x * lookSensitivity));

        // Vertical rotation (x-axis) – rotate the camera
        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        if (playerCamera)
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

    }

    /// <summary>
    /// Handle movement, gravity, and apply motion via CharacterController.
    /// </summary>
    private void HandleMovement() {

        // If grounded, reset vertical velocity if it's below zero to keep player 'grounded'
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // Apply gravity (not multiplying by Time.deltaTime yet, just accumulate it)
        verticalVelocity += gravity * Time.deltaTime;

        // Calculate move direction relative to our current orientation
        Vector3 moveDirection = (transform.right * moveInput.x) + (transform.forward * moveInput.y);

        // Combine move direction with vertical velocity
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        // Move the controller
        characterController.Move(velocity * Time.deltaTime);

    }

    public void FaceTargetForward(Vector3 target) {

        transform.forward = target;
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        xRotation = 0f;

    }

}

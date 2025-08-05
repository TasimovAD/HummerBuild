using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A simple Third Person Shooter-style controller using the new Unity Input System.
/// Attach to a GameObject with a CharacterController and assign the needed Input Actions.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BCG_TPSCharacterController : MonoBehaviour {

    /// <summary>
    /// Inputs to control the character by feeding inputMovementX and inputMovementY.
    /// </summary>
    public BCG_Inputs inputs;

    [Header("References")]
    [Tooltip("Reference to the main camera (ideally behind the player).")]
    public Camera playerCamera;

    private CharacterController characterController;

    public bool canControl = true;

    public Vector2 moveInput;
    public Vector2 lookInput;
    public float lookSensitivity = 2f;

    [Header("Movement Settings")]
    [Tooltip("Movement speed of the player.")]
    public float moveSpeed = 5f;
    [Tooltip("Gravity value. Negative for downward force.")]
    public float gravity = -9.81f;

    [Header("Rotation Settings")]
    [Tooltip("Horizontal rotation speed when looking.")]
    public float rotationSpeed = 180f;

    // Internal variables
    private Vector3 velocity;        // Current movement velocity (x, y, z).
    private float verticalSpeed = 0; // Vertical speed for jump/fall calculations.

    private void Awake() {

        characterController = GetComponent<CharacterController>();

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

        HandleMovement();
        HandleRotation();

    }

    private void HandleMovement() {

        // Read movement input (X = strafe, Y = forward)
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Transform the direction from local to world space
        moveDirection = transform.TransformDirection(moveDirection);

        // If the character is on the ground
        if (characterController.isGrounded) {
            // Slight downward force to keep grounded
            verticalSpeed = -1f;
        } else {
            // Apply gravity if in the air
            verticalSpeed += gravity * Time.deltaTime;
        }

        // Combine horizontal move with vertical velocity
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;
        velocity.y = verticalSpeed;

        // Move CharacterController
        characterController.Move(velocity * Time.deltaTime);

    }

    private void HandleRotation() {

        // Rotate the character around the Y-axis
        // Multiply by Time.deltaTime to keep frame-rate independence
        float rotationAmount = lookInput.x * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount);

    }

    public void FaceTargetForward(Vector3 target) {

        transform.forward = target;
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
        lookInput.Set(0f, 0f);

    }

}

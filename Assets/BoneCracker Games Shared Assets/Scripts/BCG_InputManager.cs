using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class BCG_InputManager : RCCP_Singleton<BCG_InputManager> {

    /// <summary>
    /// Flag to track if events have been subscribed to prevent double subscription.
    /// </summary>
    private bool eventsSubscribed = false;

    // Action Map Names - These should match your Input Actions asset
    private const string CHARACTER_MAP_NAME = "Character";

    /// <summary>
    /// Current input values received from the active input device
    /// </summary>
    public BCG_Inputs inputs = new BCG_Inputs();

    /// <summary>
    /// Reference to the Input Actions asset instance
    /// </summary>
    public InputActionAsset inputActionsInstance = null;

    // Input action map references cached for performance
    private InputActionMap characterMap;

    // Events for input actions

    /// <summary>
    /// Event triggered when movement input changes (WASD or Arrow keys)
    /// </summary>
    public delegate void onMovementChanged();
    public static event onMovementChanged OnInteract;

    /// <summary>
    /// Event triggered when aim input changes (Mouse movement)
    /// </summary>
    public event Action<Vector2> OnAimChanged;

    /// <summary>
    /// Awake is called when the script instance is being loaded
    /// </summary>
    private void Awake() {

        // Let the base singleton class handle the instance management
        // Only initialize if this is the valid instance
        if (Instance == this) {

            // Initialize inputs
            inputs = new BCG_Inputs();

            // Make this object persistent across scene loads
            DontDestroyOnLoad(gameObject);

        }

    }

    /// <summary>
    /// Called when the object becomes enabled and active
    /// </summary>
    private void OnEnable() {

        // Initialize input system
        InitializeInputSystem();

    }

    /// <summary>
    /// Called when the object becomes disabled
    /// </summary>
    private void OnDisable() {

        // Clean up input system
        CleanupInputSystem();

    }

    /// <summary>
    /// Initializes the input system and subscribes to input events
    /// </summary>
    private void InitializeInputSystem() {

        try {

            // Check if BCG_InputActions instance exists
            if (BCG_InputActionsSource.Instance == null) {

                Debug.LogWarning("BCG_InputActionsSource.Instance is null. Input system will not be initialized.");
                return;

            }

            // Get the Input Actions from BCG_InputActions
            inputActionsInstance = BCG_InputActionsSource.Instance.inputActions;

            if (inputActionsInstance == null) {

                Debug.LogWarning("InputActions asset is null in BCG_InputActionsSource.Instance");
                return;

            }

            // Cache action maps
            CacheActionMaps();

            // Enable the entire asset
            inputActionsInstance.Enable();

            // Subscribe to events only once.
            if (!eventsSubscribed)
                SubscribeToAllEvents();

        } catch (Exception e) {

            Debug.LogError($"Failed to initialize input system: {e.Message}");

        }

    }

    /// <summary>
    /// Caches references to action maps for performance
    /// </summary>
    private void CacheActionMaps() {

        try {

            // Find action maps by name instead of using indices
            characterMap = inputActionsInstance.FindActionMap(CHARACTER_MAP_NAME);

            // Validate that all maps were found
            if (characterMap == null)
                Debug.LogError($"Could not find action map: {CHARACTER_MAP_NAME}");

        } catch (Exception e) {

            Debug.LogError($"Failed to cache action maps: {e.Message}");

        }

    }

    /// <summary>
    /// Cleans up the input system and unsubscribes from events
    /// </summary>
    private void CleanupInputSystem() {

        // Safely unsubscribe from all events
        if (inputActionsInstance != null) {

            UnsubscribeFromAllEvents();

            // Disable the input actions
            inputActionsInstance.Disable();

        }

        // Clear cached references
        characterMap = null;

    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    private void Update() {

        // Validate and recreate inputs if null
        if (inputs == null)
            inputs = new BCG_Inputs();

        // Check if BCG_EnterExitSettings.Instance exists before accessing it
        bool useMobileController = false;

        if (BCG_EnterExitSettings.Instance != null)
            useMobileController = BCG_EnterExitSettings.Instance.mobileController;

        // Get inputs from appropriate source
        if (!useMobileController)
            inputs = GetKeyboardInputs();
        else
            inputs = GetMobileInputs();

    }

    /// <summary>
    /// Gets keyboard and gamepad inputs from the new input system
    /// </summary>
    /// <returns>BCG_Inputs structure with current input values</returns>
    private BCG_Inputs GetKeyboardInputs() {

        // Return current inputs if action system is not ready
        if (inputActionsInstance == null || characterMap == null)
            return inputs;

        try {

            // Get Movement action (WASD or Arrow keys)
            InputAction movementAction = characterMap.FindAction("Movement");
            if (movementAction != null) {

                Vector2 movement = movementAction.ReadValue<Vector2>();
                inputs.horizonalInput = movement.x;
                inputs.verticalInput = movement.y;

            }

            // Get Aim action (Mouse)
            InputAction aimAction = characterMap.FindAction("Aim");
            if (aimAction != null) {

                inputs.aim = aimAction.ReadValue<Vector2>();

            }

        } catch (Exception e) {

            Debug.LogError($"Error reading keyboard inputs: {e.Message}");

        }

        return inputs;

    }

    /// <summary>
    /// Gets mobile inputs from the mobile controller UI
    /// </summary>
    /// <returns>BCG_Inputs structure with current mobile input values</returns>
    private BCG_Inputs GetMobileInputs() {

        // Copy values from mobile inputs
        inputs.horizonalInput = BCG_MobileCharacterController.move.x;
        inputs.verticalInput = BCG_MobileCharacterController.move.y;
        inputs.aim = BCG_MobileCharacterController.mouse;

        return inputs;

    }

    /// <summary>
    /// Gets the current input values
    /// </summary>
    /// <returns>Current BCG_Inputs structure</returns>
    public BCG_Inputs GetInputs() {

        return inputs;

    }

    // Event Subscription Methods

    /// <summary>
    /// Subscribes to all input events
    /// </summary>
    private void SubscribeToAllEvents() {

        SubscribeCharacterMapEvents();

        eventsSubscribed = true;

    }

    /// <summary>
    /// Unsubscribes from all input events
    /// </summary>
    private void UnsubscribeFromAllEvents() {

        UnsubscribeCharacterMapEvents();

        eventsSubscribed = false;

    }

    /// <summary>
    /// Subscribes to character map input events
    /// </summary>
    private void SubscribeCharacterMapEvents() {

        if (characterMap == null)
            return;

        if (eventsSubscribed)
            return;

        try {

            // Subscribe to Movement action
            InputAction movementAction = characterMap.FindAction("Movement");
            if (movementAction != null) {

                movementAction.performed += OnMovementPerformed;
                movementAction.canceled += OnMovementCanceled;

            }

            // Subscribe to Aim action
            InputAction aimAction = characterMap.FindAction("Aim");
            if (aimAction != null) {

                aimAction.performed += OnAimPerformed;
                aimAction.canceled += OnAimCanceled;

            }

            // Subscribe to Interact action
            InputAction interactAction = characterMap.FindAction("Interact");
            if (interactAction != null) {

                interactAction.performed += OnInteractPressed;

            }

        } catch (Exception e) {

            Debug.LogError($"Failed to subscribe to character map events: {e.Message}");

        }

    }

    /// <summary>
    /// Unsubscribes from character map input events
    /// </summary>
    private void UnsubscribeCharacterMapEvents() {

        if (characterMap == null)
            return;

        if (!eventsSubscribed)
            return;

        try {

            // Unsubscribe from Movement action
            InputAction movementAction = characterMap.FindAction("Movement");
            if (movementAction != null) {

                movementAction.performed -= OnMovementPerformed;
                movementAction.canceled -= OnMovementCanceled;

            }

            // Unsubscribe from Aim action
            InputAction aimAction = characterMap.FindAction("Aim");
            if (aimAction != null) {

                aimAction.performed -= OnAimPerformed;
                aimAction.canceled -= OnAimCanceled;

            }

            // Unsubscribe from Interact action
            InputAction interactAction = characterMap.FindAction("Interact");
            if (interactAction != null) {

                interactAction.performed -= OnInteractPressed;

            }

        } catch (Exception e) {

            Debug.LogError($"Failed to unsubscribe from character map events: {e.Message}");

        }

    }

    // Input Action Callbacks

    /// <summary>
    /// Called when movement input is performed (WASD or Arrow keys pressed)
    /// </summary>
    /// <param name="context">Input action callback context</param>
    private void OnMovementPerformed(InputAction.CallbackContext context) {

        Vector2 movement = context.ReadValue<Vector2>();
        inputs.horizonalInput = movement.x;
        inputs.verticalInput = movement.y;

    }

    /// <summary>
    /// Called when movement input is canceled (WASD or Arrow keys released)
    /// </summary>
    /// <param name="context">Input action callback context</param>
    private void OnMovementCanceled(InputAction.CallbackContext context) {

        inputs.horizonalInput = 0f;
        inputs.verticalInput = 0f;

    }

    /// <summary>
    /// Called when aim input is performed (Mouse moved)
    /// </summary>
    /// <param name="context">Input action callback context</param>
    private void OnAimPerformed(InputAction.CallbackContext context) {

        Vector2 aimDelta = context.ReadValue<Vector2>();
        inputs.aim = aimDelta;

        // Trigger event for listeners
        OnAimChanged?.Invoke(aimDelta);

    }

    /// <summary>
    /// Called when aim input is canceled
    /// </summary>
    /// <param name="context">Input action callback context</param>
    private void OnAimCanceled(InputAction.CallbackContext context) {

        inputs.aim = Vector2.zero;

        // Trigger event for listeners
        OnAimChanged?.Invoke(Vector2.zero);

    }

    public void OnInteractPressed(InputAction.CallbackContext context) {

        OnInteract?.Invoke();

    }

    /// <summary>
    /// Handles application pause state changes (primarily for mobile platforms).
    /// Disables input when paused and re-enables when resumed.
    /// </summary>
    /// <param name="pauseStatus">True when application is paused, false when resumed</param>
    private void OnApplicationPause(bool pauseStatus) {

        // Only handle if we have valid input actions and not overriding inputs
        if (inputActionsInstance == null)
            return;

        try {

            if (pauseStatus) {

                // Application is being paused - disable inputs to prevent stuck inputs
                if (inputActionsInstance.enabled) {

                    inputActionsInstance.Disable();

                    // Reset current input values to prevent stuck inputs
                    ResetInputValues();

                    Debug.Log("BCG_InputManager: Inputs disabled due to application pause");

                }

            } else {

                // Application is resuming - re-enable inputs
                if (!inputActionsInstance.enabled) {

                    inputActionsInstance.Enable();
                    Debug.Log("BCG_InputManager: Inputs re-enabled after application resume");

                }

            }

        } catch (Exception e) {

            Debug.LogError($"BCG_InputManager: Error handling application pause: {e.Message}");

        }

    }

    /// <summary>
    /// Handles application focus changes (primarily for desktop platforms).
    /// Disables input when focus is lost and re-enables when focus is regained.
    /// </summary>
    /// <param name="hasFocus">True when application has focus, false when focus is lost</param>
    private void OnApplicationFocus(bool hasFocus) {

        // Only handle if we have valid input actions and not overriding inputs
        if (inputActionsInstance == null)
            return;

        // Skip on mobile platforms as OnApplicationPause handles it
#if UNITY_ANDROID || UNITY_IOS
        return;
#endif

        try {

            if (!hasFocus) {

                // Application lost focus - disable inputs to prevent stuck inputs
                if (inputActionsInstance.enabled) {

                    inputActionsInstance.Disable();

                    // Reset current input values to prevent stuck inputs
                    ResetInputValues();

                }

            } else {

                // Application regained focus - re-enable inputs
                if (!inputActionsInstance.enabled) {

                    inputActionsInstance.Enable();

                }

            }

        } catch (Exception e) {

            Debug.LogError($"BCG_InputManager: Error handling application focus: {e.Message}");

        }

    }

    /// <summary>
    /// Resets all input values to their default state.
    /// Called when application loses focus or is paused to prevent stuck inputs.
    /// </summary>
    private void ResetInputValues() {

        if (inputs != null) {

            inputs.horizonalInput = 0f;
            inputs.verticalInput = 0f;
            inputs.aim = Vector2.zero;

        }

    }

}
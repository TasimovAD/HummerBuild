//----------------------------------------------
//            BCG Shared Assets
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

public class BCG_TPSCameraController : MonoBehaviour {

    public BCG_TPSCharacterController target; // The target the camera will follow (usually the player)

    /// <summary>
    /// Inputs to control the character by feeding inputMovementX and inputMovementY.
    /// </summary>
    public BCG_Inputs inputs;

    /// <summary>
    /// Offset from the target
    /// </summary>
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    /// <summary>
    /// Speed of rotation around the target
    /// </summary>
    public float rotationSpeed = 5f;

    /// <summary>
    /// Distance from the target
    /// </summary>
    public float distance = 10f;

    /// <summary>
    /// Minimum vertical angle of the camera
    /// </summary>
    public float minYAngle = -30f;

    /// <summary>
    /// Maximum vertical angle of the camera
    /// </summary>
    public float maxYAngle = 60f;

    /// <summary>
    /// Smoothing speed for camera movement
    /// </summary>
    public float smoothSpeed = 0.125f;

    private float currentX = 0f;
    private float currentY = 0f;

    private void OnEnable() {

        target = FindObjectOfType<BCG_TPSCharacterController>();

        if (!target) {

            // Initialize camera rotation based on the initial offset
            Vector3 thisAngles = transform.eulerAngles;
            currentX = thisAngles.y;
            currentY = thisAngles.x;

            Debug.Log("Target couldn't found on " + gameObject.name + "!");
            return;

        }

        // Initialize camera rotation based on the initial offset
        Vector3 angles = target.transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

    }

    private void LateUpdate() {

        if (!target) {

            Debug.Log("Target couldn't found on " + gameObject.name + "!");
            return;

        }

        currentX += target.lookInput.x;
        currentY -= target.lookInput.y;

        // Clamp the vertical rotation to prevent flipping
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // Calculate the desired rotation
        Quaternion rotation = Quaternion.Euler(currentY, 0f, 0f);

        // Calculate the desired position
        Vector3 desiredPosition = target.transform.position + target.transform.rotation * rotation * offset;

        // Smoothly move the camera to the desired position
        transform.position = desiredPosition;

        // Always look at the target
        transform.LookAt(target.transform.position);

    }

}

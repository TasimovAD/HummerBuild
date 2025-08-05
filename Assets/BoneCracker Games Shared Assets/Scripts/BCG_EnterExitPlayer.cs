//----------------------------------------------
//            BCG Shared Assets
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enter Exit for FPS Player.
/// </summary>
[AddComponentMenu("BoneCracker Games/BCG Shared Assets Pro/BCG Enter Exit Player")]
public class BCG_EnterExitPlayer : MonoBehaviour {

    /// <summary>
    /// Is this player controller is a TPS controller?
    /// </summary>
    public bool isTPSController = false;

    /// <summary>
    /// If TPS controller, set raycast height.
    /// </summary>
    public float rayHeight = 1f;

    /// <summary>
    /// Is this controller can be controllable now?
    /// </summary>
    public bool canControl = true;

    /// <summary>
    /// Show enter exit UI message.
    /// </summary>
    public bool showGui = false;

    /// <summary>
    /// Target vehicle to enter.
    /// </summary>
    public BCG_EnterExitVehicle targetVehicle;

    /// <summary>
    /// Player starts in this target vehicle.
    /// </summary>
    public bool playerStartsAsInVehicle = false;

    /// <summary>
    /// Player is currently in this vehicle.
    /// </summary>
    public BCG_EnterExitVehicle inVehicle;

    /// <summary>
    /// If FPS controller, there should be a character camera inside the player.
    /// </summary>
    public Camera characterCamera;

    /// <summary>
    /// Event when BCG player spawned.
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGPlayerSpawned(BCG_EnterExitPlayer player);
    public static event onBCGPlayerSpawned OnBCGPlayerSpawned;

    /// <summary>
    /// Event when BCG player destroyed.
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGPlayerDestroyed(BCG_EnterExitPlayer player);
    public static event onBCGPlayerDestroyed OnBCGPlayerDestroyed;

    /// <summary>
    /// Event when BCG player entered a vehicle.
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGPlayerEnteredAVehicle(BCG_EnterExitPlayer player, BCG_EnterExitVehicle vehicle);
    public static event onBCGPlayerEnteredAVehicle OnBCGPlayerEnteredAVehicle;

    /// <summary>
    /// Event when BCG played exited from a vehicle. 
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGPlayerExitedFromAVehicle(BCG_EnterExitPlayer player, BCG_EnterExitVehicle vehicle);
    public static event onBCGPlayerExitedFromAVehicle OnBCGPlayerExitedFromAVehicle;

    private void Awake() {

        if (!playerStartsAsInVehicle)
            inVehicle = null;

        if (!isTPSController)
            characterCamera = GetComponentInChildren<Camera>();

    }

    private void OnEnable() {

        if (OnBCGPlayerSpawned != null)
            OnBCGPlayerSpawned(this);

        if (playerStartsAsInVehicle)
            StartCoroutine(StartInVehicle());

    }

    private IEnumerator StartInVehicle() {

        yield return new WaitForFixedUpdate();
        GetIn(inVehicle);

    }

    public void GetIn(BCG_EnterExitVehicle vehicle) {

        if (OnBCGPlayerEnteredAVehicle != null)
            OnBCGPlayerEnteredAVehicle(this, vehicle);

    }

    public void GetOut() {

        if (inVehicle == null)
            return;

        if (Mathf.Abs(inVehicle.CarController.absoluteSpeed) > BCG_EnterExitSettings.Instance.enterExitSpeedLimit)
            return;

        if (OnBCGPlayerExitedFromAVehicle != null)
            OnBCGPlayerExitedFromAVehicle(this, inVehicle);

    }

    public void GetOutImmediately() {

        if (inVehicle == null)
            return;

        if (OnBCGPlayerExitedFromAVehicle != null)
            OnBCGPlayerExitedFromAVehicle(this, inVehicle);

    }

    private void Update() {

        if (!canControl)
            return;

        Vector3 rayPosition;
        Quaternion rayRotation = new Quaternion();

        if (characterCamera && !isTPSController) {

            rayPosition = characterCamera.transform.position;
            rayRotation = characterCamera.transform.rotation;

        } else {

            rayPosition = transform.position + (Vector3.up * rayHeight);
            rayRotation = transform.rotation;

        }

        Vector3 rayDirection = rayRotation * Vector3.forward;
        RaycastHit hit;

        Debug.DrawRay(rayPosition, rayDirection * 1.5f, Color.blue);

        if (Physics.Raycast(rayPosition, rayDirection, out hit, 1.5f)) {

            if (!targetVehicle) {

                targetVehicle = hit.collider.transform.GetComponentInParent<BCG_EnterExitVehicle>();

            } else {

                showGui = true;

                //if (Input.GetKeyDown(BCG_EnterExitSettings.Instance.enterExitVehicleKB))
                //    GetIn(targetVehicle);

            }

        } else {

            showGui = false;
            targetVehicle = null;

        }

    }

    private void OnGUI() {

        if (showGui) {

            GUI.skin.label.fontSize = 36;
            GUI.Label(new Rect((Screen.width / 2f) - 300f, (Screen.height / 2f) - 25f, 600f, 50f), "Press Interaction [TAB] Key To Get In");

        }

    }

    private void OnDestroy() {

        if (OnBCGPlayerDestroyed != null)
            OnBCGPlayerDestroyed(this);

    }

}

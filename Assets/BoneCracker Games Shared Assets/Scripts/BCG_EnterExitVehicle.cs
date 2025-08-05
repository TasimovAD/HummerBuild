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
/// Enter Exit for BCG Vehicles.
/// </summary>
[AddComponentMenu("BoneCracker Games/BCG Shared Assets Pro/BCG Enter Exit Vehicle")]
public class BCG_EnterExitVehicle : MonoBehaviour {

    /// <summary>
    /// Car controller.
    /// </summary>
    private RCCP_CarController _carController;
    public RCCP_CarController CarController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCCP_CarController>(true);

            return _carController;

        }

    }

    /// <summary>
    /// Corresponding camera to enable / disable.
    /// </summary>
    public GameObject correspondingCamera;

    /// <summary>
    /// Currently this driver is using this cvehicle.
    /// </summary>
    public BCG_EnterExitPlayer driver;

    /// <summary>
    /// Get out position that will be used to transport the BCG player to this location.
    /// </summary>
    public Transform getOutPosition;

    /// <summary>
    /// Event when a BCG vehicle spawned.
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGVehicleSpawned(BCG_EnterExitVehicle player);
    public static event onBCGVehicleSpawned OnBCGVehicleSpawned;

    /// <summary>
    /// Event when a BCG vehicle destroyed.
    /// </summary>
    /// <param name="player"></param>
    public delegate void onBCGVehicleDestroyed(BCG_EnterExitVehicle player);
    public static event onBCGVehicleDestroyed OnBCGVehicleDestroyed;

    private void OnEnable() {

        Reset();

        gameObject.SendMessage("SetCanControl", false, SendMessageOptions.DontRequireReceiver);

        if (OnBCGVehicleSpawned != null)
            OnBCGVehicleSpawned(this);

    }

    private void Update() {

        //if (driver != null && Input.GetKeyDown(BCG_EnterExitSettings.Instance.enterExitVehicleKB))
        //	GetOut();

    }

    public void GetOut() {

        driver.GetOut();

    }

    public void Reset() {

        if (transform.Find("Get Out Pos")) {

            getOutPosition = transform.Find("Get Out Pos");

        } else {

            GameObject getOut = new GameObject("Get Out Pos");
            getOut.transform.SetParent(transform, false);
            getOut.transform.rotation = transform.rotation;
            getOut.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
            getOutPosition = getOut.transform;

        }

        if (correspondingCamera)
            return;

#if BCG_RCCP

        if (CarController) {

            correspondingCamera = FindObjectOfType<RCCP_Camera>().gameObject;
            return;

        }

#endif

#if BCG_RTC

        if (tankController) {

            correspondingCamera = FindObjectOfType<RTC_Camera>().gameObject;
            return;

        }

#endif

#if BCG_RHOC

//		if(hoverController){
//
//		correspondingCamera = FindObjectOfType<RCC_Camera> ().gameObject;
//		return;
//
//		}

#endif

    }

    private void OnDisable() {

        if (OnBCGVehicleDestroyed != null)
            OnBCGVehicleDestroyed(this);

    }

}

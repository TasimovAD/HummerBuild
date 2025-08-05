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
/// Main Enter Exit Manager for Scene.
/// </summary>
[AddComponentMenu("BoneCracker Games/BCG Shared Assets Pro/BCG Enter Exit Manager")]
public class BCG_EnterExitManager : MonoBehaviour {

    #region singleton
    private static BCG_EnterExitManager instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static BCG_EnterExitManager Instance {

        get {

            if (instance == null) {

                instance = FindObjectOfType<BCG_EnterExitManager>();

                if (instance == null) {

                    GameObject sceneManager = new GameObject("_BCGEnterExitManager");
                    instance = sceneManager.AddComponent<BCG_EnterExitManager>();

                }

            }

            return instance;

        }

    }
    #endregion

    /// <summary>
    /// Current active player.
    /// </summary>
    public BCG_EnterExitPlayer activePlayer;

    /// <summary>
    /// Cached cameras for vehicle controllers.
    /// </summary>
    public List<GameObject> cachedMainCameras = new List<GameObject>();

    /// <summary>
    /// Cached FPS/TPS players.
    /// </summary>
    public List<BCG_EnterExitPlayer> cachedPlayers = new List<BCG_EnterExitPlayer>();

    /// <summary>
    /// Cached vehicles.
    /// </summary>
    public List<BCG_EnterExitVehicle> cachedVehicles = new List<BCG_EnterExitVehicle>();

    /// <summary>
    /// UI used for enter exit.
    /// </summary>
    public BCG_EnterExitCharacterUICanvas cachedCanvas;

    /// <summary>
    /// Delay used to avoid instant get in/out.
    /// </summary>
    internal float waitTime = 5;

    public BCG_Inputs inputs;

    private void Awake() {

        BCG_EnterExitPlayer.OnBCGPlayerSpawned += BCG_Player_OnBCGPlayerSpawned;        //	Listening to any FPS/TPS character when they spawned.
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed += BCG_Player_OnBCGPlayerDestroyed;        //	Listening to any FPS/TPS character when they destroyed.
        BCG_EnterExitVehicle.OnBCGVehicleSpawned += BCG_Player_OnBCGVehicleSpawned;     //	Listening to any vehicle when they spawn.
        BCG_EnterExitVehicle.OnBCGVehicleDestroyed += BCG_Player_OnBCGVehicleDestroyed;     //	Listening to any vehicle when they destroyed.
        BCG_EnterExitPlayer.OnBCGPlayerEnteredAVehicle += BCG_Player_OnBCGPlayerEnteredAVehicle;        //	Listening an event when player gets in a vehicle.
        BCG_EnterExitPlayer.OnBCGPlayerExitedFromAVehicle += BCG_Player_OnBCGPlayerExitedFromAVehicle;      //	Listening an event when player gets out of a vehicle.
        BCG_EnterExitCharacterUICanvas.OnBCGPlayerCanvasSpawned += BCG_EnterExitCharacterUICanvas_OnBCGPlayerCanvasSpawned;     //	Listening an event when UI canvas spawned.

    }

    private void Start() {

        //	Setting off all vehicle cameras at start.
        for (int i = 0; i < cachedMainCameras.Count; i++)
            cachedMainCameras[i].SendMessage("ToggleCamera", false, SendMessageOptions.DontRequireReceiver);

        BCG_InputManager.Instance.GetInputs();

    }

    private void OnEnable() {

        // Listening input events on BCG_InputManager.
        BCG_InputManager.OnInteract += BCG_InputManager_OnInteract;

    }

    private void BCG_InputManager_OnInteract() {

        Interact();

    }

    private void Update() {

        //	Delay used to avoid instant get in/out.
        waitTime += Time.deltaTime;

        if (BCG_EnterExitSettings.Instance.autoLockMouseCursor && !BCG_EnterExitSettings.Instance.mobileController) {

            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None; // Unlock the cursor

            if (Input.GetMouseButtonDown(1))
                Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen

        }

    }

    /// <summary>
    /// When any FPS/TPS character controller spawned.
    /// </summary>
    /// <param name="player"></param>
    public void BCG_Player_OnBCGPlayerSpawned(BCG_EnterExitPlayer player) {

        //	If player is not cached, add it to the list. Otherwise don't add it.
        if (!cachedPlayers.Contains(player))
            cachedPlayers.Add(player);

        //	If currently there is no active player, assign it as an active player.
        if (activePlayer == null)
            activePlayer = player;

    }

    /// <summary>
    /// When any FPS/TPS character controller destroyed.
    /// </summary>
    /// <param name="player"></param>
    public void BCG_Player_OnBCGPlayerDestroyed(BCG_EnterExitPlayer player) {

        //	If player is cached before, remove it.
        if (cachedPlayers.Contains(player))
            cachedPlayers.Remove(player);

        //	If destroyed player is active player, remove it.
        if (activePlayer == player)
            activePlayer = null;

    }

    /// <summary>
    /// When any vehicle spawned.
    /// </summary>
    /// <param name="player"></param>
    public void BCG_Player_OnBCGVehicleSpawned(BCG_EnterExitVehicle player) {

        //	If vehicle is not cached, add it to the list.
        if (!cachedVehicles.Contains(player))
            cachedVehicles.Add(player);

        //	If camera of the vehicle is not in the list, add it to the list and disable it.
        if (!cachedMainCameras.Contains(player.correspondingCamera)) {

            cachedMainCameras.Add(player.correspondingCamera);
            player.correspondingCamera.SendMessage("ToggleCamera", false, SendMessageOptions.DontRequireReceiver);

        }

        //	If camera of the vehicle is not in the list, add it to the list and disable it.
        RCCP_SceneManager.Instance.activePlayerCamera.ToggleCamera(false);

    }

    /// <summary>
    /// When any vehicle destroyed.
    /// </summary>
    /// <param name="player"></param>
    public void BCG_Player_OnBCGVehicleDestroyed(BCG_EnterExitVehicle player) {

        //	If vehicle is not cached, add it to the list.
        if (cachedVehicles.Contains(player))
            cachedVehicles.Remove(player);

    }

    /// <summary>
    /// When player gets in a vehicle.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="vehicle"></param>
    public void BCG_Player_OnBCGPlayerEnteredAVehicle(BCG_EnterExitPlayer player, BCG_EnterExitVehicle vehicle) {

        // If delay is not enough, return.
        if (waitTime < 1)
            return;

        waitTime = 0f;      //	Resetting delay timer.

        print("Player Named " + player.name + " has entered a vehicle named " + vehicle.name);

        player.inVehicle = vehicle;     //	Player is now in this vehicle.
        player.gameObject.SetActive(false);     //	Disable the actual player character.
        player.transform.SetParent(vehicle.transform, true);        //	Parent.
        player.transform.localPosition = Vector3.zero;      //	Resetting local position, rotation, and scale.
        player.transform.localRotation = Quaternion.identity;       //	//	Resetting local position, rotation, and scale.
        player.transform.position = vehicle.transform.position;     //	//	Resetting local position, rotation, and scale.

        // If character controller is a TPS controller, that means it must have a individual camera. Disable it.
        if (player.isTPSController)
            player.characterCamera.gameObject.SetActive(false);

        //	If UI canvas is cached, set it as "In Vehicle" mode. In this mode, UI will only use in vehicle buttons.
        if (cachedCanvas)
            cachedCanvas.displayType = BCG_EnterExitCharacterUICanvas.DisplayType.InVehicle;

        //	Enabling only entered vehicle's camera. All other vehicle cameras will be disabled.
        for (int i = 0; i < cachedMainCameras.Count; i++) {

            if (cachedMainCameras[i] != vehicle.correspondingCamera) {
                cachedMainCameras[i].SendMessage("ToggleCamera", false, SendMessageOptions.DontRequireReceiver);
            } else {
                cachedMainCameras[i].SendMessage("ToggleCamera", true, SendMessageOptions.DontRequireReceiver);
                cachedMainCameras[i].SendMessage("SetTarget", vehicle.CarController, SendMessageOptions.DontRequireReceiver);
            }

        }

        vehicle.gameObject.SendMessage("SetCanControl", true, SendMessageOptions.DontRequireReceiver);      //	Enabling controllable state of the vehicle.
        //vehicle.gameObject.SendMessage("SetExternalControl", false, SendMessageOptions.DontRequireReceiver);        //	Disabling external controllable state of the vehicle.

        if (BCG_EnterExitSettings.Instance.startStopEngine)
            vehicle.gameObject.SendMessage("SetEngine", true, SendMessageOptions.DontRequireReceiver);      //	Starting the engine.

        vehicle.driver = player;        //	Assigning vehicle's driver.

#if BCG_RCCP
        if (vehicle.CarController)
            RCCP_SceneManager.Instance.activePlayerVehicle = vehicle.CarController;		//	And finally, set the actual vehicle.
#endif
#if BCG_RTC
        if (vehicle.tankController)
            RTC_SceneManager.Instance.activePlayerVehicle = vehicle.tankController;		//	And finally, reset the actual vehicle.
#endif

        if (BCG_EnterExitSettings.Instance.autoLockMouseCursor && !BCG_EnterExitSettings.Instance.mobileController)
            Cursor.lockState = CursorLockMode.None;

    }

    /// <summary>
    /// When player gets out of the vehicle.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="vehicle"></param>
    public void BCG_Player_OnBCGPlayerExitedFromAVehicle(BCG_EnterExitPlayer player, BCG_EnterExitVehicle vehicle) {

        // If delay is not enough, return.
        if (waitTime < 1)
            return;

        print("Player Named " + player.name + " has exited from a vehicle named " + vehicle.name);

        Vector3 lookDir = player.inVehicle.transform.forward;
        player.inVehicle = null;        //	Player is not in the vehicle now.
        player.transform.SetParent(null);       //	Player is not child of the vehicle anymore.
        player.transform.rotation = Quaternion.Euler(0f, vehicle.transform.rotation.y, 0f);     //	Settings position and rotation of the player.

        //	If UI canvas is cached, set it as "On Foot" mode. In this mode, UI will only use in vehicle buttons.
        if (cachedCanvas)
            cachedCanvas.displayType = BCG_EnterExitCharacterUICanvas.DisplayType.OnFoot;

        //	If vehicle has get out position, set position and rotation of the character player.
        if (vehicle.getOutPosition) {
            player.transform.position = vehicle.getOutPosition.position;
        } else {
            player.transform.position = vehicle.transform.position;
            player.transform.position += vehicle.transform.right * -1.5f;
        }

        //	Enable the carachter player.
        player.gameObject.SetActive(true);

        player.SendMessage("FaceTargetForward", lookDir, SendMessageOptions.DontRequireReceiver);

        // If character player is TPS controller, enable it's camera.
        if (player.isTPSController)
            player.characterCamera.gameObject.SetActive(true);

        //	Disable all vehicle cameras.
        for (int i = 0; i < cachedMainCameras.Count; i++)
            cachedMainCameras[i].SendMessage("ToggleCamera", false, SendMessageOptions.DontRequireReceiver);

        vehicle.gameObject.SendMessage("SetCanControl", false, SendMessageOptions.DontRequireReceiver);       //	Disabling controllable state of the vehicle.
        //vehicle.gameObject.SendMessage("SetExternalControl", true, SendMessageOptions.DontRequireReceiver);     //	Enabled external controllable state of the vehicle.

        if (BCG_EnterExitSettings.Instance.startStopEngine)
            vehicle.gameObject.SendMessage("SetEngine", BCG_EnterExitSettings.Instance.keepEnginesAlive, SendMessageOptions.DontRequireReceiver);       //	Setting engine.

        vehicle.driver = null;      //	Vehicle is empty now.

#if BCG_RCCP
        if (vehicle.CarController)
            RCCP_SceneManager.Instance.activePlayerVehicle = null;		//	And finally, reset the actual vehicle.
#endif
#if BCG_RTC
        if (vehicle.tankController)
            RTC_SceneManager.Instance.activePlayerVehicle = null;		//	And finally, reset the actual vehicle.
#endif

        waitTime = 0f;      //	Resetting delay timer.

#if BCG_ENTEREXITPHOTON
		//	Requesting ownership of the vehicle.
		if(BCG_EnterExitSettings.Instance.enableEnterExit_Photon){

			if(vehicle.gameObject.GetComponent<Photon.Pun.PhotonView>())
				vehicle.gameObject.GetComponent<Photon.Pun.PhotonView>().TransferOwnership(0);

		}
#endif

        if (BCG_EnterExitSettings.Instance.autoLockMouseCursor && !BCG_EnterExitSettings.Instance.mobileController)
            Cursor.lockState = CursorLockMode.Locked;

    }

    /// <summary>
    /// When UI spawns.
    /// </summary>
    /// <param name="canvas"></param>
    public void BCG_EnterExitCharacterUICanvas_OnBCGPlayerCanvasSpawned(BCG_EnterExitCharacterUICanvas canvas) {

        cachedCanvas = canvas;      //	Assigning cached canvas.

        //	If active player exists, enable/disable corresponding mode.
        if (activePlayer) {

            if (activePlayer.inVehicle != null)
                cachedCanvas.displayType = BCG_EnterExitCharacterUICanvas.DisplayType.InVehicle;
            else
                cachedCanvas.displayType = BCG_EnterExitCharacterUICanvas.DisplayType.OnFoot;

        }

    }

    /// <summary>
    /// Interaction used to get in/out of the vehicle.
    /// </summary>
    public void Interact() {

        if (!activePlayer)
            return;

        //	If player is not in a vehicle, and at front of a vehicle, get in. Otherwise, get out.
        if (activePlayer.inVehicle == null && activePlayer.targetVehicle)
            activePlayer.GetIn(activePlayer.targetVehicle);
        else
            activePlayer.GetOut();

    }

    private void OnDisable() {

        BCG_EnterExitPlayer.OnBCGPlayerSpawned -= BCG_Player_OnBCGPlayerSpawned;
        BCG_EnterExitPlayer.OnBCGPlayerDestroyed -= BCG_Player_OnBCGPlayerDestroyed;
        BCG_EnterExitVehicle.OnBCGVehicleSpawned -= BCG_Player_OnBCGVehicleSpawned;
        BCG_EnterExitVehicle.OnBCGVehicleDestroyed -= BCG_Player_OnBCGVehicleDestroyed;
        BCG_EnterExitPlayer.OnBCGPlayerEnteredAVehicle -= BCG_Player_OnBCGPlayerEnteredAVehicle;
        BCG_EnterExitPlayer.OnBCGPlayerExitedFromAVehicle -= BCG_Player_OnBCGPlayerExitedFromAVehicle;
        BCG_EnterExitCharacterUICanvas.OnBCGPlayerCanvasSpawned -= BCG_EnterExitCharacterUICanvas_OnBCGPlayerCanvasSpawned;

        // Listening input events on BCG_InputManager.
        BCG_InputManager.OnInteract -= BCG_InputManager_OnInteract;

    }

}

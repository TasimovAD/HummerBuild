//----------------------------------------------
//            BCG Shared Assets
//
// Copyright © 2014 - 2021 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("BoneCracker Games/BCG Shared Assets Pro/UI/BCG Mobile Character Controller")]
public class BCG_MobileCharacterController : MonoBehaviour {

    public static Vector2 mouse;
    public static Vector2 move;

    public BCG_Joystick mouseJoystick;
    public BCG_Joystick moveJoystick;


    private void Update() {

        mouse = mouseJoystick.inputVector;
        move = moveJoystick.inputVector;

    }

}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;


/// <summary>
/// BCG InputAction.
/// </summary>
public class BCG_InputActionsSource : ScriptableObject {

    #region singleton
    private static BCG_InputActionsSource instance;
    public static BCG_InputActionsSource Instance { get { if (instance == null) instance = Resources.Load("BCG_InputActions") as BCG_InputActionsSource; return instance; } }
    #endregion

    public InputActionAsset inputActions;

}

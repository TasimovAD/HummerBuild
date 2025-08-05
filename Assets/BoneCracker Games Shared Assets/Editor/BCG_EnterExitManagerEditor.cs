//----------------------------------------------
//            BCG Shared Assets
//
// Copyright © 2014 - 2021 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(BCG_EnterExitManager))]
public class BCG_EnterExitManagerEditor : Editor {

    BCG_EnterExitManager prop;

    [MenuItem("Tools/BoneCracker Games/Shared Assets/Enter-Exit/Edit Enter-Exit Settings", false, 100)]
    public static void OpenBCGEnterExitSettings() {

        Selection.activeObject = BCG_EnterExitSettings.Instance;

    }

    [MenuItem("Tools/BoneCracker Games/Shared Assets/Enter-Exit/Add Main Enter-Exit Manager To Scene", false, 100)]
    public static void CreateEnterExitManager() {

        if (FindObjectOfType<BCG_EnterExitManager>()) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Scene has _BCGEnterExitManager already!", "Scene has _BCGEnterExitManager already!", "Close");

        } else {

            GameObject newBCG_EnterExitManager = new GameObject();
            newBCG_EnterExitManager.transform.name = "_BCGEnterExitManager";
            newBCG_EnterExitManager.transform.position = Vector3.zero;
            newBCG_EnterExitManager.transform.rotation = Quaternion.identity;
            newBCG_EnterExitManager.AddComponent<BCG_EnterExitManager>();

            Selection.activeGameObject = newBCG_EnterExitManager;

        }

    }

    [MenuItem("Tools/BoneCracker Games/Shared Assets/Enter-Exit/Add Enter-Exit To Vehicle", false, 100)]
    public static void CreateEnterExitVehicle() {

        if (Selection.activeGameObject == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Select your vehicle on your scene, and then come back again!", "Select your vehicle on your scene, and then come back again.", "Close");
            return;

        }

        if (Selection.activeGameObject.GetComponentInParent<BCG_EnterExitVehicle>())
            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Selected vehicle has BCG_EnterExitVehicle already!", "Selected vehicle has BCG_EnterExitVehicle already!", "Close");
        else
            Selection.activeGameObject.AddComponent<BCG_EnterExitVehicle>();

    }

    [MenuItem("Tools/BoneCracker Games/Shared Assets/Enter-Exit/Add Enter-Exit To FPS Player", false, 100)]
    public static void CreateEnterExitPlayer() {

        if (Selection.activeGameObject == null) {

            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Select your FPS player on your scene, and then come back again!", "Select your FPS player on your scene, and then come back again.", "Close");
            return;

        }

        if (Selection.activeGameObject.GetComponentInParent<BCG_EnterExitPlayer>())
            EditorUtility.DisplayDialog("Realistic Car Controller Pro | Selected FPS Player has BCG_EnterExitPlayer already!", "Selected FPS Player has BCG_EnterExitPlayer already!", "Close");
        else
            Selection.activeGameObject.AddComponent<BCG_EnterExitPlayer>();

    }

    public override void OnInspectorGUI() {

        prop = (BCG_EnterExitManager)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("General event based enter exit system for all vehicles created by BCG.", MessageType.Info);

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("activePlayer"), new GUIContent("Active Character Player"), true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cachedMainCameras"), new GUIContent("Cached BCG Main Cameras"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cachedPlayers"), new GUIContent("Cached BCG Character Players"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cachedVehicles"), new GUIContent("Cached BCG Vehicles"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cachedCanvas"), new GUIContent("Cached UI Canvases"), true);

        EditorGUI.EndDisabledGroup();

        if (EditorApplication.isPlaying && prop.cachedMainCameras != null && prop.cachedMainCameras.Count == 0)
            EditorGUILayout.HelpBox("One main camera needed at least.", MessageType.Error);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}

//----------------------------------------------
//            BCG Shared Assets
//
// Copyright © 2014 - 2021 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(BCG_EnterExitSettings))]
public class BCG_EnterExitSettingsEditor : Editor {

    BCG_EnterExitSettings prop;

    public override void OnInspectorGUI() {

        prop = (BCG_EnterExitSettings)target;
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoLockMouseCursor"), new GUIContent("Auto Lock Mouse Cursor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startStopEngine"), new GUIContent("Start Stop Engine"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("keepEnginesAlive"), new GUIContent("Keep Engines Alive"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enterExitSpeedLimit"), new GUIContent("Enter Exit Speed Limit"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mobileController"), new GUIContent("Mobile Controller"));

        EditorGUILayout.LabelField("BCG Enter Exit  " + BCG_Version.version + " \nBoneCracker Games", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        EditorGUILayout.LabelField("Developed by Ekrem Bugra Ozdoganlar", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

}

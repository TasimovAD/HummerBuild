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
using UnityEngine.Rendering;

public class BCG_EnterExitInitLoad : MonoBehaviour {

    [InitializeOnLoadMethod]
    static void InitOnLoad() {

        EditorApplication.delayCall += EditorUpdate;

    }

    public static void EditorUpdate() {

        bool hasKey = false;

#if BCG_ENTEREXIT
        hasKey = true;
#endif

        if (!hasKey) {

            Selection.activeObject = BCG_EnterExitSettings.Instance;
            BCG_SetScriptingSymbol.SetEnabled("BCG_ENTEREXIT", true);

            RCCP_SceneUpdater.Check();

            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;

            if (rp == null)   // Built-in → nothing to convert
                return;

            bool isURP = rp.GetType().ToString().Contains("Universal");
            bool isHDRP = rp.GetType().ToString().Contains("HD");

            if (!isURP && !isHDRP)
                return;

            string rpName = isURP ? "URP" : "HDRP";
            bool ok = EditorUtility.DisplayDialog(
                "Convert Materials",
                $"Your project is using {rpName}.\n\n" +
                $"You'll need to convert the imported assets to be working with {rpName}.?\n\n" +
                $"You can open the RCCP Render Pipeline Converter Window and proceed.",
                "Yes, open converter",
                "No thanks"
            );

            if (!ok)
                return;

            RCCP_RenderPipelineConverterWindow.Init();

        }

    }

}

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
/// All demo scenes.
/// </summary>
public class BCG_DemoScenes : ScriptableObject {

    public int instanceId = 0;

    #region singleton
    private static BCG_DemoScenes instance;
    public static BCG_DemoScenes Instance { get { if (instance == null) instance = Resources.Load("BCG_DemoScenes") as BCG_DemoScenes; return instance; } }
    #endregion

    public Object demo_BlankFPS;
    public Object demo_BlankTPS;

    public Object demo_CityFPS;
    public Object demo_CityTPS;

    public string path_demo_BlankFPS;
    public string path_demo_BlankTPS;

    public string path_demo_CityFPS;
    public string path_demo_CityTPS;

    public void Clean() {

        demo_CityFPS = null;
        demo_CityTPS = null;
        demo_BlankFPS = null;
        demo_BlankTPS = null;

        path_demo_BlankFPS = "";
        path_demo_BlankTPS = "";
        path_demo_CityFPS = "";
        path_demo_CityTPS = "";

    }

    public void GetPaths() {

        if (demo_BlankFPS != null)
            path_demo_BlankFPS = RCCP_GetAssetPath.GetAssetPath(demo_BlankFPS);

        if (demo_BlankTPS != null)
            path_demo_BlankTPS = RCCP_GetAssetPath.GetAssetPath(demo_BlankTPS);

        if (demo_CityFPS != null)
            path_demo_CityFPS = RCCP_GetAssetPath.GetAssetPath(demo_CityFPS);

        if (demo_CityTPS != null)
            path_demo_CityTPS = RCCP_GetAssetPath.GetAssetPath(demo_CityTPS);

    }

}

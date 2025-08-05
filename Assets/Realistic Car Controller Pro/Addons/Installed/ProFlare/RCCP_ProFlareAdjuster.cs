//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2023 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Pro Flare/RCCP Pro Flare Adjuster")]
public class RCCP_ProFlareAdjuster : MonoBehaviour{

	private Light _light;
    private ProFlare proFlare;
    private float defaultScale = 1f;

    public float flareMultiplier = 1f;

    public bool changeScale = true;
    public bool changeColor = true;

    void Start(){

        _light = GetComponent<Light>();
        proFlare = GetComponentInChildren<ProFlare>();
        defaultScale = proFlare.GlobalScale;
        
    }

    void Update(){

        if (!proFlare || !_light)
            return;

        if(changeScale)
            proFlare.GlobalScale = defaultScale * _light.intensity * flareMultiplier;

        if(changeColor)
            proFlare.GlobalTintColor = new Color(_light.color.r, _light.color.g, _light.color.b, proFlare.GlobalTintColor.a);
        
    }

}

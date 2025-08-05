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
using UnityEngine.EventSystems;

[AddComponentMenu("BoneCracker Games/BCG Shared Assets Pro/UI/BCG UI Interaction Button")]
public class BCG_UIInteractionButton : MonoBehaviour, IPointerClickHandler {

    public void OnPointerClick(PointerEventData eventData) {

#if BCG_ENTEREXIT

        if (BCG_EnterExitManager.Instance.activePlayer != null)
            BCG_EnterExitManager.Instance.Interact();

#endif

    }

}

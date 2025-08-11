using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingButtonBinder : MonoBehaviour {
    public CraftingStation station;
    public Button craftButton;
    public TMP_Text hintText; // опционально, если используешь TextMeshPro

    void Update(){
        if (!station || !craftButton) return;

        bool can = station.CanCraft();
        craftButton.interactable = can;

        if (hintText){
            hintText.text = can ? "" : station.GetMissingText();
        }
    }
}
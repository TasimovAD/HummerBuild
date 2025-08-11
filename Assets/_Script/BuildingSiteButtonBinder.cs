using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingSiteButtonBinder : MonoBehaviour {
    public BuildingSite site;
    public Button startButton;
    public TMP_Text hintText; // опционально, если используешь TMP

    void Update() {
        if (!site || !startButton) return;
        bool can = site.CanStartCurrent();
        startButton.interactable = can;

        if (hintText) {
            hintText.text = can ? "" : site.GetMissingNeedsText();
        }
    }
}

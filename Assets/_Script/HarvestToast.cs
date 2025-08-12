using UnityEngine;
using UnityEngine.UI;

public class HarvestToast : MonoBehaviour {
    public Text uiText;       // UI элемент для текста
    public float showTime = 2f;

    private float _timer;

    void Update(){
        if (_timer > 0) {
            _timer -= Time.deltaTime;
            if (_timer <= 0 && uiText != null) uiText.gameObject.SetActive(false);
        }
    }

    public void Show(string message){
        if (uiText != null) {
            uiText.text = message;
            uiText.gameObject.SetActive(true);
        }
        _timer = showTime;
    }
}

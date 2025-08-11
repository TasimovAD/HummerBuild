using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CraftingStation : MonoBehaviour {
    [Header("Где лежат входы и куда положим выход")]
    public StorageInventory storage;

    [Header("Выход рецепта")]
    public ResourceType outType;   // например, mix
    public int outAmount = 10;

    [Header("Входы рецепта (что списать из storage перед стартом)")]
    public ResourceNeed[] inputs;  // например: cement10, sand10, water10

    [Header("Время крафта и прогресс")]
    public float craftTime = 3f;
    public Slider progressBar;

    [Header("Сигналы/эффекты (опц.)")]
    public AudioSource audioSource;
    public AudioClip startClip;
    public AudioClip doneClip;
    public Animator animator;      // если есть анимация мешалки
    public string animBoolParam = "IsCrafting"; // bool-параметр в Animator

    private bool _busy;

    /// <summary> Можно ли начать крафт прямо сейчас? </summary>
    public bool CanCraft(){
        if (_busy) return false;
        if (!storage || !outType) return false;
        if (inputs != null){
            foreach (var n in inputs){
                if (!n.type || storage.Inventory.GetAmount(n.type) < n.amount) return false;
            }
        }
        return true;
    }

    /// <summary> Текст с недостающими ресурсами (для UI-подсказки). </summary>
    public string GetMissingText(){
        if (_busy || !storage) return "";
        if (inputs == null || inputs.Length == 0) return "";

        var sb = new StringBuilder();
        foreach (var n in inputs){
            if (!n.type) continue;
            int have = storage.Inventory.GetAmount(n.type);
            if (have < n.amount){
                sb.AppendLine($"{n.type.displayName}: не хватает {n.amount - have}");
            }
        }
        return sb.ToString();
    }

    /// <summary> Вызывай по кнопке "Смешать". </summary>
    public void StartCraft(){
        if (!CanCraft()){
            Debug.LogWarning("[CraftingStation] Нельзя начать крафт. " + GetMissingText());
            return;
        }

        // Списываем входы
        if (inputs != null){
            foreach (var n in inputs){
                storage.Inventory.Remove(n.type, n.amount);
            }
        }

        // Запускаем процесс
        StartCoroutine(CraftRoutine());
    }

    private IEnumerator CraftRoutine(){
        _busy = true;

        // SFX/анимация старта
        if (audioSource && startClip) audioSource.PlayOneShot(startClip);
        if (animator && !string.IsNullOrEmpty(animBoolParam)) animator.SetBool(animBoolParam, true);

        float t = 0f;
        float dur = Mathf.Max(0.1f, craftTime);
        while (t < dur){
            t += Time.deltaTime;
            if (progressBar) progressBar.value = t / dur;
            yield return null;
        }

        // Готово — кладём результат
        storage.Inventory.Add(outType, outAmount);

        // SFX готово
        if (audioSource && doneClip) audioSource.PlayOneShot(doneClip);
        if (animator && !string.IsNullOrEmpty(animBoolParam)) animator.SetBool(animBoolParam, false);

        if (progressBar) progressBar.value = 0f;
        _busy = false;
    }
}

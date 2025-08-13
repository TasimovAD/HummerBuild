// TopBarBridge.cs
using UnityEngine;
using TMPro;

public class TopBarBridge : MonoBehaviour
{
    [Header("Источник данных")]
    public StorageInventory storage;        // StorageInventory ГЛАВНОГО склада

    [Header("Ресурсы (legacy SO)")]
    public ScriptableObject resLog;
    public ScriptableObject resCement;
    public ScriptableObject resSand;
    public ScriptableObject resWater;
    public ScriptableObject resStone;
    public ScriptableObject resMix;

    [Header("UI")]
    public TMP_Text txtLog;
    public TMP_Text txtCement;
    public TMP_Text txtSand;
    public TMP_Text txtWater;
    public TMP_Text txtStone;
    public TMP_Text txtMix;





    [Header("Резервное обновление, сек (0 = выкл)")]
    public float pollInterval = 0.5f;

    void OnEnable()
    {
        if (storage)
        {
            storage.OnChanged += OnStorageChanged;
            Debug.Log($"[TopBarBridge] Bind storage id={storage.GetInstanceID()} name={storage.name}", this);
        }
        RefreshAll();
        if (pollInterval > 0) InvokeRepeating(nameof(RefreshAll), pollInterval, pollInterval);
    }

    void OnDisable()
    {
        if (storage) storage.OnChanged -= OnStorageChanged;
        if (pollInterval > 0) CancelInvoke(nameof(RefreshAll));
    }

    void OnStorageChanged(ScriptableObject res, int delta)
    {
        if (!storage) return;
        if (res == resLog && txtLog)       txtLog.text    = storage.GetAmount(resLog).ToString();
        if (res == resCement && txtCement) txtCement.text = storage.GetAmount(resCement).ToString();
        if (res == resSand && txtSand)     txtSand.text   = storage.GetAmount(resSand).ToString();
        if (res == resWater && txtWater)   txtWater.text   = storage.GetAmount(resWater).ToString();
        if (res == resStone && txtStone)   txtStone.text   = storage.GetAmount(resStone).ToString();
        if (res == resMix && txtMix)       txtMix.text   = storage.GetAmount(resMix).ToString();
    }

    [ContextMenu("Refresh All")]
    public void RefreshAll()
    {
        if (!storage) return;
        if (txtLog)    txtLog.text    = storage.GetAmount(resLog).ToString();
        if (txtCement) txtCement.text = storage.GetAmount(resCement).ToString();
        if (txtSand)   txtSand.text   = storage.GetAmount(resSand).ToString();
        if (txtWater)   txtWater.text   = storage.GetAmount(resWater).ToString();
        if (txtStone)   txtStone.text   = storage.GetAmount(resStone).ToString();
        if (txtMix)       txtMix.text   = storage.GetAmount(resMix).ToString();
    }
}

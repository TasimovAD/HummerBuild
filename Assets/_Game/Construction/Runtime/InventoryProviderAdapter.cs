// Assets/_Game/Construction/Runtime/InventoryProviderAdapter.cs
using UnityEngine;
using System;
using System.Reflection;

public enum KeyMode { ResourceDef, LegacyScriptableObject }
public enum ParamOrder { Key_Int, Int_Key }
public enum ReturnStyle { Auto, ReturnsIntDelta, ReturnsBoolSuccess, ReturnsVoidUseDelta }

public class InventoryProviderAdapter : MonoBehaviour, IInventory
{
    [Header("Реальный инвентарь (старый склад/буфер)")]
    public MonoBehaviour provider; // твой StorageInventory

    [Header("Ключ ресурса")]
    public KeyMode keyMode = KeyMode.LegacyScriptableObject;
    public LegacyResourceMap legacyMap; // обязателен при LegacyScriptableObject

    [Header("Имена методов провайдера (впиши из интроспектора)")]
    public string getMethodName    = "GetAmount";   // пример: Get / GetAmount / QuantityOf ...
    public string addMethodName    = "AddItem";     // пример: Add / AddItem / Deposit ...
    public string removeMethodName = "RemoveItem";  // пример: Remove / Take / Withdraw ...

    [Header("Сигнатуры методов")]
    public ParamOrder addOrder    = ParamOrder.Key_Int;   // (key,int) или (int,key)
    public ParamOrder removeOrder = ParamOrder.Key_Int;
    public ReturnStyle addReturn  = ReturnStyle.Auto;     // что возвращает add/remove
    public ReturnStyle removeReturn = ReturnStyle.Auto;

    public event Action<ResourceDef> OnChanged;

    MethodInfo miGet, miAdd, miRemove;
    Type providerType;
    Type keyType; // тип ключа, который принимает провайдер

    void Awake()
    {
        if (!provider) { Debug.LogError("[Adapter] provider == null", this); return; }
        providerType = provider.GetType();

        // Найдём методы по именам
        miGet    = FindMethod(providerType, getMethodName,    1);
        miAdd    = FindMethod(providerType, addMethodName,    2);
        miRemove = FindMethod(providerType, removeMethodName, 2);

        if (miGet == null || miAdd == null || miRemove == null)
        {
            Debug.LogError($"[Adapter] Не найдены методы: Get={getMethodName} / Add={addMethodName} / Remove={removeMethodName}. Проверь имена.", provider);
            DumpProviderAPI();
            return;
        }

        // Первый параметр Get — это тип ключа
        keyType = miGet.GetParameters()[0].ParameterType;

        Debug.Log($"[Adapter] OK. Using {providerType.Name}: Get={miGet.Name}({keyType.Name}), Add={miAdd.Name}[{addOrder}], Remove={miRemove.Name}[{removeOrder}], returnStyles: add={addReturn}, remove={removeReturn}", this);
    }

    MethodInfo FindMethod(Type t, string name, int paramCount)
    {
        return t.GetMethod(name, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    }

    void DumpProviderAPI()
    {
        var methods = providerType.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        System.Text.StringBuilder sb = new();
        foreach (var m in methods)
        {
            if (m.IsSpecialName) continue;
            var ps = m.GetParameters();
            sb.AppendLine($"{m.Name}({string.Join(", ", Array.ConvertAll(ps, p=>$"{p.ParameterType.Name} {p.Name}"))}) -> {m.ReturnType.Name}");
        }
        Debug.Log("[Adapter] Методы провайдера:\n" + sb.ToString(), provider);
    }

    object ResolveKey(ResourceDef r)
    {
        if (keyMode == KeyMode.ResourceDef) return r;
        if (!legacyMap)
        {
            Debug.LogError("[Adapter] legacyMap не назначен (KeyMode=LegacyScriptableObject).", this);
            return null;
        }
        var legacy = legacyMap.ToLegacy(r);
        if (!legacy)
        {
            Debug.LogError($"[Adapter] В legacyMap нет пары для ресурса '{(r? r.DisplayName : "NULL")}'.", this);
        }
        return legacy;
    }

    int InterpretDelta(object ret, ReturnStyle style, int expected, int before, Func<int> afterGetter)
    {
        if (style == ReturnStyle.ReturnsIntDelta || (style == ReturnStyle.Auto && ret is int))
            return ret is int i ? Mathf.Abs(i) : 0;

        if (style == ReturnStyle.ReturnsBoolSuccess || (style == ReturnStyle.Auto && ret is bool))
        {
            bool ok = ret is bool b && b;
            if (!ok) return 0;
            int after = afterGetter();
            int delta = Mathf.Abs(after - before);
            return delta > 0 ? delta : expected;
        }

        // ReturnsVoidUseDelta ИЛИ Auto с неподходящим типом — меряем по факту
        int after2 = afterGetter();
        int d = Mathf.Abs(after2 - before);
        return d;
    }

    // ====== IInventory ======
    public int Get(ResourceDef r)
    {
        if (!provider || miGet == null) return 0;
        object key = ResolveKey(r);
        if (key == null) return 0;
        var ret = miGet.Invoke(provider, new object[]{ key });
        return ret is int i ? i : 0;
    }

    public int Add(ResourceDef r, int amount)
    {
        if (!provider || miAdd == null || amount <= 0) return 0;
        object key = ResolveKey(r);
        if (key == null) return 0;

        int before = Get(r);

        object[] args = addOrder == ParamOrder.Key_Int
            ? new object[]{ key, amount }
            : new object[]{ amount, key };

        var ret = miAdd.Invoke(provider, args);
        int added = InterpretDelta(ret, addReturn, amount, before, () => Get(r));
        if (added > 0) OnChanged?.Invoke(r);
        return added;
    }

    public int Remove(ResourceDef r, int amount)
    {
        if (!provider || miRemove == null || amount <= 0) return 0;
        object key = ResolveKey(r);
        if (key == null) return 0;

        int before = Get(r);

        object[] args = removeOrder == ParamOrder.Key_Int
            ? new object[]{ key, amount }
            : new object[]{ amount, key };

        var ret = miRemove.Invoke(provider, args);
        int removed = InterpretDelta(ret, removeReturn, amount, before, () => Get(r));
        if (removed > 0) OnChanged?.Invoke(r);
        return removed;
    }
}

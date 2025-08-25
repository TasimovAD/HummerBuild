using UnityEngine;

public class WalletPlayer : MonoBehaviour
{
    [SerializeField] private int _money = 2500;
    public int Money => _money;

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (_money < amount) return false;
        _money -= amount;
        return true;
    }

    public void Add(int amount) => _money += Mathf.Max(0, amount);
}

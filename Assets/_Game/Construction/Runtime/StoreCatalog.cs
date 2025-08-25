using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StorePrice
{
    public ResourceDef Resource;
    public int PricePerUnit = 100;
    public int PackSize = 1; // сколько штук кладём за покупку
}

public class StoreCatalog : MonoBehaviour
{
    public List<StorePrice> Prices = new();

    public StorePrice Get(ResourceDef r) => Prices.Find(p => p.Resource == r);
}

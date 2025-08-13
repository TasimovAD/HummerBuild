// Assets/_HummerBuild/Construction/Runtime/IInventory.cs
using System;
public interface IInventory
{
    int Get(ResourceDef r);
    int Add(ResourceDef r, int amount);      // возвращает фактически добавленное
    int Remove(ResourceDef r, int amount);   // возвращает фактически снятое
    event Action<ResourceDef> OnChanged;
}

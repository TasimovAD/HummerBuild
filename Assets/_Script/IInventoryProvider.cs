public interface IInventoryProvider
{
    Inventory Inventory { get; }
    string ProviderId { get; }
}
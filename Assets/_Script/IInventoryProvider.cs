public interface IInventoryProvider {
    string ProviderId { get; }     // для сейва
    Inventory Inventory { get; }
}

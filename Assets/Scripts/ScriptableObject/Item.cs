using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public Sprite Icon;
    public bool IsStackable;
    public int MaxStack;
    public PickupSpawnerRef PickupSpawnerRef;
}

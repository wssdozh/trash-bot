using UnityEngine;

[CreateAssetMenu(menuName = "Refs/PickupSpawnerRef")]
public class PickupSpawnerRef : ScriptableObject
{
    public PickupSpawner Value { get; private set; }

    public void Set(PickupSpawner spawner)
    {
        Value = spawner;
    }

    public void Clear()
    {
        Value = null;
    }
}
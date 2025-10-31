using UnityEngine;

[CreateAssetMenu(fileName = "DamagePopupSpawnerRef", menuName = "Refs/DamagePopupSpawnerRef")]
public class DamagePopupSpawnerRef : ScriptableObject
{
    public DamagePopupSpawner Value;

    public void Set(DamagePopupSpawner spawner)
    {
        Value = spawner;
    }

    public void Clear()
    {
        Value = null;
    }
}

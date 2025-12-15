using UnityEngine;

public abstract class SpawnerRef<TSpawner> : ScriptableObject where TSpawner : MonoBehaviour
{
    public TSpawner Value { get; private set; }

    public void Set(TSpawner spawner)
    {
        Value = spawner;
    }

    public void Clear()
    {
        Value = null;
    }
}

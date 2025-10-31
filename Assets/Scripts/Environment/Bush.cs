using UnityEngine;

public class Bush : DamageableObject
{
    [SerializeField] private PickupSpawnerRef _berrySpawnerRef;

    protected override void OnDamage()
    {
        PlayShake();
        SpawnBerry();
    }
    
    protected override void OnDeath()
    {
        
    }

    private void SpawnBerry()
    {
        if (_berrySpawnerRef == null)
        {
            return;
        }

        if (_berrySpawnerRef.Value == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
        _berrySpawnerRef.Value.Spawn(spawnPosition);
    }
}

using UnityEngine;
using UnityEngine.SocialPlatforms;

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
        BasePickup berry = _berrySpawnerRef.Value.Spawn(spawnPosition);

        berry.SetAmount(Random.Range(1, 4));

        if (berry.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
        {
            rigidbody.AddForce(Vector3.up * 2f, ForceMode.Impulse);


            rigidbody.AddForce(Vector3.left * Random.Range(-1f, 1f), ForceMode.Impulse);
            rigidbody.AddForce(Vector3.forward * Random.Range(-1f, 1f), ForceMode.Impulse);

        }
    }
}

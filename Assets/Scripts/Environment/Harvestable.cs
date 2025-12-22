using UnityEngine;

public class Harvestable : DamageableObject
{
    [Header("Зависимости")]
    [SerializeField] private PickupSpawnerRef _pickupSpawnerRef;

    [Header("Настройки спавна")]
    [SerializeField] private float _spawnHeightOffset = 0.5f;

    [Header("Импульс")]
    [Header("Ось X")]
    [SerializeField] private float _forceXMin = -1f;
    [SerializeField] private float _forceXMax = 1f;

    [Header("Ось Y")]
    [SerializeField] private float _forceYMin = 1.5f;
    [SerializeField] private float _forceYMax = 2.5f;

    [Header("Ось Z")]
    [SerializeField] private float _forceZMin = -1f;
    [SerializeField] private float _forceZMax = 1f;

    protected override void OnDamage()
    {
        SpawnBerry();
    }

    protected override void OnDeath()
    {
    }

    private void SpawnBerry()
    {
        if (_pickupSpawnerRef == null)
        {
            return;
        }

        if (_pickupSpawnerRef.Value == null)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * _spawnHeightOffset;
        BasePickup berry = _pickupSpawnerRef.Value.Spawn(spawnPosition);

        berry.SetAmount(Random.Range(1, 4));

        if (berry.TryGetComponent<Rigidbody>(out Rigidbody rigidbody) == false)
        {
            return;
        }

        float forceX = Random.Range(_forceXMin, _forceXMax);
        float forceY = Random.Range(_forceYMin, _forceYMax);
        float forceZ = Random.Range(_forceZMin, _forceZMax);

        Vector3 localImpulse = new Vector3(forceX, forceY, forceZ);
        Vector3 worldImpulse = transform.TransformDirection(localImpulse);

        rigidbody.AddForce(worldImpulse, ForceMode.Impulse);
    }
}

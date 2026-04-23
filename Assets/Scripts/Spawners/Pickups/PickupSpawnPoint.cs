using System.Collections;
using UnityEngine;

public class PickupSpawnPoint : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private PickupSpawner _spawner;

    [Header("Spawn Transform")]
    [SerializeField] private Transform _spawnTransform;

    [Header("Initial Spawn")]
    [SerializeField] private InitialSpawnMode _initialSpawnMode = InitialSpawnMode.Immediate;
    [SerializeField] private float _initialDelaySeconds = 1f;

    [Header("Regular Spawn")]
    [SerializeField] private bool _enableRegularSpawn = false;
    [SerializeField] private float _regularIntervalSeconds = 5f;

    private Coroutine _regularSpawnCoroutine;

    private void Start()
    {
        if (_initialSpawnMode == InitialSpawnMode.Immediate)
        {
            SpawnNow();
        }

        if (_initialSpawnMode == InitialSpawnMode.Delayed)
        {
            StartCoroutine(SpawnDelayed());
        }

        if (_enableRegularSpawn)
        {
            StartRegularSpawn();
        }
    }

    public BasePickup SpawnNow()
    {
        Vector3 position = _spawnTransform.position;

        return _spawner.Spawn(position, transform.parent);
    }

    public void StartRegularSpawn()
    {
        if (_regularSpawnCoroutine != null)
        {
            return;
        }

        _regularSpawnCoroutine = StartCoroutine(RegularSpawnLoop());
    }

    public void StopRegularSpawn()
    {
        if (_regularSpawnCoroutine != null)
        {
            StopCoroutine(_regularSpawnCoroutine);
            _regularSpawnCoroutine = null;
        }
    }

    private IEnumerator SpawnDelayed()
    {
        yield return new WaitForSeconds(_initialDelaySeconds);

        SpawnNow();
    }

    private IEnumerator RegularSpawnLoop()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(_regularIntervalSeconds);

        while (enabled)
        {
            yield return waitForSeconds;

            SpawnNow();
        }
    }
}

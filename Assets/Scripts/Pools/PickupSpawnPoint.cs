using System.Collections;
using UnityEngine;

public enum InitialSpawnMode
{
    None = 0,
    Immediate = 1,
    Delayed = 2
}

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

        if (_enableRegularSpawn == true)
        {
            StartRegularSpawn();
        }
    }

    public BasePickup SpawnNow()
    {
        Vector3 position = _spawnTransform.position;

        return _spawner.Spawn(position);
    }

    public void StartRegularSpawn()
    {
        if (_regularSpawnCoroutine == null == false)
        {
            return;
        }

        _regularSpawnCoroutine = StartCoroutine(RegularSpawnLoop());
    }

    public void StopRegularSpawn()
    {
        if (_regularSpawnCoroutine == null == false)
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
        while (true)
        {
            yield return new WaitForSeconds(_regularIntervalSeconds);
            
            SpawnNow();
        }
    }
}

using UnityEngine;

public class DamagePopupOnHealth : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _prefab;

    private DamagePopupSpawner _spawner;
    private float _previousValue;

    private void Start()
    {
        if (_health == null == false)
        {
            _previousValue = _health.Value;
            _health.Changed += OnHealthChanged;
        }

        _spawner = SpawnerServiceLocator.Get<DamagePopup>(_prefab.name) as DamagePopupSpawner;
    }

    private void OnDestroy()
    {
        if (_health == null == false)
        {
            _health.Changed -= OnHealthChanged;
        }
    }

    private void OnHealthChanged()
    {
        float currentValue = _health.Value;
        float damageDelta = _previousValue - currentValue;
        _previousValue = currentValue;

        if (damageDelta > 0f)
        {
            _spawner.Show(damageDelta, _spawnPoint.position);
        }
    }
}

using UnityEngine;

public class DamagePopupOnHealth : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private DamagePopupSpawnerRef _spawner;

    private float _previousValue;

    private void Awake()
    {
        if (_health == null == false)
        {
            _previousValue = _health.Value;
            _health.Changed += OnHealthChanged;
        }
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
            if (_spawner == null == false)
            {
                if (_spawner.Value == null == false)
                {
                    _spawner.Value.Show(damageDelta, _spawnPoint.position);
                }
            }
        }
    }
}

using System;
using UnityEngine;

public class AmmoReturner : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Ammo _ammo;

    private IAmmoSpawner _spawner;

    public event Action Return;

    public void Initialize(IAmmoSpawner spawner)
    {
        _spawner = spawner;
    }

    private void OnEnable()
    {
        _ammo.LifeEnded += OnLifeEnded;
    }

    private void OnDisable()
    {
        _ammo.LifeEnded -= OnLifeEnded;
    }

    private void OnLifeEnded()
    {
        if (_spawner == null == false)
        {
            Return?.Invoke();

            _spawner.Despawn(_ammo);

            return;
        }

        Return?.Invoke();

        _ammo.gameObject.SetActive(false);
    }
}

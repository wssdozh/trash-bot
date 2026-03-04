using System;
using UnityEngine;

public class AmmoReturner : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Ammo _ammo;

    private IAmmoSpawner _spawner;
    private bool _isReturned;

    public Ammo Ammo => _ammo;

    public event Action Returned;

    public void Initialize(IAmmoSpawner spawner)
    {
        _spawner = spawner;
    }

    private void OnEnable()
    {
        _isReturned = false;
    }

    public void ReturnToPool()
    {
        if (_isReturned)
        {
            return;
        }

        _isReturned = true;

        Action returnedAction = Returned;

        if (returnedAction != null)
        {
            returnedAction.Invoke();
        }

        if (_spawner != null)
        {
            _spawner.Despawn(_ammo);

            return;
        }

        _ammo.gameObject.SetActive(false);
    }
}

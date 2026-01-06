using System;
using UnityEngine;

public class BulletReturner : MonoBehaviour
{
    [SerializeField] private Bullet _bullet; 
    private BulletSpawner _spawner;

    public event Action Return;

    public void Initialize(BulletSpawner spawner)
    {
        _spawner = spawner;
    }

    private void OnDisable()
    {
        if (_spawner == null == false)
        { 
            if (gameObject.activeSelf == false)
            {
                _spawner.Despawn(_bullet);
                Return?.Invoke();
            }
        }
    }
}

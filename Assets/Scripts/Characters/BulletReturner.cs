using UnityEngine;

public class BulletReturner : MonoBehaviour
{
    [SerializeField] private Bullet _bullet; 
    private BulletSpawner _spawner;

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
            }
        }
    }
}

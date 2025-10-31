using UnityEngine;

public class PickupReturner : MonoBehaviour
{
    [SerializeField] private BasePickup _pickup;
    private PickupSpawner _spawner;

    private void Awake()
    {
        if (_pickup == null)
        {
            _pickup = GetComponent<BasePickup>();
        }
    }

    public void Initialize(PickupSpawner spawner)
    {
        _spawner = spawner;
    }

    public void ReturnToPool()
    {
        if (_spawner == null == false)
        {
            _spawner.Despawn(_pickup);
        }
    }
}

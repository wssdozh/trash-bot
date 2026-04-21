using UnityEngine;

public class PickupReturner : MonoBehaviour
{
    [SerializeField] private BasePickup _pickup;
    private PickupSpawner _spawner;
    private bool _canReturn = true;

    private void Awake()
    {
        if (_pickup == null)
        {
            _pickup = GetComponent<BasePickup>();
        }
    }

    public void SetSpawner(Spawner spawner)
    {
        _spawner = (PickupSpawner)spawner;
        _canReturn = true;
    }

    public void SetCanReturn(bool canReturn)
    {
        _canReturn = canReturn;
    }

    public void ReturnToPool()
    {
        if (_canReturn == false)
        {
            return;
        }

        if (_spawner != null)
        {
            _spawner.Despawn(_pickup);
        }
        else
        {
            Destroy(this);
        }
    }
}

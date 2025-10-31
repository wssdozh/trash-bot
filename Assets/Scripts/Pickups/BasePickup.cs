using UnityEngine;

public abstract class BasePickup : MonoBehaviour
{
    [SerializeField] protected Collider _triggerCollider;
    protected bool _isPickedUp = false;

    protected virtual void Awake()
    {
        if (_triggerCollider == null)
        {
            _triggerCollider = GetComponent<Collider>();
        }

        if (_triggerCollider != null)
        {
            _triggerCollider.isTrigger = true;
        }
    }

    protected virtual void OnEnable()
    {
        _isPickedUp = false;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        TryPickup(other);
    }

    private void TryPickup(Collider other)
    {
        if (_isPickedUp == false)
        {
            if (other.CompareTag("Player"))
            {
                _isPickedUp = true;
                OnPickup(other.gameObject);
            }
        }
    }

    protected abstract void OnPickup(GameObject player);
}

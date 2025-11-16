using UnityEngine;

public abstract class BaseAnimatedPickup : BasePickup
{
    [SerializeField] protected PickupAnimator _pickupAnimator;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] protected Collider _collider;
    [SerializeField] private PickupReturner _returner;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        _collider.enabled = true;
        _rigidbody.isKinematic = false;
    }

    protected override void OnPickup(GameObject player)
    {
        if (_pickupAnimator == null)
        {
            _pickupAnimator = GetComponent<PickupAnimator>();
        }

        if (_pickupAnimator != null)
        {
            _collider.enabled = false;
            _rigidbody.isKinematic = true;

            _pickupAnimator.PlayAttraction(player.transform, () => OnConsumed(player));
        }
        else
        {
            OnConsumed(player);
        }
    }

    protected virtual void OnConsumed(GameObject player)
    {
        _returner.ReturnToPool();
    }
}

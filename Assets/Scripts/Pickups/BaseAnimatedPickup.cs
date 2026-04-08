using UnityEngine;

public abstract class BaseAnimatedPickup : BasePickup
{
    [SerializeField] protected PickupAnimator _pickupAnimator;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] protected Collider _collider;
    [SerializeField] private PickupReturner _returner;

    [SerializeField] private PickupIdle _pickupIdle;

    protected override void OnEnable()
    {
        base.OnEnable();

        _collider.enabled = true;
        _rigidbody.isKinematic = false;

        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
        {
            pickupIdle.SetIdleActive(true);
        }
    }

    protected virtual void OnDisable()
    {
        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
        {
            pickupIdle.SetIdleActive(false);
        }
    }

    protected override void OnPickup(GameObject player)
    {
        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
        {
            pickupIdle.SetIdleActive(false);
        }

        if (_pickupAnimator == null)
        {
            _pickupAnimator = GetComponent<PickupAnimator>();
        }

        if (_pickupAnimator != null)
        {
            _collider.enabled = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            _pickupAnimator.PlayAttraction(player.transform, () => OnConsumed(player));

            return;
        }

        OnConsumed(player);
    }

    protected virtual void OnConsumed(GameObject player)
    {
        _returner.ReturnToPool();
    }

    private PickupIdle GetPickupIdle()
    {
        if (_pickupIdle == null)
        {
            _pickupIdle = GetComponentInChildren<PickupIdle>(true);
        }

        return _pickupIdle;
    }
}

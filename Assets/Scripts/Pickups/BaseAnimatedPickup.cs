using UnityEngine;

public abstract class BaseAnimatedPickup : BasePickup
{
    [SerializeField] protected PickupAnimator _pickupAnimator;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] protected Collider _collider;


    protected override void Awake()
    {
        base.Awake();

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

    protected abstract void OnConsumed(GameObject player);
}

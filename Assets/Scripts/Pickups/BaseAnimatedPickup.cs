using UnityEngine;

public abstract class BaseAnimatedPickup : BasePickup
{
    [SerializeField] protected PickupAnimator _pickupAnimator;

    protected override void OnPickup(GameObject player)
    {
        if (_pickupAnimator == null)
        {
            _pickupAnimator = GetComponent<PickupAnimator>();
        }

        if (_pickupAnimator != null)
        {
            _pickupAnimator.PlayAttraction(player.transform, () => OnConsumed(player));
        }
        else
        {
            OnConsumed(player);
        }
    }

    protected abstract void OnConsumed(GameObject player);
}

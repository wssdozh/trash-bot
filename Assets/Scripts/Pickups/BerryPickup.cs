using UnityEngine;

public class BerryPickup : BasePickup
{
    [SerializeField] private int _healAmount = 10;
    [SerializeField] private PickupAnimator _pickupAnimator;

    protected override void OnPickup(GameObject player)
    {
        if (_pickupAnimator == null)
        {
            _pickupAnimator = GetComponent<PickupAnimator>();
        }

        if (_pickupAnimator != null)
        {
            _pickupAnimator.PlayAttraction(player.transform, () => Consume(player));
        }
        else
        {
            Consume(player);
        }
    }

    private void Consume(GameObject player)
    {
        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            // playerComponent.AddHealth(_healAmount);
        }

        Destroy(gameObject);
    }
}

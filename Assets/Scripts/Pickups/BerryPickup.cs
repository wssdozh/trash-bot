using UnityEngine;

public class BerryPickup : BaseAnimatedPickup
{
    [SerializeField] private int _healAmount = 10;
    [SerializeField] private PickupReturner _returner;

    protected override void OnConsumed(GameObject player)
    {
        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            // playerComponent.AddHealth(_healAmount);
        }

        _returner.ReturnToPool();
    }
}

using UnityEngine;

public class BerryPickup : BaseAnimatedPickup
{
    protected override void OnConsumed(GameObject player)
    {
        base.OnConsumed(player);

        Player playerComponent = player.GetComponent<Player>();

        if (playerComponent != null)
        {
            // playerComponent.AddHealth(_healAmount);
        }
    }
}

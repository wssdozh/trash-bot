using UnityEngine;

public class BerryPickup : BaseAnimatedPickup
{
    [SerializeField] private int _healAmount = 10;

    protected override void OnConsumed(GameObject player)
    {
        Player playerComponent = player.GetComponent<Player>();
        if (playerComponent != null)
        {
            // playerComponent.AddHealth(_healAmount);
        }

        Destroy(gameObject);
    }
}

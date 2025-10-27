using UnityEngine;

public class Bush : DamageableObject
{
    [SerializeField] private int berriesCount = 3;
    private bool canGiveBerries = true;

    protected override void OnDamage()
    {
        Shake();
        GiveBerry();
    }

    protected override void OnDeath()
    {
        canGiveBerries = false;
    }

    private void GiveBerry()
    {
        if (canGiveBerries == false)
        {
            return;
        }

        if (berriesCount > 0)
        {
            berriesCount -= 1;
            SpawnBerry();
        }

        if (berriesCount <= 0)
        {
            canGiveBerries = false;
        }
    }

    private void SpawnBerry()
    {
        Debug.Log("Куст отдал ягоду");
    }
}

using UnityEngine;

public class Tree : DamageableObject
{
    [SerializeField] private float destroyDelay = 0.3f;

    protected override void OnDamage()
    {
        Shake();
    }

    protected override void OnDeath()
    {
        Destroy(gameObject, destroyDelay);
    }
}

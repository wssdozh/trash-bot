using UnityEngine;

public abstract class DamageableObject : MonoBehaviour
{
    [SerializeField] protected Health Health;

    protected virtual void OnEnable()
    {
        if (Health != null)
        {
            Health.Decreased += OnDamage;
            Health.Ended += OnDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (Health != null)
        {
            Health.Decreased -= OnDamage;
            Health.Ended -= OnDeath;
        }
    }

    protected abstract void OnDamage();
    protected abstract void OnDeath();
}

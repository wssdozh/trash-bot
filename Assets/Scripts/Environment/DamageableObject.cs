using UnityEngine;

public abstract class DamageableObject : MonoBehaviour
{
    [SerializeField] protected Health health;
    [SerializeField] private DamageShakeAnimator _shakeAnimator; 

    protected virtual void OnEnable()
    {
        if (health != null)
        {
            health.Decreased += OnDamage;
            health.Ended += OnDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (health != null)
        {
            health.Decreased -= OnDamage;
            health.Ended -= OnDeath;
        }
    }

    protected void PlayShake()
    {
        if (_shakeAnimator != null)
        {
            _shakeAnimator.Shake();
        }
    }

    protected abstract void OnDamage();
    protected abstract void OnDeath();
}

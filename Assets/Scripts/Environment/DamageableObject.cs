using UnityEngine;

public abstract class DamageableObject : MonoBehaviour
{
    [SerializeField] protected Health Health;
    [SerializeField] private DamageShakeAnimator _shakeAnimator; 

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

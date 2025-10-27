using UnityEngine;
using DG.Tweening;

public abstract class DamageableObject : MonoBehaviour
{
    [SerializeField] protected Health health;
    [SerializeField] protected float shakeStrength = 0.3f;
    [SerializeField] protected int shakeVibrato = 12;
    [SerializeField] protected float shakeDuration = 0.25f;

    protected Tween tween;

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

    protected virtual void OnDestroy()
    {
        if (tween != null && tween.IsActive() == true)
        {
            tween.Kill();
        }
    }

    protected void Shake()
    {
        tween = transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato);
    }

    protected abstract void OnDamage();
    protected abstract void OnDeath();
}

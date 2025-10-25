using UnityEngine;
using DG.Tweening;

public class Tree : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private float shakeStrength = 0.3f;
    [SerializeField] private int shakeVibrato = 12;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float destroyDelay = 0.3f;

    private Tween _tween;

    private void OnEnable()
    {
        if (health != null)
        {
            health.Decreased += DoShake;
            health.Ended += OnDeath;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Decreased -= DoShake;
            health.Ended -= OnDeath;
        }
    }

    private void DoShake()
    {
        _tween = transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato);
    }

    private void OnDeath()
    {
        Destroy(gameObject, destroyDelay);
    }

    private void OnDestroy()
    {
        if (_tween != null && _tween.IsActive() == true)
        {
            _tween.Kill();
        }
    }
}

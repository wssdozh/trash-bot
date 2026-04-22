using System;
using JunkyardBoss;
using UnityEngine;

public sealed class BossSegmentedHealthIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform _phaseOneFill;
    [SerializeField] private RectTransform _phaseTwoFill;
    [SerializeField] private RectTransform _phaseThreeFill;

    private BossExcavator _boss;
    private Health _health;
    private float _phaseOneWidth;
    private float _phaseTwoWidth;
    private float _phaseThreeWidth;

    private void Awake()
    {
        ValidateDependencies();
        CacheWidths();
        ResetSegments();
    }

    private void OnEnable()
    {
        SubscribeHealth();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeHealth();
    }

    public void SetBoss(BossExcavator boss)
    {
        if (_boss == boss)
        {
            return;
        }

        UnsubscribeHealth();
        _boss = boss;
        _health = boss != null ? boss.Health : null;
        SubscribeHealth();
        Refresh();
    }

    public void ClearBoss()
    {
        if (_boss == null && _health == null)
        {
            return;
        }

        UnsubscribeHealth();
        _boss = null;
        _health = null;
        ResetSegments();
    }

    private void Refresh()
    {
        if (_boss == null)
        {
            ResetSegments();

            return;
        }

        float healthRatio = Mathf.Clamp01(_boss.GetHealthRatio());
        float phaseTwoRatio = _boss.Config.PhaseTwoRatio;
        float phaseThreeRatio = _boss.Config.PhaseThreeRatio;

        SetFillWidth(_phaseOneFill, _phaseOneWidth, GetPhaseFill(healthRatio, phaseTwoRatio, 1f));
        SetFillWidth(_phaseTwoFill, _phaseTwoWidth, GetPhaseFill(healthRatio, phaseThreeRatio, phaseTwoRatio));
        SetFillWidth(_phaseThreeFill, _phaseThreeWidth, GetPhaseFill(healthRatio, 0f, phaseThreeRatio));
    }

    private float GetPhaseFill(float healthRatio, float minRatio, float maxRatio)
    {
        if (maxRatio <= minRatio)
        {
            if (healthRatio >= maxRatio)
            {
                return 1f;
            }

            return 0f;
        }

        return Mathf.Clamp01((healthRatio - minRatio) / (maxRatio - minRatio));
    }

    private void SetFillWidth(RectTransform fill, float width, float ratio)
    {
        Vector2 sizeDelta = fill.sizeDelta;
        sizeDelta.x = width * ratio;
        fill.sizeDelta = sizeDelta;
    }

    private void ResetSegments()
    {
        SetFillWidth(_phaseOneFill, _phaseOneWidth, 0f);
        SetFillWidth(_phaseTwoFill, _phaseTwoWidth, 0f);
        SetFillWidth(_phaseThreeFill, _phaseThreeWidth, 0f);
    }

    private void CacheWidths()
    {
        _phaseOneWidth = _phaseOneFill.sizeDelta.x;
        _phaseTwoWidth = _phaseTwoFill.sizeDelta.x;
        _phaseThreeWidth = _phaseThreeFill.sizeDelta.x;
    }

    private void SubscribeHealth()
    {
        if (isActiveAndEnabled == false)
        {
            return;
        }

        if (_health == null)
        {
            return;
        }

        _health.Changed -= Refresh;
        _health.Changed += Refresh;
    }

    private void UnsubscribeHealth()
    {
        if (_health == null)
        {
            return;
        }

        _health.Changed -= Refresh;
    }

    private void ValidateDependencies()
    {
        if (_phaseOneFill == null)
        {
            throw new InvalidOperationException(nameof(_phaseOneFill));
        }

        if (_phaseTwoFill == null)
        {
            throw new InvalidOperationException(nameof(_phaseTwoFill));
        }

        if (_phaseThreeFill == null)
        {
            throw new InvalidOperationException(nameof(_phaseThreeFill));
        }
    }
}

using UnityEngine;
using DG.Tweening;

public abstract class TimeScaleBase
{
    public abstract bool IsAnimating { get; }
    public abstract void Animate(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve, bool isUnscaledUpdate);
    public abstract void Kill(bool complete);
    public abstract void ResetToDefault();
    public abstract void Dispose();
}

public sealed class TimeScale : TimeScaleBase
{
    private readonly float _defaultTimeScale;
    private readonly float _baseFixedDeltaTime;
    private readonly float _minPhysicsTimeScale;

    private Tween _timeScaleTween;

    public TimeScale(float baseFixedDeltaTime, float minPhysicsTimeScale)
    {
        _defaultTimeScale = 1.0f;
        _baseFixedDeltaTime = baseFixedDeltaTime;
        _minPhysicsTimeScale = minPhysicsTimeScale;

        ApplyTimeScale(_defaultTimeScale);
    }

    public override bool IsAnimating
    {
        get
        {
            if (_timeScaleTween == null)
            {
                return false;
            }

            if (_timeScaleTween.IsActive() == false)
            {
                return false;
            }

            return _timeScaleTween.IsPlaying();
        }
    }

    public override void Animate(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve, bool isUnscaledUpdate)
    {
        Kill(false);

        _timeScaleTween = DOTween
            .To(GetTimeScale, SetTimeScale, targetTimeScale, durationSeconds)
            .SetEase(easeCurve)
            .SetUpdate(isUnscaledUpdate)
            .OnComplete(() =>
            {
                ApplyTimeScale(targetTimeScale);
            });
    }

    public override void Kill(bool complete)
    {
        if (_timeScaleTween == null)
        {
            return;
        }

        if (_timeScaleTween.IsActive() == false)
        {
            return;
        }

        _timeScaleTween.Kill(complete);
        _timeScaleTween = null;
    }

    public override void ResetToDefault()
    {
        Kill(false);
        ApplyTimeScale(_defaultTimeScale);
    }

    public override void Dispose()
    {
        Kill(false);
    }

    private float GetTimeScale()
    {
        return Time.timeScale;
    }

    private void SetTimeScale(float value)
    {
        ApplyTimeScale(value);
    }

    private void ApplyTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;

        float physicsTimeScale = timeScale;

        if (physicsTimeScale < _minPhysicsTimeScale)
        {
            physicsTimeScale = _minPhysicsTimeScale;
        }

        Time.fixedDeltaTime = _baseFixedDeltaTime * physicsTimeScale;
    }
}

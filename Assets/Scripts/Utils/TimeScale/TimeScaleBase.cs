using UnityEngine;

public abstract class TimeScaleBase
{
    public abstract bool IsAnimating { get; }
    public abstract void Animate(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve, bool isUnscaledUpdate);
    public abstract void Kill(bool complete);
    public abstract void ResetToDefault();
    public abstract void Dispose();
}

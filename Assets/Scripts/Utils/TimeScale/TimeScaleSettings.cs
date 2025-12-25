using UnityEngine;

public sealed class TimeScaleSettings : MonoBehaviour
{
    [Header("Пауза")]
    [SerializeField] private float _pauseDurationSeconds = 0.45f;
    [SerializeField] private float _resumeDurationSeconds = 0.30f;
    [SerializeField] private float _pausedTimeScale = 0.0f;

    [SerializeField] private AnimationCurve _pauseEaseCurve =
        AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

    [SerializeField] private AnimationCurve _resumeEaseCurve =
        AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

    [Header("Физика")]
    [SerializeField] private float _minPhysicsTimeScale = 0.05f;

    public float PauseDurationSeconds => _pauseDurationSeconds;
    public float ResumeDurationSeconds => _resumeDurationSeconds;
    public float PausedTimeScale => _pausedTimeScale;

    public AnimationCurve PauseEaseCurve => _pauseEaseCurve;
    public AnimationCurve ResumeEaseCurve => _resumeEaseCurve;

    public float MinPhysicsTimeScale => _minPhysicsTimeScale;
}

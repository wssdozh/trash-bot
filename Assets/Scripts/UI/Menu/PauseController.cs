using UnityEngine;
using DG.Tweening;

public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private PauseMenuView _pauseMenuView;
    [SerializeField] private PauseCameraFov _pauseCameraFov;
    [SerializeField] private float _pauseDurationSeconds = 0.45f;
    [SerializeField] private float _resumeDurationSeconds = 0.30f;
    [SerializeField] private float _pausedTimeScale = 0.0f;
    [SerializeField] private PauseMenuNavigation _pauseMenuNavigation;
    [SerializeField] private BlurOverlay _blurOverlay;


    [SerializeField] private float _minPhysicsTimeScale = 0.05f;

    [SerializeField] private AnimationCurve _pauseEaseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField] private AnimationCurve _resumeEaseCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

    private Tween _timeScaleTween;
    private bool _isPaused;

    private float _baseFixedDeltaTime;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        _baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void Pause()
    {
        if (_isPaused == true)
        {
            return;
        }

        if (_pauseMenuView.IsAnimating == true)
        {
            return;
        }

        if (_timeScaleTween != null && _timeScaleTween.IsActive() == true)
        {
            return;
        }

        _isPaused = true;

        AnimateTimeScale(_pausedTimeScale, _pauseDurationSeconds, _pauseEaseCurve);
        _pauseCameraFov.EnterPause();
        _blurOverlay.Show();
        _pauseMenuView.Show();
    }

    public void Resume()
    {
        if (_isPaused == false)
        {
            return;
        }

        if (_pauseMenuView.IsAnimating == true)
        {
            return;
        }

        if (_timeScaleTween != null && _timeScaleTween.IsActive() == true)
        {
            return;
        }

        _isPaused = false;

        _pauseMenuNavigation.CloseSettings();
        _pauseMenuView.Hide();
        _pauseCameraFov.ExitPause();
        _blurOverlay.Hide();
        AnimateTimeScale(1.0f, _resumeDurationSeconds, _resumeEaseCurve);
    }

    private void AnimateTimeScale(float targetTimeScale, float durationSeconds, AnimationCurve easeCurve)
    {
        if (_timeScaleTween != null && _timeScaleTween.IsActive() == true)
        {
            _timeScaleTween.Kill(false);
        }

        _timeScaleTween = DOTween
            .To(() => Time.timeScale, value =>
            {
                Time.timeScale = value;

                float physicsTimeScale = value;
                if (physicsTimeScale < _minPhysicsTimeScale)
                {
                    physicsTimeScale = _minPhysicsTimeScale;
                }

                Time.fixedDeltaTime = _baseFixedDeltaTime * physicsTimeScale;
            }, targetTimeScale, durationSeconds)
            .SetEase(easeCurve)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                Time.timeScale = targetTimeScale;

                float physicsTimeScale = targetTimeScale;
                if (physicsTimeScale < _minPhysicsTimeScale)
                {
                    physicsTimeScale = _minPhysicsTimeScale;
                }

                Time.fixedDeltaTime = _baseFixedDeltaTime * physicsTimeScale;
            });
    }
}

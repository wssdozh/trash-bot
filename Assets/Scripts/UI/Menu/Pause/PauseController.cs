using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PauseController : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PauseMenuView _pauseMenuView;
    [SerializeField] private List<BaseMenuView> _baseMenuViews;
    [SerializeField] private PauseCameraFov _pauseCameraFov;
    [SerializeField] private BlurOverlay _blurOverlay;
    [SerializeField] private TimeScaleSettings _timeScaleSettings;

    private TimeScale _timeScale;
    private bool _isPaused;

    public static PauseController Instance { get; private set; }

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        if (Instance != null && ReferenceEquals(Instance, this) == false)
        {
            throw new InvalidOperationException(nameof(Instance));
        }

        Instance = this;
        _timeScale = new TimeScale(_timeScaleSettings.BaseFixedDeltaTime, _timeScaleSettings.MinPhysicsTimeScale);
    }

    private void OnEnable()
    {
        _pauseMenuView.Opened += Pause;
        _pauseMenuView.Closed += Resume;
    }

    private void OnDisable()
    {
        _pauseMenuView.Opened -= Pause;
        _pauseMenuView.Closed -= Resume;
        _isPaused = false;
        _timeScale.ResetToDefault();
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    public void Pause()
    {
        if (_isPaused)
        {
            return;
        }

        if (_pauseMenuView.IsAnimating || _baseMenuViews.Any(view => view.IsAnimating))
        {
            return;
        }

        if (_timeScale.IsAnimating)
        {
            return;
        }

        _isPaused = true;

        _timeScale.Animate(
            _timeScaleSettings.PausedTimeScale,
            _timeScaleSettings.PauseDurationSeconds,
            _timeScaleSettings.PauseEaseCurve,
            true
        );

        _pauseCameraFov.EnterPause();
        _blurOverlay.Show();
        _pauseMenuView.Show();
    }

    public void PauseTimeOnly()
    {
        if (_isPaused)
        {
            return;
        }

        if (_timeScale.IsAnimating)
        {
            return;
        }

        _timeScale.Animate(
            _timeScaleSettings.PausedTimeScale,
            _timeScaleSettings.PauseDurationSeconds,
            _timeScaleSettings.PauseEaseCurve,
            true
        );
    }

    public void Resume()
    {
        if (_isPaused == false)
        {
            return;
        }

        if (_pauseMenuView.IsAnimating || _baseMenuViews.Any(view => view.IsAnimating))
        {
            return;
        }

        if (_timeScale.IsAnimating)
        {
            return;
        }

        _isPaused = false;

        _baseMenuViews.ForEach(view => view.Hide());
        _pauseMenuView.Hide();
        _pauseCameraFov.ExitPause();
        _blurOverlay.Hide();

        _timeScale.Animate(
            1.0f,
            _timeScaleSettings.ResumeDurationSeconds,
            _timeScaleSettings.ResumeEaseCurve,
            true
        );
    }

    public void ResumeTimeOnly()
    {
        if (_isPaused)
        {
            return;
        }

        if (_timeScale.IsAnimating)
        {
            return;
        }

        _timeScale.Animate(
            1.0f,
            _timeScaleSettings.ResumeDurationSeconds,
            _timeScaleSettings.ResumeEaseCurve,
            true
        );
    }
}

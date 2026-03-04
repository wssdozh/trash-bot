using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        _timeScale = new TimeScale(Time.fixedDeltaTime, _timeScaleSettings.MinPhysicsTimeScale);
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
}

using UnityEngine;
using DG.Tweening;

public sealed class PauseCameraFov : MonoBehaviour
{
    [SerializeField] private Camera _camera;

    [SerializeField] private float _pausedFovMultiplier = 0.80f;
    [SerializeField] private float _pauseDurationSeconds = 0.30f;
    [SerializeField] private float _resumeDurationSeconds = 0.22f;

    [SerializeField] private Ease _pauseEase = Ease.InOutCubic;
    [SerializeField] private Ease _resumeEase = Ease.InOutCubic;

    private Tween _tween;

    private float _baseFov;
    private float _currentFov;
    private bool _forceApply;

    private void Awake()
    {
        _baseFov = _camera.fieldOfView;
        _currentFov = _baseFov;
        _forceApply = false;
    }

    private void LateUpdate()
    {
        if (_forceApply == false)
        {
            return;
        }

        _camera.fieldOfView = _currentFov;
    }

    public void EnterPause()
    {
        KillTween();

        _forceApply = true;

        float targetFov = _baseFov * _pausedFovMultiplier;

        _tween = DOTween
            .To(() => _currentFov, value => _currentFov = value, targetFov, _pauseDurationSeconds)
            .SetEase(_pauseEase)
            .SetUpdate(true);
    }

    public void ExitPause()
    {
        KillTween();

        float targetFov = _baseFov;

        _tween = DOTween
            .To(() => _currentFov, value => _currentFov = value, targetFov, _resumeDurationSeconds)
            .SetEase(_resumeEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _currentFov = _baseFov;
                _forceApply = false;
            });
    }

    private void KillTween()
    {
        if (_tween != null && _tween.IsActive() == true)
        {
            _tween.Kill(false);
        }

        _tween = null;
    }
}

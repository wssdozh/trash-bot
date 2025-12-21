using DG.Tweening;
using UnityEngine;

public sealed class DirectionalLightRotation : MonoBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private Vector3 _rotationAxis = Vector3.right;
    [SerializeField] private float _degreesPerSecond = 10f;
    [SerializeField] private bool _isActive = true;

    private Tween _rotationTween;
    private Quaternion _startRotation;

    private void OnEnable()
    {
        if (_isActive == false)
        {
            return;
        }

        StartRotation();
    }

    private void OnDisable()
    {
        StopRotation();
    }

    public void SetActive(bool isActive)
    {
        _isActive = isActive;

        if (_isActive == false)
        {
            StopRotation();
            return;
        }

        StartRotation();
    }

    public void SetDegreesPerSecond(float degreesPerSecond)
    {
        _degreesPerSecond = degreesPerSecond;

        if (_isActive == false)
        {
            return;
        }

        StartRotation();
    }

    public void SetRotationAxis(Vector3 rotationAxis)
    {
        _rotationAxis = rotationAxis;

        if (_isActive == false)
        {
            return;
        }

        StartRotation();
    }

    private void StartRotation()
    {
        StopRotation();

        _startRotation = _light.transform.rotation;

        float duration = 360f / Mathf.Max(_degreesPerSecond, 0.0001f);

        _rotationTween = DOTween.To(
                () => 0f,
                angle => _light.transform.rotation = Quaternion.AngleAxis(angle, _rotationAxis) * _startRotation,
                360f,
                duration
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .OnStepComplete(() => _startRotation = _light.transform.rotation);
    }

    private void StopRotation()
    {
        if (_rotationTween == null)
        {
            return;
        }

        _rotationTween.Kill();
        _rotationTween = null;
    }
}

using DG.Tweening;
using UnityEngine;

public class InventorySlotHighlight : MonoBehaviour
{
    [SerializeField] private RectTransform _targetTransform;
    [SerializeField] private float _activeScale = 1.15f;
    [SerializeField] private float _duration = 0.15f;

    private Tween _scaleTween;
    private Vector3 _defaultScale;

    private void Awake()
    {
        if (_targetTransform == null)
        {
            _targetTransform = GetComponent<RectTransform>();
        }

        _defaultScale = _targetTransform.localScale;
    }

    public void SetActive(bool isActive)
    {
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
        }

        Vector3 targetScale = _defaultScale;

        if (isActive)
        {
            targetScale = _defaultScale * _activeScale;
        }

        _scaleTween = _targetTransform.DOScale(targetScale, _duration);
    }
}

using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

[RequireComponent(typeof(RectTransform))]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro _text;
    [SerializeField] private float _moveYDistance = 1.5f;
    [SerializeField] private float _duration = 1f;
    [SerializeField] private float _scaleUpValue = 1.3f;
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _randomOffsetX = 0.3f;
    [SerializeField] private float _randomOffsetY = 0.1f;

    public event Action<DamagePopup> Completed;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _text.alpha = 1f;
    }

    public void Setup(float damage)
    {
        _text.text = damage.ToString("0");
        _text.alpha = 1f;

        Vector3 position = _rectTransform.position;
        position.x += UnityEngine.Random.Range(-_randomOffsetX, _randomOffsetX);
        position.y += UnityEngine.Random.Range(0f, _randomOffsetY);
        _rectTransform.position = position;

        float targetY = position.y + _moveYDistance;
        _rectTransform.DOMoveY(targetY, _duration).SetEase(Ease.OutCubic);
        _rectTransform.DOScale(_scaleUpValue, _duration * 0.3f).SetLoops(2, LoopType.Yoyo);
        _text.DOFade(0f, _fadeDuration).SetDelay(_duration - _fadeDuration).OnComplete(NotifyCompleted);
    }

    private void NotifyCompleted()
    {
        _text.text = string.Empty;

        if (Completed == null == false)
        {
            Completed.Invoke(this);
        }
    }
}

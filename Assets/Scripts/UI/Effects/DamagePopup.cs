using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

[RequireComponent(typeof(RectTransform))]
public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    [SerializeField] private float _moveYDistance = 1.5f;
    [SerializeField] private float _moveXDistance = 0.12f;

    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _scaleUpValue = 1.25f;

    [SerializeField] private float _fadeDuration = 0.18f;

    [SerializeField] private float _randomOffsetX = 0.10f;
    [SerializeField] private float _randomOffsetY = 0.06f;

    public event Action<DamagePopup> Completed;

    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _baseScale = _rectTransform.localScale;
        _text.alpha = 1f;
    }

    private void OnDisable()
    {
        if (_sequence != null)
        {
            _sequence.Kill();
        }
    }

    public void Setup(float damage)
    {
        if (_sequence == null == false)
        {
            _sequence.Kill();
        }

        _text.text = damage.ToString("0");
        _text.alpha = 1f;
        _rectTransform.localScale = _baseScale;

        Vector3 startPosition = _rectTransform.position;
        startPosition.x += UnityEngine.Random.Range(-_randomOffsetX, _randomOffsetX);
        startPosition.y += UnityEngine.Random.Range(0f, _randomOffsetY);
        _rectTransform.position = startPosition;

        float horizontalMoveOffset = UnityEngine.Random.Range(-_moveXDistance, _moveXDistance);
        Vector3 endPosition = startPosition + new Vector3(horizontalMoveOffset, _moveYDistance, 0f);

        float fadeDelay = _duration - _fadeDuration;
        if (fadeDelay < 0f)
        {
            fadeDelay = 0f;
        }

        _sequence = DOTween.Sequence();
        _sequence.Join(_rectTransform.DOMove(endPosition, _duration).SetEase(Ease.OutCubic));
        _sequence.Join(_rectTransform.DOScale(_baseScale * _scaleUpValue, _duration * 0.3f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad));
        _sequence.Insert(fadeDelay, _text.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad));
        _sequence.OnComplete(NotifyCompleted);
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

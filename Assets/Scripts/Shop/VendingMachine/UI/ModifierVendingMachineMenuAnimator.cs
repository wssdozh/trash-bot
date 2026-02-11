using System;
using DG.Tweening;
using UnityEngine;

public sealed class ModifierVendingMachineMenuAnimator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _root;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _panelTransform;

    [Header("Cards")]
    [SerializeField] private ModifierOfferCardAnimator[] _cardAnimators;

    [Header("Open")]
    [SerializeField] private float _panelFadeDurationSeconds = 0.16f;
    [SerializeField] private float _panelScaleFrom = 0.98f;
    [SerializeField] private float _panelScaleDurationSeconds = 0.18f;
    [SerializeField] private float _cardStaggerSeconds = 0.06f;

    [Header("Close")]
    [SerializeField] private float _closeFadeDurationSeconds = 0.12f;
    [SerializeField] private float _closeCardStaggerSeconds = 0.06f;

    [Header("Time")]
    [SerializeField] private bool _ignoreTimeScale = true;

    private Sequence _sequence;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_panelTransform == null)
        {
            _panelTransform = GetComponent<RectTransform>();
        }
    }

    public void PlayOpen()
    {
        KillSequence();

        _root.SetActive(true);

        _canvasGroup.alpha = 0f;
        _panelTransform.localScale = new Vector3(_panelScaleFrom, _panelScaleFrom, 1f);

        for (int i = 0; i < _cardAnimators.Length; i++)
        {
            ModifierOfferCardAnimator cardAnimator = _cardAnimators[i];

            if (cardAnimator == null)
            {
                continue;
            }

            cardAnimator.SetHiddenInstant();
        }

        _sequence = DOTween.Sequence();
        _sequence.SetUpdate(_ignoreTimeScale);

        _sequence.Append(_canvasGroup.DOFade(1f, _panelFadeDurationSeconds).SetEase(Ease.OutCubic));
        _sequence.Join(_panelTransform.DOScale(1f, _panelScaleDurationSeconds).SetEase(Ease.OutCubic));

        int revealedCount = 0;

        for (int i = 0; i < _cardAnimators.Length; i++)
        {
            ModifierOfferCardAnimator cardAnimator = _cardAnimators[i];

            if (cardAnimator == null)
            {
                continue;
            }

            if (revealedCount > 0)
            {
                _sequence.AppendInterval(_cardStaggerSeconds);
            }

            ModifierOfferCardAnimator capturedCardAnimator = cardAnimator;

            _sequence.AppendCallback(() => capturedCardAnimator.PlayReveal(0f));

            revealedCount++;
        }
    }

    public void PlayClose(Action onClosed)
    {
        KillSequence();

        _sequence = DOTween.Sequence();
        _sequence.SetUpdate(_ignoreTimeScale);

        int hiddenCount = 0;

        for (int i = 0; i < _cardAnimators.Length; i++)
        {
            ModifierOfferCardAnimator cardAnimator = _cardAnimators[i];

            if (cardAnimator == null)
            {
                continue;
            }

            if (hiddenCount > 0)
            {
                _sequence.AppendInterval(_closeCardStaggerSeconds);
            }

            ModifierOfferCardAnimator capturedCardAnimator = cardAnimator;

            _sequence.AppendCallback(() => capturedCardAnimator.PlayHide(0f));

            hiddenCount++;
        }

        _sequence.Append(_canvasGroup.DOFade(0f, _closeFadeDurationSeconds).SetEase(Ease.InCubic));

        _sequence.OnComplete(() =>
        {
            _root.SetActive(false);

            if (onClosed != null)
            {
                onClosed.Invoke();
            }
        });
    }

    private void KillSequence()
    {
        if (_sequence == null)
        {
            return;
        }

        _sequence.Kill();
        _sequence = null;
    }
}

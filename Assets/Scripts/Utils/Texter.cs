using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Texter : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private CanvasGroup _roomClearCanvasGroup;
    [SerializeField] private RectTransform _roomClearTransform;
    [SerializeField] private Text _roomClearText;
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private string _roomClearMessage = "\u041A\u043E\u043C\u043D\u0430\u0442\u0430 \u0437\u0430\u0447\u0438\u0449\u0435\u043D\u0430";
    [SerializeField] private float _roomClearEnterOffset = 70f;
    [SerializeField] private float _roomClearHideOffset = 24f;
    [SerializeField] private float _roomClearShowDuration = 0.35f;
    [SerializeField] private float _roomClearHoldDuration = 1.25f;
    [SerializeField] private float _roomClearHideDuration = 0.4f;
    [SerializeField] private float _roomClearHiddenScale = 0.92f;

    private Sequence _roomClearSequence;
    private Vector2 _roomClearShownPos;

    private void Awake()
    {
        if (_text == null)
            throw new InvalidOperationException(nameof(_text));

        if (HasRoomClearView())
        {
            _roomClearShownPos = _roomClearTransform.anchoredPosition;
            ApplyRoomClearHidden();
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(_text.gameObject);
        
        if (HasRoomClearView())
        {
            DOTween.Kill(_roomClearCanvasGroup.gameObject);
        }

        KillRoomClearSequence();

        if (HasRoomClearView())
        {
            ApplyRoomClearHidden();
        }
    }

    public void Show(string message)
    {
        DOTween.Kill(_text.gameObject);

        if (_text.gameObject.activeSelf == false)
        {
            _text.gameObject.SetActive(true);
            SetTextAlpha(_text, 0f);
        }

        _text.text = message;

        _text
            .DOFade(1f, _fadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_text.gameObject);
    }

    public void ShowRoomClear()
    {
        if (HasRoomClearView() == false)
            return;

        KillRoomClearSequence();
        DOTween.Kill(_roomClearCanvasGroup.gameObject);
        _roomClearTransform.SetAsLastSibling();

        _roomClearText.text = _roomClearMessage;
        _roomClearCanvasGroup.alpha = 0f;
        _roomClearCanvasGroup.blocksRaycasts = false;
        _roomClearCanvasGroup.interactable = false;
        _roomClearTransform.anchoredPosition = GetRoomClearEnterPos();
        _roomClearTransform.localScale = new Vector3(_roomClearHiddenScale, _roomClearHiddenScale, 1f);

        _roomClearSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLink(_roomClearCanvasGroup.gameObject)
            .Append(_roomClearCanvasGroup.DOFade(1f, _roomClearShowDuration).SetEase(Ease.OutQuad))
            .Join(_roomClearTransform.DOAnchorPos(_roomClearShownPos, _roomClearShowDuration).SetEase(Ease.OutCubic))
            .Join(_roomClearTransform.DOScale(1f, _roomClearShowDuration).SetEase(Ease.OutBack))
            .AppendInterval(_roomClearHoldDuration)
            .Append(_roomClearCanvasGroup.DOFade(0f, _roomClearHideDuration).SetEase(Ease.InQuad))
            .Join(_roomClearTransform.DOAnchorPos(GetRoomClearHidePos(), _roomClearHideDuration).SetEase(Ease.InCubic))
            .Join(_roomClearTransform.DOScale(_roomClearHiddenScale, _roomClearHideDuration).SetEase(Ease.InQuad))
            .OnComplete(OnRoomClearHidden);
    }

    public bool CanShowRoomClear()
    {
        return HasRoomClearView();
    }

    public void Hide()
    {
        DOTween.Kill(_text.gameObject);

        if (_text.gameObject.activeSelf == false)
            return;

        if (GetTextAlpha(_text) <= 0f)
        {
            _text.gameObject.SetActive(false);

            return;
        }

        _text
            .DOFade(0f, _fadeDuration)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true)
            .SetLink(_text.gameObject)
            .OnComplete(OnPromptHidden);
    }

    public void Clear()
    {
        if (_text == null)
            return;

        _text.text = string.Empty;
        _text.gameObject.SetActive(false);

        DOTween.Kill(_text.gameObject);

        if (_roomClearCanvasGroup != null)
            DOTween.Kill(_roomClearCanvasGroup.gameObject);

        KillRoomClearSequence();

        if (_roomClearCanvasGroup != null)
            ApplyRoomClearHidden();
    }

    private void OnPromptHidden()
    {
        _text.gameObject.SetActive(false);
    }

    private void OnRoomClearHidden()
    {
        ApplyRoomClearHidden();
        _roomClearSequence = null;
    }

    private void ApplyRoomClearHidden()
    {
        _roomClearCanvasGroup.alpha = 0f;
        _roomClearCanvasGroup.blocksRaycasts = false;
        _roomClearCanvasGroup.interactable = false;
        _roomClearTransform.anchoredPosition = GetRoomClearEnterPos();
        _roomClearTransform.localScale = new Vector3(_roomClearHiddenScale, _roomClearHiddenScale, 1f);
        _roomClearText.text = string.Empty;
    }

    private Vector2 GetRoomClearEnterPos()
    {
        return _roomClearShownPos + new Vector2(0f, _roomClearEnterOffset);
    }

    private Vector2 GetRoomClearHidePos()
    {
        return _roomClearShownPos + new Vector2(0f, _roomClearHideOffset);
    }

    private void SetTextAlpha(Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private float GetTextAlpha(Text text)
    {
        return text.color.a;
    }

    private bool HasRoomClearView()
    {
        return _roomClearCanvasGroup != null && _roomClearTransform != null && _roomClearText != null;
    }

    private void KillRoomClearSequence()
    {
        if (_roomClearSequence != null && _roomClearSequence.IsActive())
            _roomClearSequence.Kill(false);

        _roomClearSequence = null;
    }
}

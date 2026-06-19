using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsHeaderActionsView : MonoBehaviour
{
    private const float ButtonWidth = 148.0f;
    private const float ButtonHeight = 48.0f;
    private const float ButtonFontSize = 20.0f;
    private const float ButtonFontMinSize = 12.0f;
    private const int LeftPadding = 24;
    private const int RightPadding = 24;
    private const int Spacing = 12;

    [SerializeField] private RectTransform _header;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private Color _panelColor = new Color32(0x23, 0x24, 0x23, 0xFF);

    private ScrollRect _scrollRect;
    private float _headerHeight;

    private void Awake()
    {
        ValidateReference(_header, nameof(_header));
        ValidateReference(_resetButton, nameof(_resetButton));
        CacheScrollRect();

        PrepareHeader();
        MoveHeaderToPanel();
        MoveViewportBelowHeader();
        MoveButton(_resetButton);

        if (_backButton != null)
        {
            MoveButton(_backButton);
        }
    }

    private void ValidateReference(Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }

    private void CacheScrollRect()
    {
        _scrollRect = GetComponent<ScrollRect>();

        if (_scrollRect == null)
        {
            throw new MissingComponentException(nameof(ScrollRect));
        }
    }

    private void PrepareHeader()
    {
        Image panelImage = GetComponent<Image>();

        if (panelImage == null)
        {
            throw new MissingComponentException(nameof(Image));
        }

        panelImage.color = _panelColor;

        Image headerImage = _header.GetComponent<Image>();

        if (headerImage != null)
        {
            headerImage.enabled = false;
        }

        HorizontalLayoutGroup layoutGroup = _header.GetComponent<HorizontalLayoutGroup>();

        if (layoutGroup == null)
        {
            throw new MissingComponentException(nameof(HorizontalLayoutGroup));
        }

        layoutGroup.padding = new RectOffset(LeftPadding, RightPadding, 0, 0);
        layoutGroup.spacing = Spacing;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        _headerHeight = GetHeaderHeight();
    }

    private float GetHeaderHeight()
    {
        LayoutElement layoutElement = _header.GetComponent<LayoutElement>();

        if (layoutElement != null && layoutElement.preferredHeight > 0.0f)
        {
            return layoutElement.preferredHeight;
        }

        return _header.rect.height;
    }

    private void MoveHeaderToPanel()
    {
        _header.SetParent(transform, false);
        _header.SetAsLastSibling();

        _header.anchorMin = new Vector2(0.0f, 1.0f);
        _header.anchorMax = new Vector2(1.0f, 1.0f);
        _header.pivot = new Vector2(0.5f, 1.0f);
        _header.anchoredPosition = Vector2.zero;
        _header.sizeDelta = new Vector2(0.0f, _headerHeight);
    }

    private void MoveViewportBelowHeader()
    {
        RectTransform viewport = _scrollRect.viewport;

        if (viewport == null)
        {
            throw new MissingReferenceException(nameof(_scrollRect.viewport));
        }

        viewport.offsetMax = new Vector2(viewport.offsetMax.x, -_headerHeight);
    }

    private void MoveButton(Button button)
    {
        RectTransform rectTransform = button.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            throw new MissingComponentException(nameof(RectTransform));
        }

        Transform oldParent = rectTransform.parent;
        rectTransform.SetParent(_header, false);
        rectTransform.SetAsLastSibling();
        DisableEmptyParent(oldParent);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = ButtonWidth;
        layoutElement.preferredWidth = ButtonWidth;
        layoutElement.minHeight = ButtonHeight;
        layoutElement.preferredHeight = ButtonHeight;
        layoutElement.flexibleWidth = 0.0f;
        layoutElement.flexibleHeight = 0.0f;

        TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText == null)
        {
            throw new MissingComponentException(nameof(TMP_Text));
        }

        buttonText.fontSize = ButtonFontSize;
        buttonText.enableAutoSizing = true;
        buttonText.fontSizeMin = ButtonFontMinSize;
        buttonText.fontSizeMax = ButtonFontSize;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.textWrappingMode = TextWrappingModes.NoWrap;
        buttonText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void DisableEmptyParent(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        if (parent == _header)
        {
            return;
        }

        if (parent.childCount > 0)
        {
            return;
        }

        parent.gameObject.SetActive(false);
    }
}

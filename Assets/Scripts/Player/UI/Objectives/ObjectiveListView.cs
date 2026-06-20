using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ObjectiveListView : MonoBehaviour
{
    private const string RootName = "ObjectiveList";
    private const string TitleName = "ObjectiveTitle";
    private const string ArrowName = "ObjectiveTitleArrow";
    private const string ArrowLeftStrokeName = "ObjectiveTitleArrowLeft";
    private const string ArrowRightStrokeName = "ObjectiveTitleArrowRight";
    private const string StepName = "ObjectiveStep";
    private const float RootWidth = 400f;
    private const float StepWidth = 352f;
    private const float RootTopOffset = -28f;
    private const float RootRightOffset = -28f;
    private const float RootScale = 1f;
    private const float ItemHeight = 48f;
    private const float ExpandedItemHeight = 92f;
    private const float TitleHeight = 52f;
    private const float ItemSpacing = 8f;
    private const float TextHorizontalMargin = 18f;
    private const float TextTopMargin = 8f;
    private const float DescriptionTopMargin = 34f;
    private const float TitleFontSize = 21f;
    private const float StepFontSize = 18f;
    private const float DescriptionFontSize = 14f;
    private const float ArrowSize = 34f;
    private const float ArrowStrokeWidth = 16f;
    private const float ArrowStrokeHeight = 3f;
    private const float ArrowStrokeOffset = 5f;
    private const float ArrowRightMargin = 22f;
    private const float ArrowTextReserve = 58f;
    private const float ArrowLeftRotation = -45f;
    private const float ArrowRightRotation = 45f;
    private const float ArrowExpandedRotation = 0f;
    private const float ArrowCollapsedRotation = 90f;
    private const float MinFontSize = 12f;
    private const int VisibleStepCount = 5;
    private const float ScrollTweenDuration = 0.18f;
    private const float ScrollWheelStep = 0.18f;
    private const float ScrollViewportHeight = ItemHeight * VisibleStepCount + ItemSpacing * (VisibleStepCount - 1);
    private const float AppearDuration = 0.18f;
    private const float HideDuration = 0.14f;
    private const float MoveDuration = 0.28f;
    private const float CompleteDuration = 0.18f;
    private const float HoverDuration = 0.16f;
    private const float CollapseDuration = 0.18f;
    private const float ReplaceDelay = 0.05f;
    private const float AppearScale = 0.96f;
    private const float CompletedScale = 0.98f;

    private static readonly Color s_titleColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color s_stepColor = new Color(1f, 1f, 1f, 0.88f);
    private static readonly Color s_completedColor = new Color(0.62f, 0.92f, 0.48f, 0.9f);

    private readonly List<ObjectiveItemView> _items = new List<ObjectiveItemView>(8);
    private readonly List<ObjectiveStepViewData> _currentSteps = new List<ObjectiveStepViewData>(8);
    private readonly Dictionary<int, ObjectiveItemView> _stepItems = new Dictionary<int, ObjectiveItemView>(8);
    private readonly Dictionary<ObjectiveItemHoverHandler, ObjectiveItemView> _hoverItems = new Dictionary<ObjectiveItemHoverHandler, ObjectiveItemView>(8);
    private RectTransform _root;
    private RectTransform _viewport;
    private RectTransform _content;
    private VerticalLayoutGroup _layoutGroup;
    private ContentSizeFitter _contentSizeFitter;
    private VerticalLayoutGroup _stepsLayoutGroup;
    private ContentSizeFitter _stepsContentSizeFitter;
    private ScrollRect _scrollRect;
    private Button _itemTemplate;
    private ObjectiveProfile _currentProfile;
    private string _currentTitle;
    private ObjectiveItemView _titleItem;
    private ObjectiveItemClickHandler _titleClickHandler;
    private RectTransform _titleArrow;
    private Sequence _sequence;
    private Tween _scrollTween;
    private readonly HashSet<int> _completedStepIndices = new HashSet<int>();
    private bool _areStepsCollapsed;

    public void Initialize(RectTransform uiRoot, Button itemTemplate)
    {
        if (uiRoot == null)
        {
            throw new InvalidOperationException(nameof(uiRoot));
        }

        if (itemTemplate == null)
        {
            throw new InvalidOperationException(nameof(itemTemplate));
        }

        _itemTemplate = itemTemplate;
        BuildRoot(uiRoot);
        Hide();
    }

    public void Render(
        ObjectiveProfile profile,
        string title,
        IReadOnlyList<ObjectiveStepViewData> steps,
        IReadOnlyCollection<int> completedStepIndices)
    {
        if (_root == null)
        {
            throw new InvalidOperationException(nameof(_root));
        }

        if (profile == null)
        {
            Hide();

            return;
        }

        if (steps == null)
        {
            throw new InvalidOperationException(nameof(steps));
        }

        if (completedStepIndices == null)
        {
            throw new InvalidOperationException(nameof(completedStepIndices));
        }

        if (_root.gameObject.activeSelf == false || _currentProfile != profile || HasStepStructureChanges(steps))
        {
            ReplaceProfile(profile, title, steps, completedStepIndices);

            return;
        }

        UpdateContent(title, steps);

        if (HasCompletedStepChanges(completedStepIndices) == false)
        {
            return;
        }

        AnimateProgress(completedStepIndices);
    }

    public void Hide()
    {
        if (_root == null)
        {
            return;
        }

        KillSequence(false);

        if (_items.Count == 0)
        {
            _root.gameObject.SetActive(false);

            return;
        }

        _sequence = DOTween.Sequence();

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            _sequence.Join(item.BuildHideTween(HideDuration, AppearScale));
        }

        _sequence.OnComplete(OnHidden);
    }

    private void OnDestroy()
    {
        KillScrollTween();
        KillSequence(false);
        ClearItems();
    }

    private void BuildRoot(RectTransform uiRoot)
    {
        GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
        rootObject.transform.SetParent(uiRoot, false);

        _root = rootObject.GetComponent<RectTransform>();
        _root.anchorMin = new Vector2(1f, 1f);
        _root.anchorMax = new Vector2(1f, 1f);
        _root.pivot = new Vector2(1f, 1f);
        _root.anchoredPosition = new Vector2(RootRightOffset, RootTopOffset);
        _root.sizeDelta = new Vector2(RootWidth, 0f);
        _root.localScale = Vector3.one * RootScale;

        _layoutGroup = rootObject.AddComponent<VerticalLayoutGroup>();
        _layoutGroup.childAlignment = TextAnchor.UpperRight;
        _layoutGroup.childControlWidth = false;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.childForceExpandWidth = false;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.spacing = ItemSpacing;

        _contentSizeFitter = rootObject.AddComponent<ContentSizeFitter>();
        _contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        BuildScrollArea(rootObject.transform);
    }

    private void BuildScrollArea(Transform parent)
    {
        GameObject viewportObject = new GameObject("ObjectiveScrollViewport", typeof(RectTransform));
        viewportObject.transform.SetParent(parent, false);
        _viewport = viewportObject.GetComponent<RectTransform>();
        _viewport.anchorMin = new Vector2(1f, 1f);
        _viewport.anchorMax = new Vector2(1f, 1f);
        _viewport.pivot = new Vector2(1f, 1f);
        _viewport.sizeDelta = new Vector2(RootWidth, ScrollViewportHeight);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;
        viewportObject.AddComponent<RectMask2D>();
        AddScrollEventTrigger(viewportObject);

        LayoutElement viewportLayoutElement = viewportObject.AddComponent<LayoutElement>();
        viewportLayoutElement.minWidth = RootWidth;
        viewportLayoutElement.preferredWidth = RootWidth;
        viewportLayoutElement.minHeight = ScrollViewportHeight;
        viewportLayoutElement.preferredHeight = ScrollViewportHeight;

        GameObject contentObject = new GameObject("ObjectiveScrollContent", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        _content = contentObject.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(1f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = Vector2.zero;

        _stepsLayoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        _stepsLayoutGroup.childAlignment = TextAnchor.UpperRight;
        _stepsLayoutGroup.childControlWidth = false;
        _stepsLayoutGroup.childControlHeight = true;
        _stepsLayoutGroup.childForceExpandWidth = false;
        _stepsLayoutGroup.childForceExpandHeight = false;
        _stepsLayoutGroup.spacing = ItemSpacing;

        _stepsContentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        _stepsContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        _stepsContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect = _root.gameObject.AddComponent<ScrollRect>();
        _scrollRect.viewport = _viewport;
        _scrollRect.content = _content;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia = true;
        _scrollRect.scrollSensitivity = 0f;
    }

    private void AddScrollEventTrigger(GameObject targetObject)
    {
        EventTrigger eventTrigger = targetObject.AddComponent<EventTrigger>();
        EventTrigger.Entry scrollEntry = new EventTrigger.Entry();
        scrollEntry.eventID = EventTriggerType.Scroll;
        scrollEntry.callback.AddListener(OnViewportScrolled);
        eventTrigger.triggers.Add(scrollEntry);
    }

    private void ReplaceProfile(
        ObjectiveProfile profile,
        string title,
        IReadOnlyList<ObjectiveStepViewData> steps,
        IReadOnlyCollection<int> completedStepIndices)
    {
        KillSequence(false);

        if (_items.Count == 0 || _root.gameObject.activeSelf == false)
        {
            RenderInitial(profile, title, steps, completedStepIndices);

            return;
        }

        _sequence = DOTween.Sequence();

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            _sequence.Join(item.BuildHideTween(HideDuration, AppearScale));
        }

        _sequence.AppendInterval(ReplaceDelay);
        _sequence.OnComplete(() => RenderInitial(profile, title, steps, completedStepIndices));
    }

    private void RenderInitial(
        ObjectiveProfile profile,
        string title,
        IReadOnlyList<ObjectiveStepViewData> steps,
        IReadOnlyCollection<int> completedStepIndices)
    {
        KillSequence(false);
        ClearItems();

        _currentProfile = profile;
        _currentTitle = title;
        _areStepsCollapsed = false;
        CopySteps(steps);
        CopyCompletedSteps(completedStepIndices);
        _root.gameObject.SetActive(true);

        _titleItem = CreateItem(
            _root,
            TitleName,
            title,
            string.Empty,
            RootWidth,
            TitleHeight,
            TitleHeight,
            TitleFontSize,
            FontStyles.Bold,
            s_titleColor);
        AddTitleClickHandler(_titleItem);
        CreateTitleArrow(_titleItem);
        CreateStepItems();
        ApplyOrder();
        ScrollToTop();
        PlayAppear();
    }

    private void CreateStepItems()
    {
        for (int stepIndex = 0; stepIndex < _currentSteps.Count; stepIndex++)
        {
            ObjectiveStepViewData step = _currentSteps[stepIndex];

            if (string.IsNullOrWhiteSpace(step.Text))
            {
                continue;
            }

            ObjectiveItemView item = CreateItem(
                _content,
                StepName,
                step.Text,
                step.Description,
                StepWidth,
                ItemHeight,
                ExpandedItemHeight,
                StepFontSize,
                FontStyles.Normal,
                s_stepColor);

            _stepItems.Add(stepIndex, item);
        }
    }

    private ObjectiveItemView CreateItem(
        Transform parent,
        string itemName,
        string text,
        string description,
        float width,
        float height,
        float expandedHeight,
        float fontSize,
        FontStyles fontStyle,
        Color textColor)
    {
        Button button = Instantiate(_itemTemplate, parent);
        button.name = itemName;
        button.enabled = false;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.raycastTarget = string.IsNullOrWhiteSpace(description) == false;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.sizeDelta = new Vector2(width, height);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = width;
        layoutElement.preferredWidth = width;
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label == null)
        {
            throw new MissingComponentException(nameof(TMP_Text));
        }

        label.text = text;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.enableAutoSizing = true;
        label.fontSizeMin = MinFontSize;
        label.fontSizeMax = fontSize;
        label.color = textColor;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = new Vector4(TextHorizontalMargin, 0f, TextHorizontalMargin, 0f);
        label.raycastTarget = false;
        StretchText(label);

        TMP_Text descriptionLabel = CreateDescriptionLabel(button.transform, label, description);
        ObjectiveItemView item = new ObjectiveItemView(
            button,
            image,
            rectTransform,
            layoutElement,
            label,
            descriptionLabel,
            GetCanvasGroup(button),
            height,
            expandedHeight,
            string.IsNullOrWhiteSpace(description) == false);

        _items.Add(item);
        AddHoverHandler(item, description);

        return item;
    }

    private void AddTitleClickHandler(ObjectiveItemView item)
    {
        _titleClickHandler = item.Button.gameObject.AddComponent<ObjectiveItemClickHandler>();
        _titleClickHandler.Clicked += OnTitleClicked;
        item.SetPointerEnabled(true);
    }

    private void CreateTitleArrow(ObjectiveItemView item)
    {
        item.SetTextRightMargin(TextHorizontalMargin + ArrowTextReserve);

        GameObject arrowObject = new GameObject(ArrowName, typeof(RectTransform));
        arrowObject.transform.SetParent(item.Button.transform, false);
        _titleArrow = arrowObject.GetComponent<RectTransform>();
        _titleArrow.anchorMin = new Vector2(1f, 0.5f);
        _titleArrow.anchorMax = new Vector2(1f, 0.5f);
        _titleArrow.pivot = new Vector2(0.5f, 0.5f);
        _titleArrow.anchoredPosition = new Vector2(-ArrowRightMargin, 0f);
        _titleArrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        _titleArrow.localRotation = Quaternion.Euler(0f, 0f, ArrowExpandedRotation);

        CreateArrowStroke(
            _titleArrow,
            ArrowLeftStrokeName,
            new Vector2(-ArrowStrokeOffset, 0f),
            ArrowLeftRotation);
        CreateArrowStroke(
            _titleArrow,
            ArrowRightStrokeName,
            new Vector2(ArrowStrokeOffset, 0f),
            ArrowRightRotation);
    }

    private void CreateArrowStroke(Transform parent, string strokeName, Vector2 position, float rotation)
    {
        GameObject strokeObject = new GameObject(strokeName, typeof(RectTransform));
        strokeObject.transform.SetParent(parent, false);

        RectTransform rectTransform = strokeObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(ArrowStrokeWidth, ArrowStrokeHeight);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = strokeObject.AddComponent<Image>();
        image.color = s_titleColor;
        image.raycastTarget = false;
    }

    private TMP_Text CreateDescriptionLabel(Transform parent, TMP_Text template, string description)
    {
        TMP_Text descriptionLabel = Instantiate(template, parent);
        descriptionLabel.name = "Description";
        descriptionLabel.text = description;
        descriptionLabel.fontSize = DescriptionFontSize;
        descriptionLabel.fontSizeMax = DescriptionFontSize;
        descriptionLabel.fontSizeMin = MinFontSize;
        descriptionLabel.fontStyle = FontStyles.Normal;
        descriptionLabel.color = s_stepColor;
        descriptionLabel.alpha = 0f;
        descriptionLabel.alignment = TextAlignmentOptions.TopLeft;
        descriptionLabel.enableAutoSizing = true;
        descriptionLabel.textWrappingMode = TextWrappingModes.Normal;
        descriptionLabel.overflowMode = TextOverflowModes.Ellipsis;
        descriptionLabel.raycastTarget = false;

        StretchText(descriptionLabel);
        descriptionLabel.margin = new Vector4(
            TextHorizontalMargin,
            DescriptionTopMargin,
            TextHorizontalMargin,
            0f);

        return descriptionLabel;
    }

    private void StretchText(TMP_Text text)
    {
        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private void AddHoverHandler(ObjectiveItemView item, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        ObjectiveItemHoverHandler hoverHandler = item.Button.gameObject.AddComponent<ObjectiveItemHoverHandler>();
        hoverHandler.PointerEntered += OnItemPointerEntered;
        hoverHandler.PointerExited += OnItemPointerExited;
        _hoverItems.Add(hoverHandler, item);
    }

    private void AnimateProgress(IReadOnlyCollection<int> completedStepIndices)
    {
        KillSequence(true);

        List<int> newCompletedStepIndices = GetNewCompletedStepIndices(completedStepIndices);
        Dictionary<ObjectiveItemView, Vector2> startPositions = CapturePositions();
        CopyCompletedSteps(completedStepIndices);
        ApplyOrder();
        RebuildLayout();
        Dictionary<ObjectiveItemView, Vector2> targetPositions = CapturePositions();

        SetLayoutEnabled(false);

        foreach (KeyValuePair<ObjectiveItemView, Vector2> startPosition in startPositions)
        {
            ObjectiveItemView item = startPosition.Key;
            item.RectTransform.anchoredPosition = startPosition.Value;
        }

        _sequence = DOTween.Sequence();

        foreach (KeyValuePair<ObjectiveItemView, Vector2> targetPosition in targetPositions)
        {
            ObjectiveItemView item = targetPosition.Key;
            _sequence.Join(item.RectTransform.DOAnchorPos(targetPosition.Value, MoveDuration).SetEase(Ease.OutCubic));
        }

        AnimateCompletedItems(newCompletedStepIndices);
        _sequence.OnComplete(OnMoveCompleted);
    }

    private void AnimateCompletedItems(IReadOnlyList<int> completedStepIndices)
    {
        for (int completedIndex = 0; completedIndex < completedStepIndices.Count; completedIndex++)
        {
            int stepIndex = completedStepIndices[completedIndex];

            if (_stepItems.ContainsKey(stepIndex) == false)
            {
                continue;
            }

            ObjectiveItemView item = _stepItems[stepIndex];
            item.SetCompleted(s_completedColor, CompletedScale);
            item.SetHoverEnabled(false);
            _sequence.Join(item.Text.DOColor(s_completedColor, CompleteDuration).SetEase(Ease.OutSine));
            _sequence.Join(item.RectTransform.DOScale(CompletedScale, CompleteDuration).SetEase(Ease.OutBack));
        }
    }

    private Dictionary<ObjectiveItemView, Vector2> CapturePositions()
    {
        Dictionary<ObjectiveItemView, Vector2> positions = new Dictionary<ObjectiveItemView, Vector2>(_items.Count);

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            positions.Add(item, item.RectTransform.anchoredPosition);
        }

        return positions;
    }

    private void ApplyOrder()
    {
        if (_titleItem != null)
        {
            _titleItem.RectTransform.SetSiblingIndex(0);
        }

        if (_viewport != null)
        {
            _viewport.SetSiblingIndex(1);
        }

        int siblingIndex = 0;

        if (_currentProfile == null)
        {
            return;
        }

        for (int stepIndex = 0; stepIndex < _currentProfile.Steps.Count; stepIndex++)
        {
            if (_completedStepIndices.Contains(stepIndex))
            {
                continue;
            }

            siblingIndex = SetStepSibling(stepIndex, siblingIndex, false);
        }

        for (int stepIndex = 0; stepIndex < _currentProfile.Steps.Count; stepIndex++)
        {
            if (_completedStepIndices.Contains(stepIndex) == false)
            {
                continue;
            }

            siblingIndex = SetStepSibling(stepIndex, siblingIndex, true);
        }
    }

    private int SetStepSibling(int stepIndex, int siblingIndex, bool isCompleted)
    {
        if (_stepItems.ContainsKey(stepIndex) == false)
        {
            return siblingIndex;
        }

        ObjectiveItemView item = _stepItems[stepIndex];
        item.RectTransform.SetSiblingIndex(siblingIndex);

        if (isCompleted)
        {
            item.SetCompleted(s_completedColor, CompletedScale);
        }
        else
        {
            item.SetActive(s_stepColor);
            item.SetHoverEnabled(true);
        }

        if (_areStepsCollapsed)
        {
            item.SetListVisible(false, 0f, AppearScale);
        }

        return siblingIndex + 1;
    }

    private void PlayAppear()
    {
        KillSequence(false);
        _sequence = DOTween.Sequence();

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            item.PrepareAppear(AppearScale);
            _sequence.Join(item.BuildAppearTween(AppearDuration));
        }
    }

    private void ClearItems()
    {
        if (_titleArrow != null)
        {
            _titleArrow.DOKill();
        }

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            RemoveClickHandler(item);
            RemoveHoverHandler(item);
            item.KillTweens();

            if (item.Button != null)
            {
                Destroy(item.Button.gameObject);
            }
        }

        _items.Clear();
        _currentSteps.Clear();
        _stepItems.Clear();
        _hoverItems.Clear();
        _titleItem = null;
        _titleClickHandler = null;
        _titleArrow = null;
        _currentProfile = null;
        _currentTitle = null;
        _completedStepIndices.Clear();
        _areStepsCollapsed = false;
    }

    private void RemoveClickHandler(ObjectiveItemView item)
    {
        ObjectiveItemClickHandler clickHandler = item.Button.GetComponent<ObjectiveItemClickHandler>();

        if (clickHandler == null)
        {
            return;
        }

        clickHandler.Clicked -= OnTitleClicked;
    }

    private void RemoveHoverHandler(ObjectiveItemView item)
    {
        ObjectiveItemHoverHandler hoverHandler = item.Button.GetComponent<ObjectiveItemHoverHandler>();

        if (hoverHandler == null)
        {
            return;
        }

        hoverHandler.PointerEntered -= OnItemPointerEntered;
        hoverHandler.PointerExited -= OnItemPointerExited;
        _hoverItems.Remove(hoverHandler);
    }

    private CanvasGroup GetCanvasGroup(Button item)
    {
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = item.gameObject.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    private bool HasStepStructureChanges(IReadOnlyList<ObjectiveStepViewData> steps)
    {
        if (_currentSteps.Count != steps.Count)
        {
            return true;
        }

        for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            ObjectiveStepViewData currentStep = _currentSteps[stepIndex];
            ObjectiveStepViewData nextStep = steps[stepIndex];

            if (string.IsNullOrWhiteSpace(currentStep.Text) != string.IsNullOrWhiteSpace(nextStep.Text))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(currentStep.Description) != string.IsNullOrWhiteSpace(nextStep.Description))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateContent(string title, IReadOnlyList<ObjectiveStepViewData> steps)
    {
        if (_currentTitle != title)
        {
            _currentTitle = title;
            _titleItem.SetText(title);
        }

        for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            ObjectiveStepViewData step = steps[stepIndex];

            if (_stepItems.ContainsKey(stepIndex) == false)
            {
                continue;
            }

            ObjectiveItemView item = _stepItems[stepIndex];
            item.SetText(step.Text);
            item.SetDescription(step.Description);
        }

        CopySteps(steps);
    }

    private bool HasCompletedStepChanges(IReadOnlyCollection<int> completedStepIndices)
    {
        if (_completedStepIndices.Count != completedStepIndices.Count)
        {
            return true;
        }

        foreach (int stepIndex in completedStepIndices)
        {
            if (_completedStepIndices.Contains(stepIndex) == false)
            {
                return true;
            }
        }

        return false;
    }

    private void CopySteps(IReadOnlyList<ObjectiveStepViewData> steps)
    {
        _currentSteps.Clear();

        for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            _currentSteps.Add(steps[stepIndex]);
        }
    }

    private void CopyCompletedSteps(IReadOnlyCollection<int> completedStepIndices)
    {
        _completedStepIndices.Clear();

        foreach (int stepIndex in completedStepIndices)
        {
            _completedStepIndices.Add(stepIndex);
        }
    }

    private List<int> GetNewCompletedStepIndices(IReadOnlyCollection<int> completedStepIndices)
    {
        List<int> newCompletedStepIndices = new List<int>(completedStepIndices.Count);

        foreach (int stepIndex in completedStepIndices)
        {
            if (_completedStepIndices.Contains(stepIndex))
            {
                continue;
            }

            newCompletedStepIndices.Add(stepIndex);
        }

        return newCompletedStepIndices;
    }

    private void OnMoveCompleted()
    {
        SetLayoutEnabled(true);
        RebuildLayout();
    }

    private void OnItemPointerEntered(ObjectiveItemHoverHandler hoverHandler)
    {
        if (_hoverItems.ContainsKey(hoverHandler) == false)
        {
            return;
        }

        ObjectiveItemView item = _hoverItems[hoverHandler];
        item.SetHovered(true, HoverDuration);
        RebuildLayout();
    }

    private void OnItemPointerExited(ObjectiveItemHoverHandler hoverHandler)
    {
        if (_hoverItems.ContainsKey(hoverHandler) == false)
        {
            return;
        }

        ObjectiveItemView item = _hoverItems[hoverHandler];
        item.SetHovered(false, HoverDuration);
        RebuildLayout();
    }

    private void OnTitleClicked(ObjectiveItemClickHandler clickHandler)
    {
        if (clickHandler != _titleClickHandler)
        {
            return;
        }

        SetStepsCollapsed(_areStepsCollapsed == false);
    }

    private void SetStepsCollapsed(bool isCollapsed)
    {
        if (_areStepsCollapsed == isCollapsed)
        {
            return;
        }

        KillSequence(false);
        _areStepsCollapsed = isCollapsed;
        _sequence = DOTween.Sequence();
        AppendTitleArrowTween(_sequence, isCollapsed);

        foreach (KeyValuePair<int, ObjectiveItemView> stepItem in _stepItems)
        {
            Tween tween = stepItem.Value.BuildListVisibilityTween(
                isCollapsed == false,
                CollapseDuration,
                AppearScale);
            _sequence.Join(tween);
        }

        _sequence.OnUpdate(RebuildLayout);
        _sequence.OnComplete(RebuildLayout);
    }

    private void AppendTitleArrowTween(Sequence sequence, bool isCollapsed)
    {
        if (_titleArrow == null)
        {
            return;
        }

        float targetRotation = isCollapsed ? ArrowCollapsedRotation : ArrowExpandedRotation;
        _titleArrow.DOKill();
        sequence.Join(_titleArrow
            .DOLocalRotate(new Vector3(0f, 0f, targetRotation), CollapseDuration)
            .SetEase(Ease.OutCubic));
    }

    private void RebuildLayout()
    {
        if (_content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
    }

    private void ScrollToTop()
    {
        RebuildLayout();
        KillScrollTween();

        if (_scrollRect == null)
        {
            return;
        }

        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void OnViewportScrolled(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;

        if (pointerEventData == null)
        {
            return;
        }

        float scrollDelta = pointerEventData.scrollDelta.y;

        if (Mathf.Abs(scrollDelta) <= Mathf.Epsilon)
        {
            scrollDelta = pointerEventData.scrollDelta.x;
        }

        if (Mathf.Abs(scrollDelta) <= Mathf.Epsilon)
        {
            return;
        }

        SmoothScroll(scrollDelta);
    }

    private void SmoothScroll(float scrollDelta)
    {
        if (_scrollRect == null)
        {
            return;
        }

        if (_content == null)
        {
            return;
        }

        if (_viewport == null)
        {
            return;
        }

        if (_content.rect.height <= _viewport.rect.height + Mathf.Epsilon)
        {
            return;
        }

        float targetPosition = Mathf.Clamp01(
            _scrollRect.verticalNormalizedPosition + scrollDelta * ScrollWheelStep);
        KillScrollTween();
        _scrollTween = DOTween.To(
                () => _scrollRect.verticalNormalizedPosition,
                SetScrollPosition,
                targetPosition,
                ScrollTweenDuration)
            .SetEase(Ease.OutCubic)
            .SetTarget(_scrollRect);
    }

    private void SetScrollPosition(float position)
    {
        _scrollRect.verticalNormalizedPosition = position;
    }

    private void OnHidden()
    {
        ClearItems();

        if (_root != null)
        {
            _root.gameObject.SetActive(false);
        }
    }

    private void SetLayoutEnabled(bool isEnabled)
    {
        if (_layoutGroup != null)
        {
            _layoutGroup.enabled = isEnabled;
        }

        if (_contentSizeFitter != null)
        {
            _contentSizeFitter.enabled = isEnabled;
        }

        if (_stepsLayoutGroup != null)
        {
            _stepsLayoutGroup.enabled = isEnabled;
        }

        if (_stepsContentSizeFitter != null)
        {
            _stepsContentSizeFitter.enabled = isEnabled;
        }
    }

    private void KillSequence(bool isComplete)
    {
        if (_sequence != null && _sequence.IsActive())
        {
            _sequence.Kill(isComplete);
        }

        _sequence = null;
        SetLayoutEnabled(true);
    }

    private void KillScrollTween()
    {
        if (_scrollTween != null && _scrollTween.IsActive())
        {
            _scrollTween.Kill(false);
        }

        _scrollTween = null;
    }

    private sealed class ObjectiveItemView
    {
        public Button Button { get; }
        public Image Image { get; }
        public RectTransform RectTransform { get; }
        public LayoutElement LayoutElement { get; }
        public TMP_Text Text { get; }
        public TMP_Text DescriptionText { get; }
        public CanvasGroup CanvasGroup { get; }
        public float Height { get; }
        public float ExpandedHeight { get; }
        public bool CanHover { get; private set; } = true;

        private readonly bool _hasDescription;
        private float _targetScale = 1f;
        private float _animationAlpha = 1f;

        public ObjectiveItemView(
            Button button,
            Image image,
            RectTransform rectTransform,
            LayoutElement layoutElement,
            TMP_Text text,
            TMP_Text descriptionText,
            CanvasGroup canvasGroup,
            float height,
            float expandedHeight,
            bool hasDescription)
        {
            Button = button;
            Image = image;
            RectTransform = rectTransform;
            LayoutElement = layoutElement;
            Text = text;
            DescriptionText = descriptionText;
            CanvasGroup = canvasGroup;
            Height = height;
            ExpandedHeight = expandedHeight;
            _hasDescription = hasDescription;
        }

        public void PrepareAppear(float scale)
        {
            SetAnimationAlpha(0f);
            RectTransform.localScale = Vector3.one * scale;
        }

        public void SetActive(Color textColor)
        {
            Text.color = textColor;
            _targetScale = 1f;
            RectTransform.localScale = Vector3.one;
            SetHoverEnabled(true);
        }

        public void SetCompleted(Color textColor, float scale)
        {
            Text.color = textColor;
            _targetScale = scale;
            RectTransform.localScale = Vector3.one * scale;
            SetHoverEnabled(false);
        }

        public void SetHoverEnabled(bool isEnabled)
        {
            if (isEnabled == false)
            {
                CanHover = true;
                SetHovered(false, 0f);
            }

            CanHover = isEnabled;
            SetPointerEnabled(CanHover && _hasDescription);
        }

        public void SetHovered(bool isHovered, float duration)
        {
            if (CanHover == false)
            {
                return;
            }

            float targetHeight = isHovered ? ExpandedHeight : Height;
            float descriptionAlpha = isHovered ? 1f : 0f;
            SetTextExpanded(isHovered);
            LayoutElement.DOKill();
            DescriptionText.DOKill();

            if (duration <= 0f)
            {
                LayoutElement.minHeight = targetHeight;
                LayoutElement.preferredHeight = targetHeight;
                DescriptionText.alpha = descriptionAlpha;

                return;
            }

            DOTween.To(() => LayoutElement.preferredHeight, SetHeight, targetHeight, duration).SetEase(Ease.OutCubic);
            DescriptionText.DOFade(descriptionAlpha, duration).SetEase(Ease.OutSine);
        }

        public void SetPointerEnabled(bool isEnabled)
        {
            if (Image == null)
            {
                return;
            }

            Image.raycastTarget = isEnabled;
        }

        public void SetText(string text)
        {
            Text.text = text;
        }

        public void SetDescription(string description)
        {
            DescriptionText.text = description;
        }

        public void SetTextRightMargin(float rightMargin)
        {
            Text.margin = new Vector4(TextHorizontalMargin, 0f, rightMargin, 0f);
        }

        public void SetListVisible(bool isVisible, float duration, float hiddenScale)
        {
            if (duration <= 0f)
            {
                ApplyListVisibility(isVisible, hiddenScale);

                return;
            }

            BuildListVisibilityTween(isVisible, duration, hiddenScale);
        }

        public Tween BuildListVisibilityTween(bool isVisible, float duration, float hiddenScale)
        {
            LayoutElement.DOKill();
            CanvasGroup.DOKill();
            RectTransform.DOKill();

            if (duration <= 0f)
            {
                ApplyListVisibility(isVisible, hiddenScale);

                return DOTween.Sequence();
            }

            if (isVisible)
            {
                Button.gameObject.SetActive(true);
                LayoutElement.ignoreLayout = false;
                SetHeight(0f);
                SetAnimationAlpha(0f);
                RectTransform.localScale = Vector3.one * hiddenScale;
                SetPointerEnabled(false);

                Sequence showSequence = DOTween.Sequence();
                showSequence.Join(DOTween.To(() => LayoutElement.preferredHeight, SetHeight, Height, duration)
                    .SetEase(Ease.OutCubic));
                showSequence.Join(DOTween.To(() => _animationAlpha, SetAnimationAlpha, 1f, duration)
                    .SetEase(Ease.OutSine)
                    .SetTarget(CanvasGroup));
                showSequence.Join(RectTransform.DOScale(_targetScale, duration).SetEase(Ease.OutBack));
                showSequence.OnComplete(() => SetPointerEnabled(CanHover && _hasDescription));

                return showSequence;
            }

            SetHovered(false, 0f);
            SetTextExpanded(false);
            DescriptionText.alpha = 0f;
            SetPointerEnabled(false);

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Join(DOTween.To(() => LayoutElement.preferredHeight, SetHeight, 0f, duration)
                .SetEase(Ease.InCubic));
            hideSequence.Join(DOTween.To(() => _animationAlpha, SetAnimationAlpha, 0f, duration)
                .SetEase(Ease.InSine)
                .SetTarget(CanvasGroup));
            hideSequence.Join(RectTransform.DOScale(hiddenScale, duration).SetEase(Ease.InSine));
            hideSequence.OnComplete(DisableLayout);

            return hideSequence;
        }

        private void ApplyListVisibility(bool isVisible, float hiddenScale)
        {
            if (isVisible)
            {
                Button.gameObject.SetActive(true);
                LayoutElement.ignoreLayout = false;
                SetHeight(Height);
                SetAnimationAlpha(1f);
                RectTransform.localScale = Vector3.one * _targetScale;
                SetPointerEnabled(CanHover && _hasDescription);

                return;
            }

            SetHovered(false, 0f);
            SetTextExpanded(false);
            DescriptionText.alpha = 0f;
            SetPointerEnabled(false);
            SetHeight(0f);
            SetAnimationAlpha(0f);
            RectTransform.localScale = Vector3.one * hiddenScale;
            DisableLayout();
        }

        private void DisableLayout()
        {
            LayoutElement.ignoreLayout = true;
            Button.gameObject.SetActive(false);
        }

        private void SetHeight(float height)
        {
            LayoutElement.minHeight = height;
            LayoutElement.preferredHeight = height;
        }

        private void SetTextExpanded(bool isExpanded)
        {
            if (isExpanded)
            {
                Text.alignment = TextAlignmentOptions.TopLeft;
                Text.margin = new Vector4(TextHorizontalMargin, TextTopMargin, TextHorizontalMargin, 0f);

                return;
            }

            Text.alignment = TextAlignmentOptions.MidlineLeft;
            Text.margin = new Vector4(TextHorizontalMargin, 0f, TextHorizontalMargin, 0f);
        }

        public Tween BuildAppearTween(float duration)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(() => _animationAlpha, SetAnimationAlpha, 1f, duration)
                .SetEase(Ease.OutSine)
                .SetTarget(CanvasGroup));
            sequence.Join(RectTransform.DOScale(1f, duration).SetEase(Ease.OutBack));

            return sequence;
        }

        public Tween BuildHideTween(float duration, float scale)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(() => _animationAlpha, SetAnimationAlpha, 0f, duration)
                .SetEase(Ease.InSine)
                .SetTarget(CanvasGroup));
            sequence.Join(RectTransform.DOScale(scale, duration).SetEase(Ease.InSine));

            return sequence;
        }

        public void KillTweens()
        {
            DOTween.Kill(RectTransform);
            DOTween.Kill(CanvasGroup);
            DOTween.Kill(Text);
            DOTween.Kill(DescriptionText);
            DOTween.Kill(LayoutElement);
        }

        private void SetAnimationAlpha(float alpha)
        {
            _animationAlpha = alpha;
            ApplyAlpha();
        }

        private void ApplyAlpha()
        {
            CanvasGroup.alpha = _animationAlpha;
        }
    }
}

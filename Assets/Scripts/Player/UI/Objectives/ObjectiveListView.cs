using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ObjectiveListView : MonoBehaviour
{
    private const string RootName = "ObjectiveList";
    private const string TitleName = "ObjectiveTitle";
    private const string StepName = "ObjectiveStep";
    private const float RootWidth = 430f;
    private const float StepWidth = 382f;
    private const float RootTopOffset = -28f;
    private const float RootRightOffset = -28f;
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
    private const float MinFontSize = 12f;
    private const float AppearDuration = 0.18f;
    private const float HideDuration = 0.14f;
    private const float MoveDuration = 0.28f;
    private const float CompleteDuration = 0.18f;
    private const float HoverDuration = 0.16f;
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
    private VerticalLayoutGroup _layoutGroup;
    private ContentSizeFitter _contentSizeFitter;
    private Button _itemTemplate;
    private ObjectiveProfile _currentProfile;
    private ObjectiveItemView _titleItem;
    private Sequence _sequence;
    private readonly HashSet<int> _completedStepIndices = new HashSet<int>();

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

        if (_root.gameObject.activeSelf == false || _currentProfile != profile || HasStepChanges(steps))
        {
            ReplaceProfile(profile, steps, completedStepIndices);

            return;
        }

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
    }

    private void ReplaceProfile(
        ObjectiveProfile profile,
        IReadOnlyList<ObjectiveStepViewData> steps,
        IReadOnlyCollection<int> completedStepIndices)
    {
        KillSequence(false);

        if (_items.Count == 0 || _root.gameObject.activeSelf == false)
        {
            RenderInitial(profile, steps, completedStepIndices);

            return;
        }

        _sequence = DOTween.Sequence();

        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
            _sequence.Join(item.BuildHideTween(HideDuration, AppearScale));
        }

        _sequence.AppendInterval(ReplaceDelay);
        _sequence.OnComplete(() => RenderInitial(profile, steps, completedStepIndices));
    }

    private void RenderInitial(
        ObjectiveProfile profile,
        IReadOnlyList<ObjectiveStepViewData> steps,
        IReadOnlyCollection<int> completedStepIndices)
    {
        KillSequence(false);
        ClearItems();

        _currentProfile = profile;
        CopySteps(steps);
        CopyCompletedSteps(completedStepIndices);
        _root.gameObject.SetActive(true);

        _titleItem = CreateItem(
            TitleName,
            profile.Title,
            string.Empty,
            RootWidth,
            TitleHeight,
            TitleHeight,
            TitleFontSize,
            FontStyles.Bold,
            s_titleColor);
        CreateStepItems();
        ApplyOrder();
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
        Button button = Instantiate(_itemTemplate, _root);
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
            rectTransform,
            layoutElement,
            label,
            descriptionLabel,
            GetCanvasGroup(button),
            height,
            expandedHeight);

        _items.Add(item);
        AddHoverHandler(item, description);

        return item;
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
        KillSequence(false);

        List<int> newCompletedStepIndices = GetNewCompletedStepIndices(completedStepIndices);
        Dictionary<ObjectiveItemView, Vector2> startPositions = CapturePositions();
        CopyCompletedSteps(completedStepIndices);
        ApplyOrder();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
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

        int siblingIndex = 1;

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
        for (int itemIndex = 0; itemIndex < _items.Count; itemIndex++)
        {
            ObjectiveItemView item = _items[itemIndex];
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
        _currentProfile = null;
        _completedStepIndices.Clear();
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

    private bool HasStepChanges(IReadOnlyList<ObjectiveStepViewData> steps)
    {
        if (_currentSteps.Count != steps.Count)
        {
            return true;
        }

        for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
        {
            ObjectiveStepViewData currentStep = _currentSteps[stepIndex];
            ObjectiveStepViewData nextStep = steps[stepIndex];

            if (currentStep.Text != nextStep.Text)
            {
                return true;
            }

            if (currentStep.Description != nextStep.Description)
            {
                return true;
            }
        }

        return false;
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
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
    }

    private void OnItemPointerEntered(ObjectiveItemHoverHandler hoverHandler)
    {
        if (_hoverItems.ContainsKey(hoverHandler) == false)
        {
            return;
        }

        ObjectiveItemView item = _hoverItems[hoverHandler];
        item.SetHovered(true, HoverDuration);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
    }

    private void OnItemPointerExited(ObjectiveItemHoverHandler hoverHandler)
    {
        if (_hoverItems.ContainsKey(hoverHandler) == false)
        {
            return;
        }

        ObjectiveItemView item = _hoverItems[hoverHandler];
        item.SetHovered(false, HoverDuration);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
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

    private sealed class ObjectiveItemView
    {
        public Button Button { get; }
        public RectTransform RectTransform { get; }
        public LayoutElement LayoutElement { get; }
        public TMP_Text Text { get; }
        public TMP_Text DescriptionText { get; }
        public CanvasGroup CanvasGroup { get; }
        public float Height { get; }
        public float ExpandedHeight { get; }
        public bool CanHover { get; private set; } = true;

        public ObjectiveItemView(
            Button button,
            RectTransform rectTransform,
            LayoutElement layoutElement,
            TMP_Text text,
            TMP_Text descriptionText,
            CanvasGroup canvasGroup,
            float height,
            float expandedHeight)
        {
            Button = button;
            RectTransform = rectTransform;
            LayoutElement = layoutElement;
            Text = text;
            DescriptionText = descriptionText;
            CanvasGroup = canvasGroup;
            Height = height;
            ExpandedHeight = expandedHeight;
        }

        public void PrepareAppear(float scale)
        {
            CanvasGroup.alpha = 0f;
            RectTransform.localScale = Vector3.one * scale;
        }

        public void SetActive(Color textColor)
        {
            Text.color = textColor;
            RectTransform.localScale = Vector3.one;
            SetHoverEnabled(true);
        }

        public void SetCompleted(Color textColor, float scale)
        {
            Text.color = textColor;
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
            sequence.Join(CanvasGroup.DOFade(1f, duration).SetEase(Ease.OutSine));
            sequence.Join(RectTransform.DOScale(1f, duration).SetEase(Ease.OutBack));

            return sequence;
        }

        public Tween BuildHideTween(float duration, float scale)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(CanvasGroup.DOFade(0f, duration).SetEase(Ease.InSine));
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
    }
}

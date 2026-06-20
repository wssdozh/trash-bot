using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class InputBindingSettingsView : MonoBehaviour
{
    private const string MoveActionName = "Move";
    private const string JumpActionName = "Jump";
    private const string SprintActionName = "Sprint";
    private const string AttackActionName = "Attack";
    private const string InteractActionName = "Interact";
    private const string UseItemActionName = "UseItem";
    private const string DropActionName = "Drop";
    private const string DropAllActionName = "DropAll";
    private const string PauseActionName = "Pause";

    private const string UpPartName = "up";
    private const string DownPartName = "down";
    private const string LeftPartName = "left";
    private const string RightPartName = "right";
    private const string KeyboardMouseGroupName = "Keyboard&Mouse";

    private const string SectionName = "InputSection";
    private const string VideoSectionName = "VideoSection";
    private const string DeveloperSectionName = "DeveloperSection";
    private const string SectionTitle = "Управление";
    private const string MoveUpLabel = "Вперёд";
    private const string MoveDownLabel = "Назад";
    private const string MoveLeftLabel = "Влево";
    private const string MoveRightLabel = "Вправо";
    private const string JumpLabel = "Прыжок";
    private const string SprintLabel = "Бег";
    private const string AttackLabel = "Атака";
    private const string InteractLabel = "Взаимодействие";
    private const string UseItemLabel = "Предмет";
    private const string DropLabel = "Выбросить";
    private const string PauseLabel = "Пауза";
    private const string WaitingLabel = "...";

    private const float RowHeight = 56.0f;
    private const float SeparatorHeight = 2.0f;
    private const float LabelWidth = 280.0f;
    private const float ButtonWidth = 164.0f;
    private const float ButtonHeight = 46.0f;
    private const float HeaderFontSize = 24.0f;
    private const float LabelFontSize = 20.0f;
    private const float ButtonFontSize = 20.0f;
    private const float ButtonFontMinSize = 12.0f;
    private const float RowSpacing = 18.0f;

    private static readonly Color s_separatorColor = new Color(1.0f, 1.0f, 1.0f, 0.55f);

    [SerializeField] private Transform _content;
    [SerializeField] private Button _buttonTemplate;

    private readonly BindingButton[] _bindingButtons =
    {
        new BindingButton(MoveUpLabel, MoveActionName, UpPartName),
        new BindingButton(MoveDownLabel, MoveActionName, DownPartName),
        new BindingButton(MoveLeftLabel, MoveActionName, LeftPartName),
        new BindingButton(MoveRightLabel, MoveActionName, RightPartName),
        new BindingButton(JumpLabel, JumpActionName, string.Empty),
        new BindingButton(SprintLabel, SprintActionName, string.Empty),
        new BindingButton(AttackLabel, AttackActionName, string.Empty),
        new BindingButton(InteractLabel, InteractActionName, string.Empty),
        new BindingButton(UseItemLabel, UseItemActionName, string.Empty),
        new BindingButton(DropLabel, DropActionName, string.Empty),
        new BindingButton(PauseLabel, PauseActionName, string.Empty)
    };

    private PlayerInputActions _inputs;
    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private UnityAction[] _bindingClickListeners;
    private TMP_Text _textTemplate;
    private bool _isBound;

    private void Awake()
    {
        ValidateReferences();

        _inputs = new PlayerInputActions();
        PlayerInputBindingOverrideStore.Apply(_inputs);
        _textTemplate = _buttonTemplate.GetComponentInChildren<TMP_Text>(true);

        if (_textTemplate == null)
        {
            throw new MissingComponentException(nameof(TMP_Text));
        }

        BuildView();
        RefreshButtons();
    }

    private void OnEnable()
    {
        PlayerInputBindingOverrideStore.Changed += OnBindingOverridesChanged;
    }

    private void OnDisable()
    {
        PlayerInputBindingOverrideStore.Changed -= OnBindingOverridesChanged;
    }

    private void OnDestroy()
    {
        CancelRebinding();
        Unbind();

        if (_inputs == null)
        {
            return;
        }

        _inputs.Disable();
        _inputs.Dispose();
        _inputs = null;
    }

    private void ValidateReferences()
    {
        ValidateReference(_content, nameof(_content));
        ValidateReference(_buttonTemplate, nameof(_buttonTemplate));
    }

    private void ValidateReference(UnityEngine.Object target, string fieldName)
    {
        if (target == null)
        {
            throw new MissingReferenceException(fieldName);
        }
    }

    private void BuildView()
    {
        _bindingClickListeners = new UnityAction[_bindingButtons.Length];

        RectTransform section = CreateSection();
        CreateHeader(section);
        CreateSeparator(section);

        for (int i = 0; i < _bindingButtons.Length; i++)
        {
            CreateBindingRow(section, i);
        }

        _isBound = true;
    }

    private RectTransform CreateSection()
    {
        RectTransform sectionTemplate = FindSectionTemplate();
        RectTransform section = Instantiate(sectionTemplate, _content);
        section.name = SectionName;
        PlaceSection(section);
        ClearChildren(section);

        return section;
    }

    private void PlaceSection(RectTransform section)
    {
        Transform videoSection = _content.Find(VideoSectionName);

        if (videoSection != null)
        {
            section.SetSiblingIndex(videoSection.GetSiblingIndex() + 1);

            return;
        }

        Transform developerSection = _content.Find(DeveloperSectionName);

        if (developerSection == null)
        {
            section.SetAsLastSibling();
            return;
        }

        section.SetSiblingIndex(developerSection.GetSiblingIndex());
    }

    private RectTransform FindSectionTemplate()
    {
        for (int i = 0; i < _content.childCount; i++)
        {
            Transform child = _content.GetChild(i);
            RectTransform rectTransform = child.GetComponent<RectTransform>();
            Image image = child.GetComponent<Image>();
            VerticalLayoutGroup layoutGroup = child.GetComponent<VerticalLayoutGroup>();

            if (rectTransform != null && image != null && layoutGroup != null)
            {
                return rectTransform;
            }
        }

        throw new MissingReferenceException(nameof(VerticalLayoutGroup));
    }

    private void ClearChildren(Transform parent)
    {
        List<Transform> children = new List<Transform>(parent.childCount);

        for (int i = 0; i < parent.childCount; i++)
        {
            children.Add(parent.GetChild(i));
        }

        for (int i = 0; i < children.Count; i++)
        {
            Destroy(children[i].gameObject);
        }
    }

    private void CreateHeader(Transform parent)
    {
        TextMeshProUGUI headerText = CreateText(SectionTitle, parent);
        headerText.fontSize = HeaderFontSize;
        headerText.fontStyle = FontStyles.Bold;
    }

    private void CreateSeparator(Transform parent)
    {
        GameObject separator = new GameObject("Separator", typeof(RectTransform));
        separator.transform.SetParent(parent, false);

        Image image = separator.AddComponent<Image>();
        image.color = s_separatorColor;

        LayoutElement layoutElement = separator.AddComponent<LayoutElement>();
        layoutElement.minHeight = SeparatorHeight;
        layoutElement.preferredHeight = SeparatorHeight;
    }

    private void CreateBindingRow(Transform parent, int bindingButtonIndex)
    {
        BindingButton bindingButton = _bindingButtons[bindingButtonIndex];
        Transform row = CreateRow(parent, bindingButton.Label + " Row");

        CreateLabel(bindingButton.Label, row);
        CreateFlexibleSpace(row);

        Button keyButton = CreateButton(row, bindingButton.Label, string.Empty);

        _bindingButtons[bindingButtonIndex] = new BindingButton(
            bindingButton.Label,
            bindingButton.ActionName,
            bindingButton.BindingPartName,
            keyButton,
            keyButton.GetComponentInChildren<TMP_Text>(true));

        _bindingClickListeners[bindingButtonIndex] = () => OnBindingClicked(bindingButtonIndex);
        keyButton.onClick.AddListener(_bindingClickListeners[bindingButtonIndex]);
    }

    private Transform CreateRow(Transform parent, string rowName)
    {
        GameObject row = new GameObject(rowName, typeof(RectTransform));
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layoutGroup = row.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.padding = new RectOffset(8, 8, 0, 0);
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = RowSpacing;

        LayoutElement layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.minHeight = RowHeight;
        layoutElement.preferredHeight = RowHeight;
        layoutElement.flexibleWidth = 1.0f;

        return row.transform;
    }

    private void CreateLabel(string text, Transform parent)
    {
        TextMeshProUGUI labelText = CreateText(text, parent);
        labelText.fontSize = LabelFontSize;

        LayoutElement layoutElement = labelText.gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = LabelWidth;
        layoutElement.preferredWidth = LabelWidth;
    }

    private void CreateFlexibleSpace(Transform parent)
    {
        GameObject space = new GameObject("Space", typeof(RectTransform));
        space.transform.SetParent(parent, false);

        LayoutElement layoutElement = space.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1.0f;
    }

    private TextMeshProUGUI CreateText(string text, Transform parent)
    {
        GameObject textObject = new GameObject(text, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = _textTemplate.font;
        textComponent.fontSharedMaterial = _textTemplate.fontSharedMaterial;
        textComponent.color = _textTemplate.color;
        textComponent.fontSize = _textTemplate.fontSize;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;
        textComponent.text = text;

        return textComponent;
    }

    private Button CreateButton(Transform parent, string name, string text)
    {
        Button button = Instantiate(_buttonTemplate, parent);
        button.name = name;

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = ButtonWidth;
        layoutElement.preferredWidth = ButtonWidth;
        layoutElement.minHeight = ButtonHeight;
        layoutElement.preferredHeight = ButtonHeight;

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
        buttonText.text = text;

        return button;
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < _bindingButtons.Length; i++)
        {
            BindingButton bindingButton = _bindingButtons[i];
            InputAction action = GetAction(bindingButton.ActionName);
            int bindingIndex = GetBindingIndex(action, bindingButton.BindingPartName);

            if (bindingButton.ValueText == null)
            {
                throw new MissingReferenceException(nameof(bindingButton.ValueText));
            }

            bindingButton.ValueText.text = InputControlPath.ToHumanReadableString(
                action.bindings[bindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }
    }

    private void OnBindingClicked(int bindingButtonIndex)
    {
        CancelRebinding();
        StartCoroutine(BeginRebinding(bindingButtonIndex));
    }

    private void OnBindingOverridesChanged()
    {
        PlayerInputBindingOverrideStore.Apply(_inputs);
        RefreshButtons();
    }

    private IEnumerator BeginRebinding(int bindingButtonIndex)
    {
        yield return null;

        BindingButton bindingButton = _bindingButtons[bindingButtonIndex];
        InputAction action = GetAction(bindingButton.ActionName);
        int bindingIndex = GetBindingIndex(action, bindingButton.BindingPartName);

        if (bindingButton.ValueText == null)
        {
            throw new MissingReferenceException(nameof(bindingButton.ValueText));
        }

        bindingButton.ValueText.text = WaitingLabel;
        action.Disable();

        _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnComplete(operation => CompleteRebinding(action, bindingIndex, operation))
            .Start();
    }

    private void CompleteRebinding(
        InputAction action,
        int bindingIndex,
        InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        _rebindingOperation = null;

        if (HasDuplicateBinding(action, bindingIndex))
        {
            RejectRebinding(action, bindingIndex);
            return;
        }

        SynchronizeLinkedBinding(action, bindingIndex);
        PlayerInputBindingOverrideStore.Save(_inputs);
        RefreshButtons();
    }

    private bool HasDuplicateBinding(InputAction action, int bindingIndex)
    {
        string bindingPath = action.bindings[bindingIndex].effectivePath;

        for (int i = 0; i < _bindingButtons.Length; i++)
        {
            BindingButton bindingButton = _bindingButtons[i];
            InputAction otherAction = GetAction(bindingButton.ActionName);
            int otherBindingIndex = GetBindingIndex(otherAction, bindingButton.BindingPartName);

            if (otherAction == action && otherBindingIndex == bindingIndex)
            {
                continue;
            }

            if (otherAction.bindings[otherBindingIndex].effectivePath == bindingPath)
            {
                return true;
            }
        }

        return false;
    }

    private void RejectRebinding(InputAction action, int bindingIndex)
    {
        action.RemoveBindingOverride(bindingIndex);
        RefreshButtons();
    }

    private void SynchronizeLinkedBinding(InputAction action, int bindingIndex)
    {
        if (action.name != DropActionName)
        {
            return;
        }

        InputAction dropAllAction = GetAction(DropAllActionName);
        int dropAllBindingIndex = GetBindingIndex(dropAllAction, string.Empty);
        dropAllAction.ApplyBindingOverride(dropAllBindingIndex, action.bindings[bindingIndex].effectivePath);
    }

    private void CancelRebinding()
    {
        if (_rebindingOperation == null)
        {
            return;
        }

        _rebindingOperation.Cancel();
        _rebindingOperation.Dispose();
        _rebindingOperation = null;

        RefreshButtons();
    }

    private void Unbind()
    {
        if (_isBound == false)
        {
            return;
        }

        for (int i = 0; i < _bindingButtons.Length; i++)
        {
            if (_bindingButtons[i].Button != null && _bindingClickListeners[i] != null)
            {
                _bindingButtons[i].Button.onClick.RemoveListener(_bindingClickListeners[i]);
            }
        }

        _isBound = false;
    }

    private InputAction GetAction(string actionName)
    {
        InputAction action = _inputs.asset.FindAction(actionName, true);

        if (action == null)
        {
            throw new InvalidOperationException(actionName);
        }

        return action;
    }

    private int GetBindingIndex(InputAction action, string bindingPartName)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (IsTargetBinding(binding, bindingPartName))
            {
                return i;
            }
        }

        throw new InvalidOperationException(action.name);
    }

    private bool IsTargetBinding(InputBinding binding, string bindingPartName)
    {
        if (string.IsNullOrEmpty(binding.groups))
        {
            return false;
        }

        if (binding.groups.Contains(KeyboardMouseGroupName) == false)
        {
            return false;
        }

        if (string.IsNullOrEmpty(bindingPartName))
        {
            return binding.isComposite == false && binding.isPartOfComposite == false;
        }

        return binding.isPartOfComposite && binding.name == bindingPartName;
    }

    private struct BindingButton
    {
        public readonly string Label;
        public readonly string ActionName;
        public readonly string BindingPartName;
        public readonly Button Button;
        public readonly TMP_Text ValueText;

        public BindingButton(string label, string actionName, string bindingPartName)
            : this(label, actionName, bindingPartName, null, null)
        {
        }

        public BindingButton(
            string label,
            string actionName,
            string bindingPartName,
            Button button,
            TMP_Text valueText)
        {
            Label = label;
            ActionName = actionName;
            BindingPartName = bindingPartName;
            Button = button;
            ValueText = valueText;
        }
    }
}

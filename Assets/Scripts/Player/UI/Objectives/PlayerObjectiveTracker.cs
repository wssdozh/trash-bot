using System;
using System.Collections.Generic;
using JunkyardBoss;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerObjectiveTracker : MonoBehaviour
{
    private const string ProfileResourcesPath = "Objectives";
    private const string ButtonResourcesPath = "Prefabs/UI/Button";
    private const string MoveActionName = "Move";
    private const string AttackActionName = "Attack";
    private const string InteractActionName = "Interact";
    private const string UseItemActionName = "UseItem";
    private const string UpPartName = "up";
    private const string DownPartName = "down";
    private const string LeftPartName = "left";
    private const string RightPartName = "right";
    private const string KeyboardMouseGroupName = "Keyboard&Mouse";
    private const string MoveKeysToken = "{MoveKeys}";
    private const string AttackKeyToken = "{AttackKey}";
    private const string InteractKeyToken = "{InteractKey}";
    private const string UseItemKeyToken = "{UseItemKey}";
    private const string LeftMousePath = "<Mouse>/leftButton";
    private const string RightMousePath = "<Mouse>/rightButton";
    private const string MiddleMousePath = "<Mouse>/middleButton";
    private const string LeftMouseLabel = "\u041b\u041a\u041c";
    private const string RightMouseLabel = "\u041f\u041a\u041c";
    private const string MiddleMouseLabel = "\u0421\u041a\u041c";
    private const float MoveSqrThreshold = 0.01f;

    private readonly List<ObjectiveStepViewData> _stepViewData = new List<ObjectiveStepViewData>(8);
    private readonly HashSet<int> _completedStepIndices = new HashSet<int>();

    private ObjectiveProfile[] _profiles;
    private ObjectiveProfile _currentProfile;
    private ObjectiveListView _view;
    private Player _player;
    private PlayerInputActions _labelInputs;
    private bool _isInitialized;
    private bool _isSubscribed;

    public void Initialize(Player player, RectTransform uiRoot)
    {
        if (player == null)
        {
            throw new InvalidOperationException(nameof(player));
        }

        if (uiRoot == null)
        {
            throw new InvalidOperationException(nameof(uiRoot));
        }

        Button itemTemplate = Resources.Load<Button>(ButtonResourcesPath);

        if (itemTemplate == null)
        {
            throw new MissingReferenceException(ButtonResourcesPath);
        }

        _profiles = Resources.LoadAll<ObjectiveProfile>(ProfileResourcesPath);

        if (_profiles == null)
        {
            throw new MissingReferenceException(ProfileResourcesPath);
        }

        if (_profiles.Length == 0)
        {
            throw new MissingReferenceException(ProfileResourcesPath);
        }

        _player = player;
        _labelInputs = new PlayerInputActions();
        PlayerInputBindingOverrideStore.Apply(_labelInputs);
        _view = gameObject.AddComponent<ObjectiveListView>();
        _view.Initialize(uiRoot, itemTemplate);
        _isInitialized = true;

        Subscribe();
        ShowProfile(RoomType.Start);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        DisposeLabelInputs();
    }

    private void Subscribe()
    {
        if (_isInitialized == false)
        {
            return;
        }

        if (_isSubscribed == true)
        {
            return;
        }

        _player.Moved += OnPlayerMoved;
        _player.AttackStarted += OnPlayerAttackStarted;
        _player.ItemUseRequested += OnPlayerItemUseRequested;
        _player.Interacted += OnPlayerInteracted;
        ObjectiveTarget.Completed += OnObjectiveTargetCompleted;
        PlayerModifierPurchase.Purchased += OnPlayerModifierPurchased;
        RoomCombatLock.Cleared += OnRoomCleared;
        BossExcavator.AnyDied += OnBossDied;
        RoomEnterTrigger.AnyEntered += OnRoomEntered;
        PlayerInputBindingOverrideStore.Changed += OnBindingOverridesChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        _player.Moved -= OnPlayerMoved;
        _player.AttackStarted -= OnPlayerAttackStarted;
        _player.ItemUseRequested -= OnPlayerItemUseRequested;
        _player.Interacted -= OnPlayerInteracted;
        ObjectiveTarget.Completed -= OnObjectiveTargetCompleted;
        PlayerModifierPurchase.Purchased -= OnPlayerModifierPurchased;
        RoomCombatLock.Cleared -= OnRoomCleared;
        BossExcavator.AnyDied -= OnBossDied;
        RoomEnterTrigger.AnyEntered -= OnRoomEntered;
        PlayerInputBindingOverrideStore.Changed -= OnBindingOverridesChanged;
        _isSubscribed = false;
    }

    private void ShowProfile(RoomType roomType)
    {
        ObjectiveProfile profile = FindProfile(roomType);

        if (profile == null)
        {
            _currentProfile = null;
            _view.Hide();

            return;
        }

        _currentProfile = profile;
        _completedStepIndices.Clear();
        RenderProfile();
    }

    private ObjectiveProfile FindProfile(RoomType roomType)
    {
        for (int profileIndex = 0; profileIndex < _profiles.Length; profileIndex++)
        {
            ObjectiveProfile profile = _profiles[profileIndex];

            if (profile == null)
            {
                continue;
            }

            if (profile.RoomType == roomType)
            {
                return profile;
            }
        }

        return null;
    }

    private void TryCompleteSteps(ObjectiveTrigger trigger, string targetId)
    {
        if (_currentProfile == null)
        {
            return;
        }

        if (_currentProfile.Steps == null)
        {
            return;
        }

        bool hasCompletedStep = false;

        for (int stepIndex = 0; stepIndex < _currentProfile.Steps.Count; stepIndex++)
        {
            if (_completedStepIndices.Contains(stepIndex))
            {
                continue;
            }

            ObjectiveStepDefinition step = _currentProfile.Steps[stepIndex];

            if (step == null)
            {
                continue;
            }

            if (CanCompleteStep(step, trigger, targetId) == false)
            {
                continue;
            }

            _completedStepIndices.Add(stepIndex);
            hasCompletedStep = true;
        }

        if (hasCompletedStep)
        {
            RenderProfile();
        }
    }

    private bool CanCompleteStep(ObjectiveStepDefinition step, ObjectiveTrigger trigger, string targetId)
    {
        if (step.Trigger != trigger)
        {
            return false;
        }

        if (trigger == ObjectiveTrigger.ExitRoom)
        {
            return true;
        }

        if (trigger == ObjectiveTrigger.TargetCompleted)
        {
            return string.Equals(step.TargetId, targetId, StringComparison.Ordinal);
        }

        if (trigger == ObjectiveTrigger.Purchase && string.IsNullOrWhiteSpace(step.TargetId) == false)
        {
            return string.Equals(step.TargetId, targetId, StringComparison.Ordinal);
        }

        return true;
    }

    private void RenderProfile()
    {
        if (_currentProfile == null)
        {
            _view.Hide();

            return;
        }

        _view.Render(_currentProfile, BuildStepViewData(_currentProfile), _completedStepIndices);
    }

    private IReadOnlyList<ObjectiveStepViewData> BuildStepViewData(ObjectiveProfile profile)
    {
        _stepViewData.Clear();

        if (profile.Steps == null)
        {
            return _stepViewData;
        }

        for (int stepIndex = 0; stepIndex < profile.Steps.Count; stepIndex++)
        {
            ObjectiveStepDefinition step = profile.Steps[stepIndex];

            if (step == null)
            {
                _stepViewData.Add(new ObjectiveStepViewData(string.Empty, string.Empty));

                continue;
            }

            _stepViewData.Add(new ObjectiveStepViewData(
                FormatObjectiveText(step.Text),
                FormatObjectiveText(step.Description)));
        }

        return _stepViewData;
    }

    private string FormatObjectiveText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text
            .Replace(MoveKeysToken, GetMoveKeys())
            .Replace(AttackKeyToken, GetBindingDisplay(AttackActionName, string.Empty))
            .Replace(InteractKeyToken, GetBindingDisplay(InteractActionName, string.Empty))
            .Replace(UseItemKeyToken, GetBindingDisplay(UseItemActionName, string.Empty));
    }

    private string GetMoveKeys()
    {
        string upKey = GetBindingDisplay(MoveActionName, UpPartName);
        string downKey = GetBindingDisplay(MoveActionName, DownPartName);
        string leftKey = GetBindingDisplay(MoveActionName, LeftPartName);
        string rightKey = GetBindingDisplay(MoveActionName, RightPartName);

        return $"{upKey}/{leftKey}/{downKey}/{rightKey}";
    }

    private string GetBindingDisplay(string actionName, string bindingPartName)
    {
        InputAction action = GetAction(actionName);
        int bindingIndex = GetBindingIndex(action, bindingPartName);
        string bindingPath = action.bindings[bindingIndex].effectivePath;

        return GetHumanBindingLabel(bindingPath);
    }

    private InputAction GetAction(string actionName)
    {
        InputAction action = _labelInputs.asset.FindAction(actionName, true);

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

    private string GetHumanBindingLabel(string bindingPath)
    {
        if (bindingPath == LeftMousePath)
        {
            return LeftMouseLabel;
        }

        if (bindingPath == RightMousePath)
        {
            return RightMouseLabel;
        }

        if (bindingPath == MiddleMousePath)
        {
            return MiddleMouseLabel;
        }

        return InputControlPath.ToHumanReadableString(
            bindingPath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    private void DisposeLabelInputs()
    {
        if (_labelInputs == null)
        {
            return;
        }

        _labelInputs.Disable();
        _labelInputs.Dispose();
        _labelInputs = null;
    }

    private void OnPlayerMoved(Vector2 moveVector)
    {
        if (moveVector.sqrMagnitude <= MoveSqrThreshold)
        {
            return;
        }

        TryCompleteSteps(ObjectiveTrigger.Move, string.Empty);
    }

    private void OnPlayerAttackStarted()
    {
        TryCompleteSteps(ObjectiveTrigger.Attack, string.Empty);
    }

    private void OnPlayerItemUseRequested()
    {
        TryCompleteSteps(ObjectiveTrigger.UseItem, string.Empty);
    }

    private void OnPlayerInteracted(Interactable interactable)
    {
        if (interactable == null)
        {
            return;
        }

        ObjectiveTarget objectiveTarget = interactable.GetComponent<ObjectiveTarget>();

        if (objectiveTarget == null)
        {
            objectiveTarget = interactable.GetComponentInParent<ObjectiveTarget>();
        }

        if (objectiveTarget == null)
        {
            return;
        }

        objectiveTarget.Complete();
    }

    private void OnObjectiveTargetCompleted(string targetId)
    {
        TryCompleteSteps(ObjectiveTrigger.TargetCompleted, targetId);
    }

    private void OnPlayerModifierPurchased(string targetId)
    {
        TryCompleteSteps(ObjectiveTrigger.Purchase, targetId);
    }

    private void OnRoomCleared(RoomCombatLock roomCombatLock)
    {
        if (IsCurrentRoom(roomCombatLock) == false)
        {
            return;
        }

        TryCompleteSteps(ObjectiveTrigger.RoomCleared, string.Empty);
    }

    private void OnBossDied(BossExcavator boss)
    {
        if (IsCurrentRoom(boss) == false)
        {
            return;
        }

        TryCompleteSteps(ObjectiveTrigger.BossDefeated, string.Empty);
    }

    private void OnBindingOverridesChanged()
    {
        PlayerInputBindingOverrideStore.Apply(_labelInputs);
        RenderProfile();
    }

    private void OnRoomEntered(RoomEnterTrigger roomEnterTrigger)
    {
        TryCompleteSteps(ObjectiveTrigger.ExitRoom, string.Empty);

        if (roomEnterTrigger == null)
        {
            return;
        }

        RoomGenerator roomGenerator = roomEnterTrigger.GetComponentInParent<RoomGenerator>();

        if (roomGenerator == null)
        {
            return;
        }

        if (roomGenerator.TryGetRoomType(out RoomType roomType) == false)
        {
            return;
        }

        if (_currentProfile != null && _currentProfile.RoomType == roomType)
        {
            return;
        }

        ShowProfile(roomType);
    }

    private bool IsCurrentRoom(Component component)
    {
        if (component == null)
        {
            return false;
        }

        if (_currentProfile == null)
        {
            return false;
        }

        RoomGenerator roomGenerator = component.GetComponentInParent<RoomGenerator>();

        if (roomGenerator == null)
        {
            return false;
        }

        if (roomGenerator.TryGetRoomType(out RoomType roomType) == false)
        {
            return false;
        }

        return _currentProfile.RoomType == roomType;
    }
}

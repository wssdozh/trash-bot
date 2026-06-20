using System;
using System.Collections.Generic;
using JunkyardBoss;
using UnityEngine;
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
    private const string MoveKeysToken = "{MoveKeys}";
    private const string AttackKeyToken = "{AttackKey}";
    private const string InteractKeyToken = "{InteractKey}";
    private const string UseItemKeyToken = "{UseItemKey}";
    private const string EnemyProgressToken = "{EnemyProgress}";
    private const string MeleeLeftToken = "{MeleeLeft}";
    private const string MeleeTotalToken = "{MeleeTotal}";
    private const string BomberLeftToken = "{BomberLeft}";
    private const string BomberTotalToken = "{BomberTotal}";
    private const string DroneLeftToken = "{DroneLeft}";
    private const string DroneTotalToken = "{DroneTotal}";
    private const string TurretLeftToken = "{TurretLeft}";
    private const string TurretTotalToken = "{TurretTotal}";
    private const string BossPhaseToken = "{BossPhase}";
    private const string EnemyTypeMelee = "melee";
    private const string EnemyTypeBomber = "bomber";
    private const string EnemyTypeDrone = "drone";
    private const string EnemyTypeTurret = "turret";
    private const string AllEnemiesDefeatedText = "все враги убиты";
    private const string EnemyProgressText = "осталось {0}/{1}";
    private const string BossPhaseOneText = "первая фаза";
    private const string BossPhaseTwoText = "вторая фаза";
    private const string BossPhaseThreeText = "третья фаза";
    private const float MoveSqrThreshold = 0.01f;

    private readonly List<ObjectiveStepViewData> _stepViewData = new List<ObjectiveStepViewData>(8);
    private readonly HashSet<int> _completedStepIndices = new HashSet<int>();
    private readonly EnemyObjectiveCounters _enemyCounters = new EnemyObjectiveCounters();

    private ObjectiveProfile[] _profiles;
    private ObjectiveProfile _currentProfile;
    private RoomGenerator _currentRoomGenerator;
    private BossExcavatorPhase _currentBossPhase;
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
        ShowProfile(RoomType.Start, null);
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
        Enemy.AnyDied += OnEnemyDied;
        Turret.AnyDied += OnTurretDied;
        RoomCombatLock.Cleared += OnRoomCleared;
        BossExcavator.AnyPhaseChanged += OnBossPhaseChanged;
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
        Enemy.AnyDied -= OnEnemyDied;
        Turret.AnyDied -= OnTurretDied;
        RoomCombatLock.Cleared -= OnRoomCleared;
        BossExcavator.AnyPhaseChanged -= OnBossPhaseChanged;
        BossExcavator.AnyDied -= OnBossDied;
        RoomEnterTrigger.AnyEntered -= OnRoomEntered;
        PlayerInputBindingOverrideStore.Changed -= OnBindingOverridesChanged;
        _isSubscribed = false;
    }

    private void ShowProfile(RoomType roomType, RoomGenerator roomGenerator)
    {
        ObjectiveProfile profile = FindProfile(roomType);

        if (profile == null)
        {
            _currentProfile = null;
            _currentRoomGenerator = null;
            _view.Hide();

            return;
        }

        _currentProfile = profile;
        _currentRoomGenerator = roomGenerator;
        RefreshRuntimeState();
        _completedStepIndices.Clear();
        CompleteAbsentEnemyTypeSteps();
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

        if (trigger == ObjectiveTrigger.EnemyTypeCleared)
        {
            return string.Equals(step.TargetId, targetId, StringComparison.Ordinal);
        }

        if (trigger == ObjectiveTrigger.BossPhaseReached)
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

        _view.Render(
            _currentProfile,
            FormatObjectiveText(_currentProfile.Title),
            BuildStepViewData(_currentProfile),
            _completedStepIndices);
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

            if (ShouldShowStep(step) == false)
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
            .Replace(UseItemKeyToken, GetBindingDisplay(UseItemActionName, string.Empty))
            .Replace(EnemyProgressToken, GetEnemyProgressText())
            .Replace(MeleeLeftToken, _enemyCounters.MeleeLeft.ToString())
            .Replace(MeleeTotalToken, _enemyCounters.MeleeTotal.ToString())
            .Replace(BomberLeftToken, _enemyCounters.BomberLeft.ToString())
            .Replace(BomberTotalToken, _enemyCounters.BomberTotal.ToString())
            .Replace(DroneLeftToken, _enemyCounters.DroneLeft.ToString())
            .Replace(DroneTotalToken, _enemyCounters.DroneTotal.ToString())
            .Replace(TurretLeftToken, _enemyCounters.TurretLeft.ToString())
            .Replace(TurretTotalToken, _enemyCounters.TurretTotal.ToString())
            .Replace(BossPhaseToken, GetBossPhaseText());
    }

    private bool ShouldShowStep(ObjectiveStepDefinition step)
    {
        if (step.Trigger != ObjectiveTrigger.EnemyTypeCleared)
        {
            return true;
        }

        return GetEnemyTypeTotal(step.TargetId) > 0;
    }

    private string GetEnemyProgressText()
    {
        int total = _enemyCounters.Total;
        int left = _enemyCounters.Left;

        if (total <= 0)
        {
            return AllEnemiesDefeatedText;
        }

        if (left <= 0)
        {
            return AllEnemiesDefeatedText;
        }

        return string.Format(EnemyProgressText, left, total);
    }

    private string GetBossPhaseText()
    {
        if (_currentBossPhase == BossExcavatorPhase.PhaseTwo)
        {
            return BossPhaseTwoText;
        }

        if (_currentBossPhase == BossExcavatorPhase.PhaseThree)
        {
            return BossPhaseThreeText;
        }

        return BossPhaseOneText;
    }

    private int GetEnemyTypeTotal(string targetId)
    {
        if (targetId == EnemyTypeMelee)
        {
            return _enemyCounters.MeleeTotal;
        }

        if (targetId == EnemyTypeBomber)
        {
            return _enemyCounters.BomberTotal;
        }

        if (targetId == EnemyTypeDrone)
        {
            return _enemyCounters.DroneTotal;
        }

        if (targetId == EnemyTypeTurret)
        {
            return _enemyCounters.TurretTotal;
        }

        return 0;
    }

    private void RefreshRuntimeState()
    {
        _enemyCounters.Clear();
        _currentBossPhase = BossExcavatorPhase.PhaseOne;

        if (_currentRoomGenerator == null)
        {
            return;
        }

        RefreshEnemyCounters();
        RefreshBossPhase();
    }

    private void RefreshEnemyCounters()
    {
        Enemy[] enemies = _currentRoomGenerator.GetComponentsInChildren<Enemy>(true);

        for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
        {
            Enemy enemy = enemies[enemyIndex];

            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead)
            {
                continue;
            }

            string enemyType = GetEnemyType(enemy);
            _enemyCounters.Add(enemyType);
        }

        Turret[] turrets = _currentRoomGenerator.GetComponentsInChildren<Turret>(true);

        for (int turretIndex = 0; turretIndex < turrets.Length; turretIndex++)
        {
            Turret turret = turrets[turretIndex];

            if (turret == null)
            {
                continue;
            }

            if (turret.IsDead)
            {
                continue;
            }

            _enemyCounters.Add(EnemyTypeTurret);
        }
    }

    private void RefreshBossPhase()
    {
        BossExcavator[] bosses = _currentRoomGenerator.GetComponentsInChildren<BossExcavator>(true);

        for (int bossIndex = 0; bossIndex < bosses.Length; bossIndex++)
        {
            BossExcavator boss = bosses[bossIndex];

            if (boss == null)
            {
                continue;
            }

            _currentBossPhase = boss.Phase;

            return;
        }
    }

    private void CompleteAbsentEnemyTypeSteps()
    {
        if (_currentProfile == null)
        {
            return;
        }

        if (_currentProfile.Steps == null)
        {
            return;
        }

        for (int stepIndex = 0; stepIndex < _currentProfile.Steps.Count; stepIndex++)
        {
            ObjectiveStepDefinition step = _currentProfile.Steps[stepIndex];

            if (step == null)
            {
                continue;
            }

            if (step.Trigger != ObjectiveTrigger.EnemyTypeCleared)
            {
                continue;
            }

            if (GetEnemyTypeTotal(step.TargetId) > 0)
            {
                continue;
            }

            _completedStepIndices.Add(stepIndex);
        }
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
        return PlayerInputBindingLabel.Get(_labelInputs, actionName, bindingPartName);
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

    private void OnEnemyDied(Enemy enemy)
    {
        if (IsCurrentRoom(enemy) == false)
        {
            return;
        }

        string enemyType = GetEnemyType(enemy);
        _enemyCounters.Remove(enemyType);

        if (_enemyCounters.GetLeft(enemyType) <= 0)
        {
            TryCompleteSteps(ObjectiveTrigger.EnemyTypeCleared, enemyType);

            return;
        }

        RenderProfile();
    }

    private void OnTurretDied(Turret turret)
    {
        if (IsCurrentRoom(turret) == false)
        {
            return;
        }

        _enemyCounters.Remove(EnemyTypeTurret);

        if (_enemyCounters.TurretLeft <= 0)
        {
            TryCompleteSteps(ObjectiveTrigger.EnemyTypeCleared, EnemyTypeTurret);

            return;
        }

        RenderProfile();
    }

    private void OnRoomCleared(RoomCombatLock roomCombatLock)
    {
        if (IsCurrentRoom(roomCombatLock) == false)
        {
            return;
        }

        TryCompleteSteps(ObjectiveTrigger.RoomCleared, string.Empty);
    }

    private void OnBossPhaseChanged(BossExcavator boss, BossExcavatorPhase phase)
    {
        if (IsCurrentRoom(boss) == false)
        {
            return;
        }

        _currentBossPhase = phase;
        TryCompleteSteps(ObjectiveTrigger.BossPhaseReached, phase.ToString());
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

        if (IsActiveProfileRoom(roomGenerator, roomType))
        {
            CompleteCurrentRoomExit();

            return;
        }

        if (_currentProfile != null)
        {
            TryCompleteSteps(ObjectiveTrigger.ExitRoom, string.Empty);
        }

        ShowProfile(roomType, roomGenerator);
    }

    private bool IsActiveProfileRoom(RoomGenerator roomGenerator, RoomType roomType)
    {
        if (_currentRoomGenerator == roomGenerator)
        {
            return true;
        }

        if (_currentRoomGenerator != null)
        {
            return false;
        }

        if (_currentProfile == null)
        {
            return false;
        }

        return _currentProfile.RoomType == roomType;
    }

    private void CompleteCurrentRoomExit()
    {
        TryCompleteSteps(ObjectiveTrigger.ExitRoom, string.Empty);
        _currentProfile = null;
        _currentRoomGenerator = null;
        _completedStepIndices.Clear();
        _enemyCounters.Clear();
        _view.Hide();
    }

    private string GetEnemyType(Enemy enemy)
    {
        if (enemy == null)
        {
            return EnemyTypeMelee;
        }

        EnemyDroneBrain droneBrain = enemy.GetComponentInChildren<EnemyDroneBrain>(true);

        if (droneBrain != null)
        {
            return EnemyTypeDrone;
        }

        EnemySuicideAttack suicideAttack = enemy.GetComponentInChildren<EnemySuicideAttack>(true);

        if (suicideAttack != null)
        {
            return EnemyTypeBomber;
        }

        return EnemyTypeMelee;
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

        if (_currentRoomGenerator != null)
        {
            return _currentRoomGenerator == roomGenerator;
        }

        if (roomGenerator.TryGetRoomType(out RoomType roomType) == false)
        {
            return false;
        }

        return _currentProfile.RoomType == roomType;
    }

    private sealed class EnemyObjectiveCounters
    {
        public int MeleeTotal { get; private set; }
        public int MeleeLeft { get; private set; }
        public int BomberTotal { get; private set; }
        public int BomberLeft { get; private set; }
        public int DroneTotal { get; private set; }
        public int DroneLeft { get; private set; }
        public int TurretTotal { get; private set; }
        public int TurretLeft { get; private set; }

        public int Total => MeleeTotal + BomberTotal + DroneTotal + TurretTotal;
        public int Left => MeleeLeft + BomberLeft + DroneLeft + TurretLeft;

        public void Clear()
        {
            MeleeTotal = 0;
            MeleeLeft = 0;
            BomberTotal = 0;
            BomberLeft = 0;
            DroneTotal = 0;
            DroneLeft = 0;
            TurretTotal = 0;
            TurretLeft = 0;
        }

        public void Add(string enemyType)
        {
            if (enemyType == EnemyTypeMelee)
            {
                MeleeTotal += 1;
                MeleeLeft += 1;

                return;
            }

            if (enemyType == EnemyTypeBomber)
            {
                BomberTotal += 1;
                BomberLeft += 1;

                return;
            }

            if (enemyType == EnemyTypeDrone)
            {
                DroneTotal += 1;
                DroneLeft += 1;

                return;
            }

            if (enemyType == EnemyTypeTurret)
            {
                TurretTotal += 1;
                TurretLeft += 1;
            }
        }

        public void Remove(string enemyType)
        {
            if (enemyType == EnemyTypeMelee)
            {
                MeleeLeft = Mathf.Max(0, MeleeLeft - 1);

                return;
            }

            if (enemyType == EnemyTypeBomber)
            {
                BomberLeft = Mathf.Max(0, BomberLeft - 1);

                return;
            }

            if (enemyType == EnemyTypeDrone)
            {
                DroneLeft = Mathf.Max(0, DroneLeft - 1);

                return;
            }

            if (enemyType == EnemyTypeTurret)
            {
                TurretLeft = Mathf.Max(0, TurretLeft - 1);
            }
        }

        public int GetLeft(string enemyType)
        {
            if (enemyType == EnemyTypeMelee)
            {
                return MeleeLeft;
            }

            if (enemyType == EnemyTypeBomber)
            {
                return BomberLeft;
            }

            if (enemyType == EnemyTypeDrone)
            {
                return DroneLeft;
            }

            if (enemyType == EnemyTypeTurret)
            {
                return TurretLeft;
            }

            return 0;
        }
    }
}

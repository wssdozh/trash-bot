using System;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Attacker _attacker;
    [SerializeField] private Stamina _stamina;
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private WeaponModifierApplier _weaponModifierApplier;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private AnimatorSwitcher _animatorSwitcher;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private PlayerAnimationEvents _animationEvents;
    [SerializeField] private PlayerMovementGate _movementGate;

    [Header("Настройки")]
    [SerializeField] private float _timeBattle = 3f;
    [SerializeField] private float _attackStaminaCost = 5f;

    [Header("Задержка первой атаки")]
    [SerializeField] private float _attackStartDelaySeconds = 0.25f;
    [SerializeField] private float _meleeAttackStartDelaySeconds = 0.15f;

    private PlayerCombatCore _combatCore;
    private PlayerBattleState _battleState;
    private PlayerActiveWeaponType _activeWeaponType;
    private bool _isInitialized;

    private bool _isAttackStartPending;
    private float _attackStartDelayTimerSeconds;

    public float AttackStaminaCost => _attackStaminaCost;

    public bool IsInBattle
    {
        get
        {
            if (_isInitialized == false)
            {
                return false;
            }

            return _combatCore.IsInBattle;
        }
    }

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        InitializeIfNeeded();

        _inventory.InventoryChanged += OnInventoryChanged;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;

        _isAttackStartPending = false;
        _attackStartDelayTimerSeconds = 0f;

        _combatCore.OnEnabled();
    }

    private void OnDisable()
    {
        if (_isInitialized == false)
        {
            return;
        }

        _inventory.InventoryChanged -= OnInventoryChanged;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;

        CancelAttackStartDelay();

        _combatCore.OnDisabled();
    }

    private void Update()
    {
        if (_isInitialized == false)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        TickAttackStartDelay(deltaTime);

        _combatCore.Tick(deltaTime);
    }

    public bool AttackStart()
    {
        InitializeIfNeeded();

        if (IsInBattle)
        {
            return _combatCore.AttackStart();
        }

        if (_isAttackStartPending)
        {
            return true;
        }

        float attackStartDelaySeconds = GetFirstAttackStartDelaySeconds();

        _battleState.Touch();

        if (attackStartDelaySeconds <= 0f)
        {
            return _combatCore.AttackStart();
        }

        _isAttackStartPending = true;
        _attackStartDelayTimerSeconds = attackStartDelaySeconds;

        return true;
    }

    public void AttackCancel()
    {
        InitializeIfNeeded();

        CancelAttackStartDelay();

        _combatCore.AttackCancel();
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        InitializeIfNeeded();

        _combatCore.SetAimPoint(aimPoint);
    }

    public void ExitBattle()
    {
        InitializeIfNeeded();

        CancelAttackStartDelay();

        _combatCore.ExitBattle();
    }

    public void ApplyModifier(float attackStaminaCost)
    {
        _attackStaminaCost = Mathf.Max(0f, attackStaminaCost);

        if (_isInitialized == false)
        {
            return;
        }

        _combatCore.ApplyAttackStaminaCost(_attackStaminaCost);
    }

    private void OnInventoryChanged()
    {
        _combatCore.OnInventoryChanged();
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        _combatCore.OnInventoryChanged();
    }

    private void TickAttackStartDelay(float deltaTime)
    {
        if (_isAttackStartPending == false)
        {
            return;
        }

        _attackStartDelayTimerSeconds -= deltaTime;

        if (_attackStartDelayTimerSeconds > 0f)
        {
            return;
        }

        _isAttackStartPending = false;
        _attackStartDelayTimerSeconds = 0f;

        _combatCore.AttackStart();
    }

    private void CancelAttackStartDelay()
    {
        _isAttackStartPending = false;
        _attackStartDelayTimerSeconds = 0f;
    }

    private float GetFirstAttackStartDelaySeconds()
    {
        WeaponType weaponType = _activeWeaponType.Value;

        if (weaponType == WeaponType.Melee || weaponType == WeaponType.None)
        {
            return _meleeAttackStartDelaySeconds;
        }

        return _attackStartDelaySeconds;
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_weaponModifierApplier == null)
        {
            throw new InvalidOperationException(nameof(_weaponModifierApplier));
        }

        PlayerActiveWeaponType activeWeaponType = new PlayerActiveWeaponType(_inventory);
        _activeWeaponType = activeWeaponType;

        PlayerBattleState battleState = new PlayerBattleState(
            _animator,
            _animatorSwitcher,
            _weaponHolder,
            activeWeaponType,
            _timeBattle);

        _battleState = battleState;

        PlayerRangedFire rangedFire = new PlayerRangedFire(
            _weaponHolder,
            _stamina,
            battleState,
            _attackStaminaCost);

        PlayerMeleeAttack meleeAttack = new PlayerMeleeAttack(
            _attacker,
            _stamina,
            _weaponModifierApplier,
            _animator,
            _animationEvents,
            _movementGate,
            battleState,
            _attackStaminaCost);

        _combatCore = new PlayerCombatCore(
            battleState,
            activeWeaponType,
            meleeAttack,
            rangedFire);

        _combatCore.ApplyAttackStaminaCost(_attackStaminaCost);

        _isInitialized = true;
    }
}

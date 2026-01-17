using System.Collections;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Attacker _attacker;
    [SerializeField] private Stamina _stamina;
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private AnimatorSwitcher _animatorSwitcher;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private PlayerAnimationEvents _animationEvents;
    [SerializeField] private PlayerMovementGate _movementGate;

    [Header("Настройки")]
    [SerializeField] private float _timeBattle = 3f;
    [SerializeField] private float _attackStaminaCost = 5f;

    private Coroutine _battleTimerCoroutine;

    private bool _isMeleeAttackInProgress;
    private bool _isMeleeHitPending;
    private bool _isMeleeAttackBuffered;
    private bool _isMeleeBufferLocked;

    private bool _isRangedFiring;

    public bool IsInBattle { get; private set; }

    private void OnEnable()
    {
        _inventory.InventoryChanged += UpdateWeaponAnimatorIfNeeded;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;

        _animationEvents.Attacking += OnAttackingFrame;
        _animationEvents.AttackEnded += OnAttackEnded;

        _weaponHolder.SetSwitchLocked(false);

        if (IsInBattle == false)
        {
            _weaponHolder.SetHoldAllowed(false);
        }
    }

    private void OnDisable()
    {
        _inventory.InventoryChanged -= UpdateWeaponAnimatorIfNeeded;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;

        _animationEvents.Attacking -= OnAttackingFrame;
        _animationEvents.AttackEnded -= OnAttackEnded;

        _movementGate.AllowMovement();

        _isRangedFiring = false;
        _weaponHolder.SetSwitchLocked(false);
        _weaponHolder.SetHoldAllowed(false);
    }

    public bool AttackStart()
    {
        WeaponType weaponType = GetActiveWeaponType();

        if (weaponType == WeaponType.Melee || weaponType == WeaponType.None)
        {
            return StartMeleeAttack();
        }

        return StartRangedAttack();
    }

    public void AttackCancel()
    {
        if (_isRangedFiring == false)
        {
            return;
        }

        StopFiringOnly();
        UnlockWeaponSwitchAndRefreshAnimator();
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor == null)
        {
            return;
        }

        fireExecutor.SetAimPoint(aimPoint);
    }

    public void ExitBattle()
    {
        ResetMeleeState();
        _isRangedFiring = false;

        _movementGate.AllowMovement();

        _weaponHolder.SetSwitchLocked(false);
        _weaponHolder.SetHoldAllowed(false);

        if (IsInBattle == true)
        {
            IsInBattle = false;

            _animator.SetFight(false);
            _animatorSwitcher.SetBattleMode(false);
            _animatorSwitcher.SetWeaponType(WeaponType.None);
        }

        StopBattleTimer();
        StopFiringOnly();
    }

    private bool StartRangedAttack()
    {
        if (_isRangedFiring == true)
        {
            EnterBattle();
            return true;
        }

        if (_stamina.Value <= 0f)
        {
            return false;
        }

        bool shouldRestoreHoldDisallowed = IsInBattle == false;

        _weaponHolder.SetHoldAllowed(true);

        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor == null)
        {
            RestoreHoldAllowedIfNeeded(shouldRestoreHoldDisallowed);
            return false;
        }

        if (fireExecutor.TryStartFiring() == false)
        {
            RestoreHoldAllowedIfNeeded(shouldRestoreHoldDisallowed);
            return false;
        }

        _stamina.Decrease(_attackStaminaCost);

        EnterBattle();

        _isRangedFiring = true;
        _weaponHolder.SetSwitchLocked(true);

        return true;
    }

    private void RestoreHoldAllowedIfNeeded(bool shouldRestoreHoldDisallowed)
    {
        if (shouldRestoreHoldDisallowed == false)
        {
            return;
        }

        _weaponHolder.SetHoldAllowed(false);
    }

    private bool StartMeleeAttack()
    {
        if (_isMeleeAttackInProgress == true)
        {
            return BufferMeleeAttack();
        }

        if (_stamina.Value <= 0f)
        {
            return false;
        }

        _isRangedFiring = false;
        ResetMeleeState();

        StartMeleeAttackCore();
        return true;
    }

    private bool BufferMeleeAttack()
    {
        if (_isMeleeBufferLocked == true)
        {
            return false;
        }

        if (_isMeleeAttackBuffered == true)
        {
            return false;
        }

        _isMeleeAttackBuffered = true;
        _isMeleeBufferLocked = true;

        EnterBattle();
        return true;
    }

    private bool TryStartChainedMeleeAttack()
    {
        if (_stamina.Value <= 0f)
        {
            return false;
        }

        StartMeleeAttackCore();
        return true;
    }

    private void StartMeleeAttackCore()
    {
        _isMeleeAttackInProgress = true;
        _isMeleeHitPending = true;

        _movementGate.BlockMovement();

        _animator.TriggerAttack();
        EnterBattle();

        _weaponHolder.SetSwitchLocked(true);
    }

    private void OnAttackingFrame()
    {
        if (_isMeleeAttackInProgress == false)
        {
            return;
        }

        if (_isMeleeHitPending == false)
        {
            return;
        }

        _isMeleeHitPending = false;

        if (_stamina.Value <= 0f)
        {
            return;
        }

        if (_attacker.PerformAttack() == true)
        {
            _stamina.Decrease(_attackStaminaCost);
        }
    }

    private void OnAttackEnded()
    {
        if (_isMeleeAttackInProgress == false)
        {
            return;
        }

        _isMeleeAttackInProgress = false;
        _isMeleeHitPending = false;

        if (_isMeleeAttackBuffered == true)
        {
            _isMeleeAttackBuffered = false;

            if (TryStartChainedMeleeAttack() == true)
            {
                return;
            }
        }

        _isMeleeBufferLocked = false;

        _movementGate.AllowMovement();

        UnlockWeaponSwitchAndRefreshAnimator();
    }

    private void EnterBattle()
    {
        if (IsInBattle == false)
        {
            IsInBattle = true;

            _animator.SetFight(true);
            _weaponHolder.SetHoldAllowed(true);

            _animatorSwitcher.SetBattleMode(true);
            UpdateWeaponAnimatorIfNeeded();
        }

        RestartBattleTimer();
    }

    private void RestartBattleTimer()
    {
        if (_battleTimerCoroutine != null)
        {
            StopCoroutine(_battleTimerCoroutine);
        }

        _battleTimerCoroutine = StartCoroutine(BattleTimerRoutine());
    }

    private IEnumerator BattleTimerRoutine()
    {
        yield return new WaitForSeconds(_timeBattle);

        _battleTimerCoroutine = null;
        ExitBattle();
    }

    private void StopBattleTimer()
    {
        if (_battleTimerCoroutine == null)
        {
            return;
        }

        StopCoroutine(_battleTimerCoroutine);
        _battleTimerCoroutine = null;
    }

    private void StopFiringOnly()
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor != null)
        {
            fireExecutor.StopFiring();
        }

        _isRangedFiring = false;
    }

    private void UnlockWeaponSwitchAndRefreshAnimator()
    {
        _weaponHolder.SetSwitchLocked(false);
        UpdateWeaponAnimatorIfNeeded();
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        UpdateWeaponAnimatorIfNeeded();
    }

    private void UpdateWeaponAnimatorIfNeeded()
    {
        if (IsInBattle == false)
        {
            return;
        }

        if (_weaponHolder.IsSwitchLocked == true)
        {
            return;
        }

        WeaponType weaponType = GetActiveWeaponType();
        _animatorSwitcher.SetWeaponType(weaponType);
    }

    private WeaponType GetActiveWeaponType()
    {
        int activeIndex = _inventory.ActiveIndex;
        int slotsCount = _inventory.Slots.Count;

        if (activeIndex < 0)
        {
            return WeaponType.None;
        }

        if (activeIndex >= slotsCount)
        {
            return WeaponType.None;
        }

        InventorySlot slot = _inventory.Slots[activeIndex];

        if (slot.IsEmpty() == true)
        {
            return WeaponType.None;
        }

        return slot.Item.WeaponType;
    }

    private void ResetMeleeState()
    {
        _isMeleeAttackInProgress = false;
        _isMeleeHitPending = false;
        _isMeleeAttackBuffered = false;
        _isMeleeBufferLocked = false;
    }
}

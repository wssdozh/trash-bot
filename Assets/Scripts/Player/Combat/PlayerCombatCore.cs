using UnityEngine;

public sealed class PlayerCombatCore
{
    private readonly PlayerBattleState _battleState;
    private readonly PlayerActiveWeaponType _activeWeaponType;
    private readonly PlayerMeleeAttack _meleeAttack;
    private readonly PlayerRangedFire _rangedFire;

    public bool IsInBattle => _battleState.IsInBattle;

    public PlayerCombatCore(
        PlayerBattleState battleState,
        PlayerActiveWeaponType activeWeaponType,
        PlayerMeleeAttack meleeAttack,
        PlayerRangedFire rangedFire)
    {
        _battleState = battleState;
        _activeWeaponType = activeWeaponType;
        _meleeAttack = meleeAttack;
        _rangedFire = rangedFire;
    }

    public void OnEnabled()
    {
        _battleState.SetSwitchLocked(false);

        if (_battleState.IsInBattle == false)
        {
            _battleState.SetHoldAllowed(false);
        }
    }

    public void OnDisabled()
    {
        ExitBattle();
    }

    public void Tick(float deltaTime)
    {
        _rangedFire.Tick();

        if (_battleState.Tick(deltaTime) == false)
        {
            return;
        }

        ExitBattle();
    }

    public void EnterBattle()
    {
        _battleState.Touch();
    }

    public bool AttackStart()
    {
        WeaponType weaponType = _activeWeaponType.Value;

        if (weaponType == WeaponType.Melee || weaponType == WeaponType.None)
        {
            _rangedFire.StopFiringOnly();

            return _meleeAttack.StartAttack();
        }

        _meleeAttack.CancelOnly();

        return _rangedFire.StartFiring();
    }

    public void AttackCancel()
    {
        if (_rangedFire.IsFiring == false)
        {
            return;
        }

        _rangedFire.StopFiringOnly();
        _battleState.UnlockWeaponSwitchAndRefreshAnimator();
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        _rangedFire.SetAimPoint(aimPoint);
    }

    public void ExitBattle()
    {
        _meleeAttack.CancelOnly();
        _rangedFire.StopFiringOnly();

        _battleState.SetSwitchLocked(false);
        _battleState.Exit();
    }

    public void OnInventoryChanged()
    {
        _battleState.SyncWeaponAnimatorIfNeeded();
    }

    public void ApplyAttackStaminaCost(float attackStaminaCost)
    {
        _meleeAttack.SetAttackStaminaCost(attackStaminaCost);
        _rangedFire.SetAttackStaminaCost(attackStaminaCost);
    }
}

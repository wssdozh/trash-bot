using UnityEngine;

public sealed class PlayerRangedFire
{
    private readonly WeaponHolder _weaponHolder;
    private readonly Stamina _stamina;
    private readonly PlayerBattleState _battleState;
    private readonly float _attackStaminaCost;

    public bool IsFiring { get; private set; }

    public PlayerRangedFire(
        WeaponHolder weaponHolder,
        Stamina stamina,
        PlayerBattleState battleState,
        float attackStaminaCost)
    {
        _weaponHolder = weaponHolder;
        _stamina = stamina;
        _battleState = battleState;
        _attackStaminaCost = attackStaminaCost;
    }

    public void Tick()
    {
        if (IsFiring == false)
        {
            return;
        }

        _battleState.Touch();
    }

    public bool StartFiring()
    {
        if (IsFiring)
        {
            _battleState.Touch();

            return true;
        }

        if (_stamina.Value <= 0f)
        {
            return false;
        }

        bool shouldRestoreHoldDisallowed = _battleState.IsInBattle == false;

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

        _battleState.Touch();

        IsFiring = true;
        _battleState.SetSwitchLocked(true);

        return true;
    }

    public void StopFiringOnly()
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor != null)
        {
            fireExecutor.StopFiring();
        }

        IsFiring = false;
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

    private void RestoreHoldAllowedIfNeeded(bool shouldRestoreHoldDisallowed)
    {
        if (shouldRestoreHoldDisallowed == false)
        {
            return;
        }

        _weaponHolder.SetHoldAllowed(false);
    }
}

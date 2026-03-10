using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAnimation : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private AnimatorSwitcher _animatorSwitcher;
    [SerializeField] private EnemyMove _enemyMove;
    [SerializeField] private EnemyMeleeBrain _enemyMeleeBrain;
    [SerializeField] private Health _health;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private WeaponHolder _weaponHolder;

    [Header("Weapon")]
    [SerializeField] private BasePickup _weaponPrefab;
    [SerializeField] private WeaponType _weaponType = WeaponType.None;

    private void Awake()
    {
        if (_animator == null)
        {
            throw new InvalidOperationException(nameof(_animator));
        }

        if (_animatorSwitcher == null)
        {
            throw new InvalidOperationException(nameof(_animatorSwitcher));
        }

        if (_enemyMove == null)
        {
            throw new InvalidOperationException(nameof(_enemyMove));
        }

        if (_enemyMeleeBrain == null)
        {
            throw new InvalidOperationException(nameof(_enemyMeleeBrain));
        }

        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_enemy == null)
        {
            throw new InvalidOperationException(nameof(_enemy));
        }

        if (_weaponPrefab != null && _weaponHolder == null)
        {
            throw new InvalidOperationException(nameof(_weaponHolder));
        }
    }

    private void OnEnable()
    {
        ConfigureAnimator();
        RefreshWeaponView();
        _health.Decreased += OnHealthDecreased;
        _enemy.Died += OnEnemyDied;

        ApplyState();
    }

    private void OnDisable()
    {
        _health.Decreased -= OnHealthDecreased;
        _enemy.Died -= OnEnemyDied;
        ClearWeaponView();
    }

    private void Update()
    {
        ApplyState();
    }

    public void TriggerAttack()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        _animatorSwitcher.SetBattleMode(true);
        _animator.SetFight(true);
        _animator.TriggerAttack();
    }

    private void OnHealthDecreased()
    {
        if (_enemy.IsDead)
        {
            return;
        }

        _animator.TriggerTakeDamage();
    }

    private void OnEnemyDied()
    {
        ApplyStopped();
    }

    private void ApplyState()
    {
        if (_enemy.IsDead)
        {
            ApplyStopped();

            return;
        }

        bool isFight = IsFight();

        _animatorSwitcher.SetBattleMode(isFight);
        _animator.SetFight(isFight);
        _animator.SetMoveState(_enemyMove.MoveAmount > 0f);
        _animator.SetSprintState(_enemyMove.IsRunning);
        _animator.SetWorldMoveDirection(_enemyMove.MoveDirection);
    }

    private bool IsFight()
    {
        EnemyState enemyState = _enemyMeleeBrain.State;

        if (enemyState == EnemyState.Chase)
        {
            return true;
        }

        if (enemyState == EnemyState.Fight)
        {
            return true;
        }

        if (enemyState == EnemyState.Search)
        {
            return true;
        }

        return false;
    }

    private void ApplyStopped()
    {
        _animatorSwitcher.SetBattleModeInstant(false);
        _animator.SetFight(false);
        _animator.SetMoveState(false);
        _animator.SetSprintState(false);
        _animator.SetWorldMoveDirection(Vector3.zero);
    }

    private void ConfigureAnimator()
    {
        _animatorSwitcher.SetWeaponTypeInstant(_weaponType);
        _animatorSwitcher.SetBattleModeInstant(false);
        _animator.ResetAttackOrder();
    }

    private void RefreshWeaponView()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        _weaponHolder.SetHoldAllowed(true);
        _weaponHolder.SetSwitchLocked(false);

        if (_weaponPrefab == null)
        {
            _weaponHolder.Clear();

            return;
        }

        _weaponHolder.Equip(_weaponPrefab);
    }

    private void ClearWeaponView()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        _weaponHolder.Clear();
    }
}

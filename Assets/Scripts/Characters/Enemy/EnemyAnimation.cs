using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class EnemyAnimation : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private AnimatorSwitcher _animatorSwitcher;
    [SerializeField] private EnemyMove _enemyMove;
    [FormerlySerializedAs("_enemyMeleeBrain")]
    [SerializeField] private MonoBehaviour _enemyBrain;
    [SerializeField] private Health _health;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private WeaponHolder _weaponHolder;

    [Header("Weapon")]
    [SerializeField] private BasePickup _weaponPrefab;
    [SerializeField] private WeaponType _weaponType = WeaponType.None;
    [SerializeField, Min(0.1f)] private float _weaponFireRateScale = 1f;
    [SerializeField, Min(0.1f)] private float _weaponDamageScale = 1f;
    [SerializeField, Min(0.02f)] private float _updateTick = 0.05f;

    private IEnemyBrain _enemyBrainState;
    private Coroutine _stateCoroutine;
    private WaitForSeconds _stateWait;

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

        if (_enemyBrain == null)
        {
            throw new InvalidOperationException(nameof(_enemyBrain));
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

        if (_weaponFireRateScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_weaponFireRateScale));
        }

        if (_weaponDamageScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_weaponDamageScale));
        }

        ResolveBrain();
        _stateWait = new WaitForSeconds(GetUpdateTick());
    }

    private void OnEnable()
    {
        ConfigureAnimator();
        RefreshWeaponView();
        _health.Decreased += OnHealthDecreased;
        _enemy.Died += OnEnemyDied;

        ApplyState();
        StartStateLoop();
    }

    private void OnDisable()
    {
        _health.Decreased -= OnHealthDecreased;
        _enemy.Died -= OnEnemyDied;
        StopStateLoop();
        ClearWeaponView();
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

    public void SetWeapon(BasePickup weaponPrefab)
    {
        if (weaponPrefab != null && _weaponHolder == null)
        {
            throw new InvalidOperationException(nameof(_weaponHolder));
        }

        if (_weaponPrefab == weaponPrefab)
        {
            return;
        }

        _weaponPrefab = weaponPrefab;

        if (isActiveAndEnabled == false)
        {
            return;
        }

        _animatorSwitcher.SetWeaponTypeInstant(GetWeaponType());
        RefreshWeaponView();
        ApplyState();
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
        EnemyState enemyState = _enemyBrainState.State;

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
        _animatorSwitcher.SetWeaponTypeInstant(GetWeaponType());
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
        ApplyWeaponBalance();
    }

    private void ClearWeaponView()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        _weaponHolder.Clear();
    }

    private void ApplyWeaponBalance()
    {
        if (_weaponHolder == null)
        {
            return;
        }

        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor == null)
        {
            return;
        }

        WeaponModifierContext weaponModifierContext = new WeaponModifierContext();
        weaponModifierContext.SetDefaults();
        weaponModifierContext.WeaponType = GetWeaponType();
        weaponModifierContext.FireRateMultiplier = _weaponFireRateScale;
        weaponModifierContext.DamageMultiplier = _weaponDamageScale;

        fireExecutor.ApplyModifierContext(weaponModifierContext);
    }

    private WeaponType GetWeaponType()
    {
        if (_weaponPrefab == null)
        {
            return WeaponType.None;
        }

        if (_weaponPrefab.Item == null)
        {
            return _weaponType;
        }

        return _weaponPrefab.Item.WeaponType;
    }

    private void ResolveBrain()
    {
        IEnemyBrain directBrain = GetBrain(_enemyBrain);

        if (directBrain != null && _enemyBrain.isActiveAndEnabled)
        {
            _enemyBrainState = directBrain;

            return;
        }

        MonoBehaviour[] brainSources = GetComponentsInParent<MonoBehaviour>(true);
        int brainIndex = 0;

        while (brainIndex < brainSources.Length)
        {
            MonoBehaviour brainSource = brainSources[brainIndex];
            IEnemyBrain enemyBrain = GetBrain(brainSource);

            if (enemyBrain != null && brainSource.isActiveAndEnabled)
            {
                _enemyBrain = brainSource;
                _enemyBrainState = enemyBrain;

                return;
            }

            brainIndex += 1;
        }

        brainIndex = 0;

        while (brainIndex < brainSources.Length)
        {
            MonoBehaviour brainSource = brainSources[brainIndex];
            IEnemyBrain enemyBrain = GetBrain(brainSource);

            if (enemyBrain != null)
            {
                _enemyBrain = brainSource;
                _enemyBrainState = enemyBrain;

                return;
            }

            brainIndex += 1;
        }

        throw new InvalidOperationException(nameof(_enemyBrain));
    }

    private IEnemyBrain GetBrain(MonoBehaviour brainSource)
    {
        if (brainSource is IEnemyBrain enemyBrain)
        {
            return enemyBrain;
        }

        return null;
    }

    private IEnumerator StateLoop()
    {
        while (enabled)
        {
            ApplyState();

            yield return _stateWait;
        }
    }

    private void StartStateLoop()
    {
        StopStateLoop();
        _stateWait = new WaitForSeconds(GetUpdateTick());
        _stateCoroutine = StartCoroutine(StateLoop());
    }

    private void StopStateLoop()
    {
        if (_stateCoroutine == null)
        {
            return;
        }

        StopCoroutine(_stateCoroutine);
        _stateCoroutine = null;
    }

    private float GetUpdateTick()
    {
        return Mathf.Max(0.02f, _updateTick);
    }
}

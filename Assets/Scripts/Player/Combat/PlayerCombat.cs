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

    private PlayerCombatCore _combatCore;
    private bool _isInitialized;

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

        _combatCore.OnDisabled();
    }

    private void Update()
    {
        if (_isInitialized == false)
        {
            return;
        }

        _combatCore.Tick(Time.deltaTime);
    }

    public bool AttackStart()
    {
        InitializeIfNeeded();
        return _combatCore.AttackStart();
    }

    public void AttackCancel()
    {
        InitializeIfNeeded();
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
        _combatCore.ExitBattle();
    }

    private void OnInventoryChanged()
    {
        _combatCore.OnInventoryChanged();
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        _combatCore.OnInventoryChanged();
    }

    private void InitializeIfNeeded()
    {
        if (_isInitialized == true)
        {
            return;
        }

        PlayerActiveWeaponType activeWeaponType = new PlayerActiveWeaponType(_inventory);

        PlayerBattleState battleState = new PlayerBattleState(
            _animator,
            _animatorSwitcher,
            _weaponHolder,
            activeWeaponType,
            _timeBattle);

        PlayerRangedFire rangedFire = new PlayerRangedFire(
            _weaponHolder,
            _stamina,
            battleState,
            _attackStaminaCost);

        PlayerMeleeAttack meleeAttack = new PlayerMeleeAttack(
            _attacker,
            _stamina,
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

        _isInitialized = true;
    }
}

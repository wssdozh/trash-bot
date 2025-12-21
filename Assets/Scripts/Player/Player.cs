using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Attacker _attack;
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private CharacterJump _jump;
    [SerializeField] private CharacterRotator _rotator;
    [SerializeField] private CharacterEffects _effects;
    [SerializeField] private CharacterAudio _audio;
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private CursorManager _cursor;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Health _health;
    [SerializeField] private Stamina _stamina;
    [SerializeField] private CharacterInteractor _interactor;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private InventoryDropper _inventoryDropper;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private AnimatorSwitcher _animatorSwitcher;
    [SerializeField] private WeaponHolder _weaponHolder;
    [SerializeField] private PauseController _pauseController;

    [Header("Настройки")]
    [SerializeField] private float _timeBattle = 3f;
    [SerializeField] private float _jumpStaminaCost = 10f;
    [SerializeField] private float _attackStaminaCost = 5f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 5f;

    private PlayerInputActions _inputs;

    private bool _isBattle = false;
    private bool _isSprinting = false;

    private Coroutine _battleTimerCoroutine;
    private Coroutine _sprintCoroutine;

    private Vector2 _moveInput;

    public event Action Died;

    private void Awake()
    {
        _inputs = new PlayerInputActions();

        SubscribeInput();
        SubscribeInventory();

        StartCoroutine(HoverTickRoutine());
    }

    private void OnEnable()
    {
        _inputs.Enable();
        _health.Ended += Die;
    }

    private void OnDisable()
    {
        _inputs.Disable();
        _health.Ended -= Die;
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
        UnsubscribeInventory();

        if (_inputs != null)
        {
            _inputs.Dispose();
            _inputs = null;
        }
    }

    private void FixedUpdate()
    {
        if (_isBattle == true)
        {
            _rotator.Rotate();
            return;
        }

        _rotator.RotateTowardsMovement(_moveInput);
    }

    private void SubscribeInput()
    {
        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;

        _inputs.Player.Jump.performed += OnJumpPerformed;

        _inputs.Player.Sprint.performed += OnSprintPerformed;
        _inputs.Player.Sprint.canceled += OnSprintCanceled;

        _inputs.Player.Attack.performed += OnAttackPerformed;
        _inputs.Player.Attack.canceled += OnAttackCanceled;

        _inputs.Player.UseItem.performed += OnUseItemPerformed;

        _inputs.Player.Scroll.performed += OnScrollPerformed;

        _inputs.Player.Interact.performed += OnInteractPerformed;

        _inputs.Player.Drop.performed += OnDropPerformed;
        _inputs.Player.DropAll.performed += OnDropAllPerformed;
    }

    private void UnsubscribeInput()
    {
        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;

        _inputs.Player.Jump.performed -= OnJumpPerformed;

        _inputs.Player.Sprint.performed -= OnSprintPerformed;
        _inputs.Player.Sprint.canceled -= OnSprintCanceled;

        _inputs.Player.Attack.performed -= OnAttackPerformed;
        _inputs.Player.Attack.canceled -= OnAttackCanceled;

        _inputs.Player.UseItem.performed -= OnUseItemPerformed;

        _inputs.Player.Scroll.performed -= OnScrollPerformed;

        _inputs.Player.Interact.performed -= OnInteractPerformed;

        _inputs.Player.Drop.performed -= OnDropPerformed;
        _inputs.Player.DropAll.performed -= OnDropAllPerformed;
    }

    private void SubscribeInventory()
    {
        _inventory.InventoryChanged += SetCurrentAnimator;
        _inventory.ActiveIndexChanged += OnActiveIndexChanged;
    }

    private void UnsubscribeInventory()
    {
        _inventory.InventoryChanged -= SetCurrentAnimator;
        _inventory.ActiveIndexChanged -= OnActiveIndexChanged;
    }

    private void OnActiveIndexChanged(int activeIndex)
    {
        SetCurrentAnimator();
    }

    private void Die()
    {
        Died?.Invoke();
    }

    private IEnumerator HoverTickRoutine()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(0.15f);

        while (enabled == true)
        {
            _interactor.TickHover();

            FireExecutor fireExecutor = _weaponHolder.FireExecutor;

            if (fireExecutor != null)
            {
                fireExecutor.SetAimPoint(_cursor.MouseHitPos);
            }

            yield return waitForSeconds;
        }
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 scrollValue = context.ReadValue<Vector2>();

        if (scrollValue.y > 0f)
        {
            _inventory.PreviousActiveSlot();
        }

        if (scrollValue.y < 0f)
        {
            _inventory.NextActiveSlot();
        }
    }

    private void OnDropPerformed(InputAction.CallbackContext context)
    {
        _inventoryDropper.DropOneFromActiveSlot();
    }

    private void OnDropAllPerformed(InputAction.CallbackContext context)
    {
        _inventoryDropper.DropAllFromActiveSlot();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor != null)
        {
            TryStartFiring(fireExecutor);
            return;
        }

        TryMeleeAttack();
    }

    private void TryStartFiring(FireExecutor fireExecutor)
    {
        if (_stamina.Value <= 0f)
        {
            return;
        }

        bool startedFiring = fireExecutor.TryStartFiring();

        if (startedFiring == false)
        {
            return;
        }

        _stamina.Decrease(_attackStaminaCost);
        EnterBattleMode();
    }

    private void TryMeleeAttack()
    {
        if (_stamina.Value > 0f && _attack.PerformAttack() == true)
        {
            _stamina.Decrease(_attackStaminaCost);
        }

        _animator.TriggerAttack();
        EnterBattleMode();
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        FireExecutor fireExecutor = _weaponHolder.FireExecutor;

        if (fireExecutor != null)
        {
            fireExecutor.StopFiring();
        }
    }

    private void OnUseItemPerformed(InputAction.CallbackContext context)
    {
        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty() == true)
        {
            return;
        }

        if (activeSlot.Item.Effects.Count <= 0)
        {
            return;
        }

        UseActiveItem(activeSlot);
    }

    private void UseActiveItem(InventorySlot slot)
    {
        Item item = slot.Item;

        for (int effectIndex = 0; effectIndex < item.Effects.Count; effectIndex++)
        {
            ItemEffect effect = item.Effects[effectIndex];
            effect.Apply(_effects);
        }

        _inventory.TryRemoveFromSlot(_inventory.ActiveIndex, 1);
        _audio.PlayItemUse(item);
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (_pauseController.IsPaused == false)
        {
            _pauseController.Pause();
            return;
        }

        _pauseController.Resume();
        
        ExitBattleMode();

        _interactor.TryInteract(_movement.gameObject);
        _animator.TriggerPoint();
    }

    private void EnterBattleMode()
    {
        _isBattle = true;
        _animator.SetFight(true);

        SetCurrentAnimator();
        StopSprinting();

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

        ExitBattleMode();
        _battleTimerCoroutine = null;
    }

    private void ExitBattleMode()
    {
        if (_isBattle == false)
        {
            return;
        }

        _isBattle = false;
        _animator.SetFight(false);
        SetAnimator(WeaponType.None);
    }

    private void SetAnimator(WeaponType weaponType)
    {
        _animatorSwitcher.SetWeaponType(weaponType);
    }

    private void SetCurrentAnimator()
    {
        if (_isBattle == false)
        {
            return;
        }

        InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

        if (activeSlot.IsEmpty() == true)
        {
            SetAnimator(WeaponType.None);
            return;
        }

        SetAnimator(activeSlot.Item.WeaponType);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        _movement.OnMove(_moveInput);

        _animator.SetMoveState(true);
        _audio.PlayFootstep();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _moveInput = Vector2.zero;
        _movement.OnMove(Vector2.zero);

        _animator.SetMoveState(false);
        _audio.PlayFootstep();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (_stamina.Value > _jumpStaminaCost)
        {
            _jump.OnJump();
            _stamina.Decrease(_jumpStaminaCost);
        }
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        if (_stamina.Value <= 0f)
        {
            return;
        }

        if (_isSprinting == true)
        {
            return;
        }

        ExitBattleMode();

        _isSprinting = true;
        _movement.OnSprint(true);
        _animator.SetSprintState(true);

        if (_sprintCoroutine != null)
        {
            StopCoroutine(_sprintCoroutine);
        }

        _sprintCoroutine = StartCoroutine(SprintConsumeRoutine());
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        StopSprinting();
    }

    private IEnumerator SprintConsumeRoutine()
    {
        while (_isSprinting == true)
        {
            if (_stamina.Value <= _stamina.MinValue)
            {
                StopSprinting();
                yield break;
            }

            _stamina.Decrease(_sprintStaminaCostPerSecond * Time.deltaTime);
            yield return null;
        }

        _sprintCoroutine = null;
    }

    private void StopSprinting()
    {
        if (_isSprinting == false)
        {
            return;
        }

        _isSprinting = false;
        _movement.OnSprint(false);
        _animator.SetSprintState(false);

        if (_sprintCoroutine != null)
        {
            StopCoroutine(_sprintCoroutine);
            _sprintCoroutine = null;
        }
    }
}

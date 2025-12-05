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



    [Header("Настройки")]
    [SerializeField] private float _timeBattle = 3f;
    [SerializeField] private float _jumpStaminaCost = 10f;
    [SerializeField] private float _attackStaminaCost = 5f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 5f;

    private PlayerInputActions _inputs;
    private bool _isBattle = false;
    private bool _isSprinting = false;
    private Coroutine _waitCoroutine;
    private Coroutine _sprintCoroutine;
    private Vector2 _moveInput;

    public event Action Died;

    private void Awake()
    {
        _inputs = new PlayerInputActions();

        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;

        _inputs.Player.Jump.performed += OnJumpPerformed;
        _inputs.Player.Sprint.performed += OnSprintPerformed;
        _inputs.Player.Sprint.canceled += OnSprintCanceled;

        _inputs.Player.Attack.performed += OnAttackPerformed;
        _inputs.Player.UseItem.performed += OnUseItemPerformed;

        _inputs.Player.Scroll.performed += OnScrollPerformed;

        _inputs.Player.Interact.performed += OnInteractPerformed;
        _inputs.Player.Drop.performed += OnDropPerformed;
        _inputs.Player.DropAll.performed += OnDropAllPerformed;

        StartCoroutine(While());
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

    private IEnumerator While()
    {
        WaitForSeconds waitForSeconds = new WaitForSeconds(0.15f);

        while (enabled)
        {
            _interactor.TickHover();
            
            yield return waitForSeconds;
        }
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        if (_inventory == null)
        {
            return;
        }

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
        if (_inventory == null)
        {
            return;
        }

        _inventoryDropper.DropOneFromActiveSlot();
    }

    private void OnDropAllPerformed(InputAction.CallbackContext context)
    {
        if (_inventory == null)
        {
            return;
        }

        _inventoryDropper.DropAllFromActiveSlot();
    }

    private void FixedUpdate()
    {
        if (_isBattle)
            _rotator.Rotate();
        else
            _rotator.RotateTowardsMovement(_moveInput);
    }

    private void Die()
    {
        Died?.Invoke();
    }

    private void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        if(_stamina.Value > 0f && _attack.PerformAttack())
        {
            _stamina.Decrease(_attackStaminaCost);
        }

        if (_waitCoroutine != null)
            StopCoroutine(_waitCoroutine);

        _waitCoroutine = StartCoroutine(Wait());
    }


    private void OnUseItemPerformed(InputAction.CallbackContext ctx)
    {
        if (_inventory != null)
        {
            InventorySlot activeSlot = _inventory.Slots[_inventory.ActiveIndex];

            if (activeSlot.IsEmpty() == false && activeSlot.Item.Effects.Count > 0)
            {
                UseActiveItem(activeSlot);

                return;
            }
        }
    }

    private void UseActiveItem(InventorySlot slot)
    {
        Item item = slot.Item;

        for (int i = 0; i < item.Effects.Count; i++)
        {
            ItemEffect effect = item.Effects[i];
            effect.Apply(_effects);
        } 

        _inventory.TryRemoveFromSlot(_inventory.ActiveIndex, 1);

        _audio.PlayItemUse(item);
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        _interactor.TryInteract(gameObject);
    }

    private IEnumerator Wait()
    {
        _isBattle = true;

        yield return new WaitForSeconds(_timeBattle);

        _isBattle = false;
    }
    
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>();
        _movement.OnMove(_moveInput);

        _animator.SetMoveState(true);

        _audio.PlayFootstep();
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveInput = Vector2.zero;
        _movement.OnMove(Vector2.zero);

        _animator.SetMoveState(false);

        _audio.PlayFootstep();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (_stamina.Value > _jumpStaminaCost)
        {
            _jump.OnJump();
            _stamina.Decrease(_jumpStaminaCost);
        }
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        if (_stamina.Value > 0f && _isSprinting == false)
        {
            _isSprinting = true;
            _movement.OnSprint(true);

            _animator.SetSprintState(true);

            _sprintCoroutine = StartCoroutine(SprintConsume());
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        StopSprinting();
    }

    private IEnumerator SprintConsume()
    {
        while (_isSprinting)
        {
            if (_stamina.Value <= _stamina.MinValue)
            {
                StopSprinting();
                yield break;
            }

            _stamina.Decrease(_sprintStaminaCostPerSecond * Time.deltaTime);

            yield return null;
        }
    }

    private void StopSprinting()
    {
        if (_isSprinting == false)
            return;

        _isSprinting = false;
        _movement.OnSprint(false);
        _animator.SetSprintState(false);

        if (_sprintCoroutine != null)
            StopCoroutine(_sprintCoroutine);
    }
}

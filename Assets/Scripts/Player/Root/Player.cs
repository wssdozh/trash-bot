using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private Health _health;
    [SerializeField] private RectTransform _uiCanvas;
    [SerializeField] private RectTransform _bossHealthIndicatorTemplate;

    [Header("Модули")]
    [SerializeField] private PlayerMovement _movement;
    [SerializeField] private PlayerCombat _combat;
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private PlayerInteraction _interaction;
    [SerializeField] private PlayerPause _pause;

    private PlayerInputActions _inputs;
    private BossHealthOverlay _bossHealthOverlay;
    private RemainingEnemyOverlay _remainingEnemyOverlay;

    public static Player Instance { get; private set; }
    public event Action Died;
    public PlayerMovement Movement => _movement;

    private void Awake()
    {
        Instance = this;

        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_uiCanvas == null)
        {
            throw new InvalidOperationException(nameof(_uiCanvas));
        }

        if (_bossHealthIndicatorTemplate == null)
        {
            throw new InvalidOperationException(nameof(_bossHealthIndicatorTemplate));
        }

        _inputs = new PlayerInputActions();
        _bossHealthOverlay = gameObject.AddComponent<BossHealthOverlay>();
        _bossHealthOverlay.Initialize(_uiCanvas, _bossHealthIndicatorTemplate);
        _remainingEnemyOverlay = gameObject.AddComponent<RemainingEnemyOverlay>();
        _remainingEnemyOverlay.Initialize(transform, _uiCanvas);
        SubscribeInput();
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
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }

        if (_inputs == null)
        {
            return;
        }

        UnsubscribeInput();
        _inputs.Dispose();
        _inputs = null;
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

        _inputs.Player.Pause.performed += OnPausePerformed;
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

        _inputs.Player.Pause.performed -= OnPausePerformed;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            _inventory.SetActiveSlot(0);
            return;
        }

        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            _inventory.SetActiveSlot(1);
            return;
        }

        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            _inventory.SetActiveSlot(2);
        }
    }

    private void FixedUpdate()
    {
        _movement.TickFixed(_combat.IsInBattle);
    }

    private void Die()
    {
        if (Died != null)
        {
            Died.Invoke();
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 moveVector = context.ReadValue<Vector2>();
        _movement.SetMove(moveVector);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _movement.SetMove(Vector2.zero);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        _movement.TryJump();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        _combat.ExitBattle();
        _movement.TryStartSprinting();
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        _movement.StopSprinting();
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        bool attackStarted = _combat.AttackStart();

        if (attackStarted)
        {
            _movement.StopSprinting();
        }
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        _combat.AttackCancel();
    }

    private void OnUseItemPerformed(InputAction.CallbackContext context)
    {
        _inventory.TryUseActiveItem();
    }

    private void OnScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 scrollValue = context.ReadValue<Vector2>();
        _inventory.Scroll(scrollValue);
    }

    private void OnDropPerformed(InputAction.CallbackContext context)
    {
        _inventory.DropOne();
    }

    private void OnDropAllPerformed(InputAction.CallbackContext context)
    {
        _inventory.DropAll();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        _combat.ExitBattle();
        _interaction.Interact();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        _pause.Toggle();
    }
}

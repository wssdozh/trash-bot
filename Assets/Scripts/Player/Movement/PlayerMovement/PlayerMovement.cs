using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CharacterMover _movement;
    [SerializeField] private CharacterJump _jump;
    [SerializeField] private CharacterRotator _rotator;
    [SerializeField] private PlayerAnimator _animator;
    [SerializeField] private Stamina _stamina;
    [SerializeField] private PlayerMovementGate _movementGate;

    [Header("Настройки")]
    [SerializeField] private float _jumpStaminaCost = 10f;
    [SerializeField] private float _sprintStaminaCostPerSecond = 5f;
    [SerializeField] private float _moveStopDelaySeconds = 0.06f;

    [Header("Атака: плавное замедление и возврат")]
    [SerializeField] private float _attackEnterSeconds = 0.12f;
    [SerializeField] private float _attackExitSeconds = 0.10f;
    [SerializeField] private float _attackMovementMultiplier = 0.15f;

    private PlayerMoveApplier _moveApplier;
    private PlayerMoveStopDelay _moveStopDelay;
    private PlayerAttackMovementBlend _attackMovementBlend;
    private PlayerSprint _sprint;
    private PlayerJumpAction _jumpAction;
    private PlayerRotationByState _rotationByState;

    public bool IsSprinting => _sprint.IsSprinting;

    public float JumpStaminaCost => _jumpStaminaCost;

    public float SprintStaminaCostPerSecond => _sprintStaminaCostPerSecond;

    private void Awake()
    {
        _moveApplier = new PlayerMoveApplier(_movement, _animator);
        _moveStopDelay = new PlayerMoveStopDelay(_moveApplier, _moveStopDelaySeconds);

        _sprint = new PlayerSprint(_movement, _animator, _stamina, _sprintStaminaCostPerSecond);
        _jumpAction = new PlayerJumpAction(_jump, _stamina, _jumpStaminaCost);

        _attackMovementBlend = new PlayerAttackMovementBlend(
            _moveApplier,
            _sprint,
            _attackEnterSeconds,
            _attackExitSeconds,
            _attackMovementMultiplier);

        _rotationByState = new PlayerRotationByState(_rotator, _moveApplier, _attackMovementBlend);
    }

    private void OnEnable()
    {
        _movementGate.MovementAllowedChanged += OnMovementAllowedChanged;
    }

    private void OnDisable()
    {
        _movementGate.MovementAllowedChanged -= OnMovementAllowedChanged;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (_movementGate.IsMovementAllowed == false)
        {
            _sprint.Stop();
            _moveStopDelay.Cancel();
            _attackMovementBlend.TickSlowdown(deltaTime);
            return;
        }

        _sprint.Tick(deltaTime);

        if (_attackMovementBlend.IsRecoverActive)
        {
            _attackMovementBlend.TickRecover(deltaTime);
            return;
        }

        _moveStopDelay.Tick(deltaTime);
    }

    public void SetMove(Vector2 moveVector)
    {
        _moveApplier.SetInput(moveVector);

        if (_movementGate.IsMovementAllowed == false)
        {
            return;
        }

        if (_attackMovementBlend.IsRecoverActive)
        {
            return;
        }

        _moveStopDelay.OnInputMove(moveVector);
    }

    public void TryJump()
    {
        _jumpAction.TryJump(_movementGate.IsMovementAllowed);
    }

    public void TryStartSprinting()
    {
        if (_movementGate.IsMovementAllowed == false)
        {
            return;
        }

        _sprint.TryStart();
    }

    public void StopSprinting()
    {
        _sprint.Stop();
    }

    public void ApplyModifier(float jumpStaminaCost, float sprintStaminaCostPerSecond)
    {
        _jumpStaminaCost = Mathf.Max(0f, jumpStaminaCost);
        _sprintStaminaCostPerSecond = Mathf.Max(0f, sprintStaminaCostPerSecond);

        if (_jumpAction != null)
        {
            _jumpAction.SetCost(_jumpStaminaCost);
        }

        if (_sprint != null)
        {
            _sprint.SetCostPerSecond(_sprintStaminaCostPerSecond);
        }
    }

    public void TickFixed(bool isInBattle)
    {
        _rotationByState.TickFixed(isInBattle, _movementGate.IsMovementAllowed);
    }

    private void OnMovementAllowedChanged(bool isAllowed)
    {
        if (isAllowed == false)
        {
            _attackMovementBlend.CancelRecover();
            _moveStopDelay.Cancel();
            _sprint.Stop();
            return;
        }

        _moveStopDelay.Cancel();
        _attackMovementBlend.StartRecover();
    }
}

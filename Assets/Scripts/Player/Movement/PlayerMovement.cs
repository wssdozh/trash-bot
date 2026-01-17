using System.Collections;
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

    private Vector2 _inputMoveVector;
    private Vector2 _appliedMoveVector;

    private Vector3 _lastWorldMoveDirection;

    private bool _isStopDelayActive;
    private float _stopMoveTimerSeconds;

    private bool _isAttackRecoverActive;

    private Coroutine _sprintCoroutine;

    public bool IsSprinting { get; private set; }

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
        if (_movementGate.IsMovementAllowed == false)
        {
            UpdateAttackMovementSlowdown();
            return;
        }

        if (_isAttackRecoverActive == true)
        {
            UpdateAttackMovementRecover();
            return;
        }

        UpdateMoveStopDelay();
    }

    public void SetMove(Vector2 moveVector)
    {
        _inputMoveVector = moveVector;

        if (_movementGate.IsMovementAllowed == false)
        {
            return;
        }

        if (_isAttackRecoverActive == true)
        {
            return;
        }

        if (moveVector == Vector2.zero)
        {
            ApplyMove(Vector2.zero);

            _isStopDelayActive = true;
            _stopMoveTimerSeconds = 0f;

            _animator.SetWorldMoveDirection(_lastWorldMoveDirection);
            _animator.SetMoveState(true);

            return;
        }

        _isStopDelayActive = false;

        ApplyMove(moveVector);
        _animator.SetMoveState(true);
    }

    public void TryJump()
    {
        if (_movementGate.IsMovementAllowed == false)
        {
            return;
        }

        if (_stamina.Value > _jumpStaminaCost)
        {
            _jump.OnJump();
            _stamina.Decrease(_jumpStaminaCost);
        }
    }

    public void TryStartSprinting()
    {
        if (_movementGate.IsMovementAllowed == false)
        {
            return;
        }

        if (_stamina.Value <= 0f)
        {
            return;
        }

        if (IsSprinting == true)
        {
            return;
        }

        IsSprinting = true;
        _movement.OnSprint(true);
        _animator.SetSprintState(true);

        if (_sprintCoroutine != null)
        {
            StopCoroutine(_sprintCoroutine);
        }

        _sprintCoroutine = StartCoroutine(SprintConsumeRoutine());
    }

    public void StopSprinting()
    {
        if (IsSprinting == false)
        {
            return;
        }

        IsSprinting = false;
        _movement.OnSprint(false);
        _animator.SetSprintState(false);

        if (_sprintCoroutine != null)
        {
            StopCoroutine(_sprintCoroutine);
            _sprintCoroutine = null;
        }
    }

    public void TickFixed(bool isInBattle)
    {
        if (isInBattle == true)
        {
            _rotator.Rotate();
            return;
        }

        if (_movementGate.IsMovementAllowed == false)
        {
            _rotator.RotateTowardsMovement(_appliedMoveVector);
            return;
        }

        if (_isAttackRecoverActive == true)
        {
            _rotator.RotateTowardsMovement(_appliedMoveVector);
            return;
        }

        _rotator.RotateTowardsMovement(_inputMoveVector);
    }

    private void OnMovementAllowedChanged(bool isAllowed)
    {
        if (isAllowed == false)
        {
            _isAttackRecoverActive = false;
            _isStopDelayActive = false;
            StopSprinting();
            return;
        }

        _isStopDelayActive = false;
        _isAttackRecoverActive = true;
    }

    private void UpdateMoveStopDelay()
    {
        if (_isStopDelayActive == false)
        {
            return;
        }

        if (_inputMoveVector != Vector2.zero)
        {
            _isStopDelayActive = false;
            return;
        }

        if (_moveStopDelaySeconds <= 0f)
        {
            _isStopDelayActive = false;
            _animator.SetWorldMoveDirection(Vector3.zero);
            _animator.SetMoveState(false);
            return;
        }

        _stopMoveTimerSeconds += Time.deltaTime;

        if (_stopMoveTimerSeconds < _moveStopDelaySeconds)
        {
            _animator.SetWorldMoveDirection(_lastWorldMoveDirection);
            _animator.SetMoveState(true);
            return;
        }

        _isStopDelayActive = false;
        _animator.SetWorldMoveDirection(Vector3.zero);
        _animator.SetMoveState(false);
    }

    private void UpdateAttackMovementSlowdown()
    {
        StopSprinting();

        Vector2 targetMoveVector = GetAttackTargetMoveVector();
        float speed = GetSpeedFromSeconds(_attackEnterSeconds);
        Vector2 newMoveVector = MoveVectorTowards(_appliedMoveVector, targetMoveVector, speed);

        ApplyMove(newMoveVector);

        if (newMoveVector == Vector2.zero)
        {
            _animator.SetMoveState(false);
            return;
        }

        _animator.SetMoveState(true);
    }

    private void UpdateAttackMovementRecover()
    {
        Vector2 targetMoveVector = _inputMoveVector;
        float speed = GetSpeedFromSeconds(_attackExitSeconds);
        Vector2 newMoveVector = MoveVectorTowards(_appliedMoveVector, targetMoveVector, speed);

        ApplyMove(newMoveVector);

        if (newMoveVector == targetMoveVector)
        {
            _isAttackRecoverActive = false;
        }

        if (newMoveVector == Vector2.zero)
        {
            _animator.SetMoveState(false);
            return;
        }

        _animator.SetMoveState(true);
    }

    private Vector2 GetAttackTargetMoveVector()
    {
        if (_inputMoveVector == Vector2.zero)
        {
            return Vector2.zero;
        }

        return _inputMoveVector * _attackMovementMultiplier;
    }

    private Vector2 MoveVectorTowards(Vector2 current, Vector2 target, float speed)
    {
        float delta = speed * Time.deltaTime;

        float newX = Mathf.MoveTowards(current.x, target.x, delta);
        float newY = Mathf.MoveTowards(current.y, target.y, delta);

        Vector2 result = new Vector2(newX, newY);
        return result;
    }

    private float GetSpeedFromSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            return 1000f;
        }

        return 1f / seconds;
    }

    private void ApplyMove(Vector2 moveVector)
    {
        _appliedMoveVector = moveVector;
        _movement.OnMove(moveVector);

        Vector3 worldMoveDirection = ConvertMoveVectorToWorldDirection(moveVector);

        if (worldMoveDirection != Vector3.zero)
        {
            _lastWorldMoveDirection = worldMoveDirection;
        }

        _animator.SetWorldMoveDirection(worldMoveDirection);
    }

    private Vector3 ConvertMoveVectorToWorldDirection(Vector2 moveVector)
    {
        Vector3 worldMoveDirection = new Vector3(moveVector.x, 0f, moveVector.y);
        return worldMoveDirection;
    }

    private IEnumerator SprintConsumeRoutine()
    {
        while (IsSprinting == true)
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
}

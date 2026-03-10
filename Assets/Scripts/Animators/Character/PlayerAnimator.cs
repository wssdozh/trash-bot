using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private readonly int _moveHash = Animator.StringToHash("Move");
    private readonly int _jumpHash = Animator.StringToHash("Jump");
    private readonly int _pointHash = Animator.StringToHash("Point");
    private readonly int _isFightHash = Animator.StringToHash("IsFight");
    private readonly int _attackHash = Animator.StringToHash("Attack");
    private readonly int _attackIndexHash = Animator.StringToHash("AttackIndex");
    private readonly int _takeDamageHash = Animator.StringToHash("TakeDamage");

    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveLerpSpeed = 5f;
    [SerializeField] private float _walkMoveValue = 0.5f;
    [SerializeField] private float _runMoveValue = 1f;
    [SerializeField] private float _moveDirectionDeadZone = 0.1f;

    [SerializeField] private int _attackVariantsCount = 3;

    private float _currentMove;
    private float _targetMove;
    private bool _isMoving;
    private bool _isSprinting;

    private Vector3 _worldMoveDirection;

    private StepAnimator _stepAnimator;

    private int _nextAttackIndex;
    private bool _hasMove;
    private bool _hasJump;
    private bool _hasPoint;
    private bool _hasIsFight;
    private bool _hasAttack;
    private bool _hasAttackIndex;
    private bool _hasTakeDamage;

    private void Awake()
    {
        if (_animator == null)
        {
            throw new InvalidOperationException(nameof(_animator));
        }

        CacheParameters();
        _stepAnimator = new StepAnimator(_animator, transform, _moveDirectionDeadZone);
        _worldMoveDirection = Vector3.zero;
        _nextAttackIndex = 0;
    }

    private void Update()
    {
        UpdateMove();
        _stepAnimator.UpdateStepFromMoveDirection(_isMoving, _worldMoveDirection);
    }

    public void SetMoveState(bool isMoving)
    {
        _isMoving = isMoving;

        if (_isMoving == false)
        {
            _worldMoveDirection = Vector3.zero;
        }
    }

    public void SetSprintState(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    public void SetWorldMoveDirection(Vector3 worldMoveDirection)
    {
        _worldMoveDirection = worldMoveDirection;
    }

    public void TriggerJump()
    {
        if (_hasJump == false)
        {
            return;
        }

        _animator.SetTrigger(_jumpHash);
    }

    public void TriggerPoint()
    {
        if (_hasPoint == false)
        {
            return;
        }

        _animator.SetTrigger(_pointHash);
    }

    public void TriggerAttack()
    {
        if (_hasAttack == false)
        {
            return;
        }

        int attackIndex = GetNextAttackIndex();

        if (_hasAttackIndex)
        {
            _animator.SetInteger(_attackIndexHash, attackIndex);
        }

        _animator.SetTrigger(_attackHash);
    }

    public void TriggerTakeDamage()
    {
        if (_hasTakeDamage == false)
        {
            return;
        }

        _animator.SetTrigger(_takeDamageHash);
    }

    public void SetFight(bool isFight)
    {
        if (_hasIsFight == false)
        {
            return;
        }

        _animator.SetBool(_isFightHash, isFight);
    }

    public void SetController(RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            throw new InvalidOperationException(nameof(controller));
        }

        _animator.runtimeAnimatorController = controller;
        _animator.Rebind();
        _animator.Update(0f);
        ClearParameters();
        CacheParameters();
        _stepAnimator = new StepAnimator(_animator, transform, _moveDirectionDeadZone);
        _currentMove = 0f;
        _targetMove = 0f;
        _nextAttackIndex = 0;
    }

    public void SetLayerWeight(int layerIndex, float layerWeight)
    {
        _animator.SetLayerWeight(layerIndex, layerWeight);
    }

    public void ResetAttackOrder()
    {
        _nextAttackIndex = 0;
    }

    private int GetNextAttackIndex()
    {
        if (_attackVariantsCount <= 1)
        {
            _nextAttackIndex = 0;
            return 0;
        }

        int selectedAttackIndex = _nextAttackIndex;

        _nextAttackIndex += 1;
        if (_nextAttackIndex >= _attackVariantsCount)
        {
            _nextAttackIndex = 0;
        }

        return selectedAttackIndex;
    }

    private void UpdateMove()
    {
        if (_hasMove == false)
        {
            return;
        }

        if (_isMoving == false)
        {
            _targetMove = 0f;
        }
        else
        {
            if (_isSprinting)
            {
                _targetMove = _runMoveValue;
            }
            else
            {
                _targetMove = _walkMoveValue;
            }
        }

        _currentMove = Mathf.MoveTowards(_currentMove, _targetMove, _moveLerpSpeed * Time.deltaTime);
        _animator.SetFloat(_moveHash, _currentMove);
    }

    private void CacheParameters()
    {
        AnimatorControllerParameter[] parameters = _animator.parameters;
        int parameterIndex = 0;

        while (parameterIndex < parameters.Length)
        {
            AnimatorControllerParameter parameter = parameters[parameterIndex];
            int hash = parameter.nameHash;

            if (parameter.type == AnimatorControllerParameterType.Float && hash == _moveHash)
            {
                _hasMove = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Trigger && hash == _jumpHash)
            {
                _hasJump = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Trigger && hash == _pointHash)
            {
                _hasPoint = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Bool && hash == _isFightHash)
            {
                _hasIsFight = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Trigger && hash == _attackHash)
            {
                _hasAttack = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Int && hash == _attackIndexHash)
            {
                _hasAttackIndex = true;
            }

            if (parameter.type == AnimatorControllerParameterType.Trigger && hash == _takeDamageHash)
            {
                _hasTakeDamage = true;
            }

            parameterIndex += 1;
        }
    }

    private void ClearParameters()
    {
        _hasMove = false;
        _hasJump = false;
        _hasPoint = false;
        _hasIsFight = false;
        _hasAttack = false;
        _hasAttackIndex = false;
        _hasTakeDamage = false;
    }
}

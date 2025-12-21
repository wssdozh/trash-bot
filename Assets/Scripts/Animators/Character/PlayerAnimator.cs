using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private static readonly int _moveHash = Animator.StringToHash("Move");
    private static readonly int _jumpHash = Animator.StringToHash("Jump");
    private static readonly int _pointHash = Animator.StringToHash("Point");
    private static readonly int _isFightHash = Animator.StringToHash("IsFight");
    private static readonly int _attackHash = Animator.StringToHash("Attack");
    private static readonly int _takeDamageHash = Animator.StringToHash("TakeDamage");

    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveLerpSpeed = 5f;
    [SerializeField] private float _walkMoveValue = 0.5f;
    [SerializeField] private float _runMoveValue = 1f;
    [SerializeField] private float _moveDirectionDeadZone = 0.0001f;

    private float _currentMove;
    private float _targetMove;
    private bool _isMoving;
    private bool _isSprinting;

    private StepAnimator _stepAnimator;

    private void Awake()
    {
        _stepAnimator = new StepAnimator(_animator, transform, _moveDirectionDeadZone);
    }

    private void Update()
    {
        UpdateMove();

        _stepAnimator.UpdateStepFromMovement(_isMoving);
    }

    public void SetMoveState(bool isMoving)
    {
        _isMoving = isMoving;
    }

    public void SetSprintState(bool isSprinting)
    {
        _isSprinting = isSprinting;
    }

    public void TriggerJump()
    {
        _animator.SetTrigger(_jumpHash);
    }

    public void TriggerPoint()
    {
        _animator.SetTrigger(_pointHash);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(_attackHash);
    }

    public void TriggerTakeDamage()
    {
        _animator.SetTrigger(_takeDamageHash);
    }

    public void SetFight(bool isFight)
    {
        _animator.SetBool(_isFightHash, isFight);
    }

    private void UpdateMove()
    {
        if (_isMoving == false)
        {
            _targetMove = 0f;
        }
        else
        {
            if (_isSprinting == true)
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

    public void TryStep(Vector3 worldMoveDirection)
    {
        _stepAnimator.TryStep(worldMoveDirection);
    }

    public void StopStep()
    {
        _stepAnimator.StopStep();
    }
}

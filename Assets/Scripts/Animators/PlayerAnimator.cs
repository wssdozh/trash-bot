using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private static readonly int MoveHash = Animator.StringToHash("Move");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int AscendHash = Animator.StringToHash("IsAscending");
    private static readonly int FallHash = Animator.StringToHash("IsFalling");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");

    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveLerpSpeed = 5f;
    [SerializeField] private float _walkMoveValue = 0.5f;
    [SerializeField] private float _runMoveValue = 1f;

    private float _currentMove;
    private float _targetMove;
    private bool _isMoving;
    private bool _isSprinting;

    private void Update()
    {
        UpdateMove();
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
        _animator.SetTrigger(JumpHash);
    }

    public void SetAscend(bool isAscending)
    {
        _animator.SetBool(AscendHash, isAscending);
    }

    public void SetFall(bool isFalling)
    {
        _animator.SetBool(FallHash, isFalling);
    }

    public void TriggerAttack()
    {
        _animator.SetTrigger(AttackHash);
    }

    public void TriggerTakeDamage()
    {
        _animator.SetTrigger(TakeDamageHash);
    }

    private void UpdateMove()
    {
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
        _animator.SetFloat(MoveHash, _currentMove);
    }
}

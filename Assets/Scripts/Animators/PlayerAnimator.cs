using UnityEngine;

public sealed class PlayerAnimator : MonoBehaviour
{
    private static readonly int _moveXHash = Animator.StringToHash("MoveX");
    private static readonly int _moveYHash = Animator.StringToHash("MoveY");
    private static readonly int _jumpHash = Animator.StringToHash("Jump");
    private static readonly int _pointHash = Animator.StringToHash("Point");
    private static readonly int _isFightHash = Animator.StringToHash("IsFight");
    private static readonly int _attackHash = Animator.StringToHash("Attack");
    private static readonly int _takeDamageHash = Animator.StringToHash("TakeDamage");

    [SerializeField] private Animator _animator;
    [SerializeField] private float _moveLerpSpeed = 8f;
    [SerializeField] private float _moveDirectionDeadZone = 0.0001f;
    [SerializeField] private float _runForwardMoveY = 2f;
    [SerializeField] private float _axisSwitchThreshold = 0.15f;

    private float _currentMoveX;
    private float _currentMoveY;
    private float _targetMoveX;
    private float _targetMoveY;

    private bool _isMoving;
    private bool _isSprinting;

    private StepAnimator _stepAnimatorLogic;

    private void Awake()
    {
        _stepAnimatorLogic = new StepAnimator(transform, _moveDirectionDeadZone, _runForwardMoveY);
    }

    private void Update()
    {
        Vector2 targetMove = _stepAnimatorLogic.UpdateMoveFromMovement(_isMoving, _isSprinting);
        _targetMoveX = targetMove.x;
        _targetMoveY = targetMove.y;

        if (_targetMoveX == 0f)
        {
            _currentMoveX = 0f;
        }

        if (_targetMoveY == 0f)
        {
            _currentMoveY = 0f;
        }

        _currentMoveX = Mathf.MoveTowards(_currentMoveX, _targetMoveX, _moveLerpSpeed * Time.deltaTime);
        _currentMoveY = Mathf.MoveTowards(_currentMoveY, _targetMoveY, _moveLerpSpeed * Time.deltaTime);

        _animator.SetFloat(_moveXHash, _currentMoveX);
        _animator.SetFloat(_moveYHash, _currentMoveY);
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

    public void TryStep(Vector3 worldMoveDirection)
    {
        Vector2 targetMove = _stepAnimatorLogic.GetMoveFromWorldDirection(worldMoveDirection, _isSprinting);
        _targetMoveX = targetMove.x;
        _targetMoveY = targetMove.y;
    }

    public void StopStep()
    {
        _targetMoveX = 0f;
        _targetMoveY = 0f;
    }
}

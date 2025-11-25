using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private static readonly int s_walkHash = Animator.StringToHash("Walk");
    private static readonly int s_runHash = Animator.StringToHash("Run");
    private static readonly int s_jumpHash = Animator.StringToHash("Jump");
    private static readonly int s_ascendHash = Animator.StringToHash("IsAscending");
    private static readonly int s_fallHash = Animator.StringToHash("IsFalling");
    private static readonly int s_attackHash = Animator.StringToHash("Attack");
    private static readonly int s_takeDamageHash = Animator.StringToHash("TakeDamage");

    [SerializeField] private Animator _animator;
    
    private bool _isJumping;

    public void TriggerAttack()
    {
        _animator.SetTrigger(s_attackHash);
    }

    public void TriggerTakeDamage()
    {
        _animator.SetTrigger(s_takeDamageHash);
    }

    public void SetJump(bool isJumping)
    {
        _isJumping = isJumping;
        _animator.SetBool(s_jumpHash, isJumping);
    }

    public void SetRun(bool isRunning)
    {
        bool shouldRun = isRunning && _isJumping == false;
        _animator.SetBool(s_runHash, shouldRun);
    }

    public void SetAscend(bool isAscending)
    {
        _animator.SetBool(s_ascendHash, isAscending);
    }

    public void SetFall(bool isFalling)
    {
        _animator.SetBool(s_fallHash, isFalling);
    }
}
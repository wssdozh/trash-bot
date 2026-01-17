using UnityEngine;

public class AttackEndedStateBehaviour : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // if (layerIndex != 1)
        // {
        //     return;
        // }

        PlayerAnimationEvents playerAnimationEvents = animator.GetComponent<PlayerAnimationEvents>();

        if (playerAnimationEvents == null)
        {
            return;
        }

        playerAnimationEvents.InvokeAttackEndedEvent();
    }
}

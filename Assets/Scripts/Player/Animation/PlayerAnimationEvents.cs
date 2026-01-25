using System;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public event Action Attacking;
    public event Action AttackEnded;

    public void InvokeAttackingEvent()
    {
        Attacking?.Invoke();
    }

    public void InvokeAttackEndedEvent()
    {
        AttackEnded?.Invoke();
    }
}

using System;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public event Action Attacking;
    public event Action AttackEnded;
    public event Action Stepped;

    public void InvokeAttackingEvent()
    {
        Attacking?.Invoke();
    }

    public void InvokeAttackEndedEvent()
    {
        AttackEnded?.Invoke();
    }

    public void InvokeStepEvent()
    {
        Stepped?.Invoke();
    }
}

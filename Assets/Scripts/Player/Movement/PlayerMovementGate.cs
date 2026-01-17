using System;
using UnityEngine;

public class PlayerMovementGate : MonoBehaviour
{
    public bool IsMovementAllowed { get; private set; }

    public event Action<bool> MovementAllowedChanged;

    private void Awake()
    {
        IsMovementAllowed = true;
    }

    public void BlockMovement()
    {
        SetMovementAllowed(false);
    }

    public void AllowMovement()
    {
        SetMovementAllowed(true);
    }

    private void SetMovementAllowed(bool isAllowed)
    {
        if (IsMovementAllowed == isAllowed)
        {
            return;
        }

        IsMovementAllowed = isAllowed;

        if (MovementAllowedChanged != null)
        {
            MovementAllowedChanged.Invoke(IsMovementAllowed);
        }
    }
}

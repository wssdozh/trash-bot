using System;
using UnityEngine;

public abstract class BaseMenuView : MonoBehaviour
{
    public Action Opened;
    public Action Closed;
    public abstract bool IsOpen { get; protected set; }
    public abstract bool IsAnimating { get; protected set; }

    public virtual void Show()
    {
        if (IsAnimating || IsOpen)
        {
            return;
        }

        IsOpen = true;

        Opened?.Invoke();
    }

    public virtual void Hide()
    {
        if (IsAnimating || IsOpen == false)
        {
            return;
        }

        IsOpen = false;

        Closed?.Invoke();
    }
}
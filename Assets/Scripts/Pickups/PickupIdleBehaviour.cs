using UnityEngine;

public abstract class PickupIdleBehaviour : MonoBehaviour
{
    private bool _isIdleActive;

    public void SetIdleActive(bool isIdleActive)
    {
        if (_isIdleActive == isIdleActive)
        {
            return;
        }

        _isIdleActive = isIdleActive;

        if (_isIdleActive == true)
        {
            OnIdleActivated();

            return;
        }

        OnIdleDeactivated();
    }

    protected abstract void OnIdleActivated();
    protected abstract void OnIdleDeactivated();
}

using UnityEngine;

public abstract class HealthIndicatorBase<T> : MonoBehaviour where T : Health
{
    [SerializeField] protected T Health;

    protected virtual void OnEnable()
    {
        Health.Changed += Display;
    }

    protected virtual void OnDisable()
    {
        Health.Changed -= Display;
    }

    protected virtual void Start()
    {
        Display();
    }

    protected abstract void Display();
}
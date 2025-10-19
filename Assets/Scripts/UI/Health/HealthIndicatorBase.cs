using UnityEngine;

public abstract class HealthIndicatorBase : MonoBehaviour
{
    [SerializeField] protected Health Health;

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
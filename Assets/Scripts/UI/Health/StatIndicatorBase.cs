using UnityEngine;


public abstract class StatIndicatorBase<T> : MonoBehaviour where T : Stat
{
    [SerializeField] protected T Stat;

    protected virtual void OnEnable()
    {
        Stat.Changed += Display;
    }

    protected virtual void OnDisable()
    {
        Stat.Changed -= Display;
    }

    protected virtual void Start()
    {
        Display();
    }

    protected abstract void Display();
}
 
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private CharacterDied _characterDied;

    public event Action Died;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }
    }

    private void OnEnable()
    {
        _health.Ended += Die;
    }

    private void OnDisable()
    {
        _health.Ended -= Die;
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (_characterDied != null)
        {
            _characterDied.EnableRegdoll();
        }

        Action died = Died;

        if (died != null)
        {
            died.Invoke();
        }
    }
}

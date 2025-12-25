using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private CharacterDied _characterDied;

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
        _characterDied.EnableRegdoll();
    }
}
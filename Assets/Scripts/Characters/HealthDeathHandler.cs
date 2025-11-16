using UnityEngine;

public class HealthDeathHandler : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private GameObject _rootObject;
    [SerializeField] private Collider _rootCollider;

    private void OnEnable()
    {
        _health.Ended += OnEnded;
    }

    private void OnDisable()
    {
        _health.Ended -= OnEnded;
    }

    private void OnEnded()
    {
        _rootObject.SetActive(false);
        _rootCollider.enabled = false;
    }
}

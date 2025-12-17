using UnityEngine;

public class HealthFeedbackTrigger : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private FeedbackGroup _feedbackGroup;

    private void OnEnable()
    {
        _health.Decreased += OnDecreased;
    }

    private void OnDisable()
    {
        _health.Decreased -= OnDecreased;
    }

    private void OnDecreased()
    {
        _feedbackGroup.Play();
    }
}

using UnityEngine;

public class HealthFeedbackTrigger : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private FeedbackGroup _feedbackGroup;

    private bool _isSubscribed;

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(Health health, FeedbackGroup feedbackGroup)
    {
        _health = health;
        _feedbackGroup = feedbackGroup;

        if (isActiveAndEnabled == false)
        {
            return;
        }

        Subscribe();
    }

    private void Subscribe()
    {
        if (_health == null)
        {
            return;
        }

        if (_feedbackGroup == null)
        {
            return;
        }

        if (_isSubscribed)
        {
            return;
        }

        _health.Decreased += OnDecreased;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (_isSubscribed == false)
        {
            return;
        }

        if (_health == null)
        {
            _isSubscribed = false;

            return;
        }

        _health.Decreased -= OnDecreased;
        _isSubscribed = false;
    }

    private void OnDecreased()
    {
        if (_feedbackGroup == null)
        {
            return;
        }

        _feedbackGroup.Play();
    }
}

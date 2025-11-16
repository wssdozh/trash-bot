using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private TargetRotator _targetRotator;
    [SerializeField] private IdleRotator _idleRotator;

    private void OnEnable()
    {
        _targetVision.TargetFound += OnTargetFound;
        _targetVision.TargetLost += OnTargetLost;
    }

    private void OnDisable()
    {
        _targetVision.TargetFound -= OnTargetFound;
        _targetVision.TargetLost -= OnTargetLost;
    }

    private void Start()
    {
        SetIdleState();
    }

    private void OnTargetFound()
    {
        SetTrackingState();
    }

    private void OnTargetLost()
    {
        SetIdleState();
    }

    private void SetTrackingState()
    {
        _idleRotator.enabled = false;
        _targetRotator.enabled = true;
    }

    private void SetIdleState()
    {
        _targetRotator.enabled = false;
        _idleRotator.ResetBaseRotation();
        _idleRotator.enabled = true;
    }
}

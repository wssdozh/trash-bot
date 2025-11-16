using UnityEngine;

public class TargetRotator : MonoBehaviour
{
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private float _rotationSpeed = 10f;

    private void Update()
    {
        if (_targetVision.IsTargetVisible == false)
        {
            return;
        }

        Transform currentTarget = _targetVision.CurrentTarget;

        if (currentTarget == null)
        {
            return;
        }

        Vector3 directionToTarget = (currentTarget.position - _rotationPivot.position).normalized;

        if (directionToTarget.sqrMagnitude == 0f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        float step = _rotationSpeed * Time.deltaTime;

        _rotationPivot.rotation = Quaternion.Lerp(
            _rotationPivot.rotation,
            targetRotation,
            step
        );
    }
}

using UnityEngine;

public class TargetRotator : MonoBehaviour
{
    [SerializeField] private TargetVision _targetVision;
    [SerializeField] private Transform _rotationPivot;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _maxPitchAngle = 89f;

    private Vector3 _aimPoint;
    private AimRotationSolver _aimRotationSolver;
    private bool _hasAimPoint;

    private void Awake()
    {
        if (_rotationPivot == null)
        {
            throw new System.InvalidOperationException(nameof(_rotationPivot));
        }

        _aimRotationSolver = new AimRotationSolver(_maxPitchAngle);
    }

    public void SetAimPoint(Vector3 aimPoint)
    {
        _aimPoint = aimPoint;
        _hasAimPoint = true;
    }

    public void ClearAimPoint()
    {
        _hasAimPoint = false;
    }

    private void Update()
    {
        Vector3 targetPoint;

        if (TryGetTargetPoint(out targetPoint) == false)
        {
            return;
        }

        Vector3 directionToTarget = targetPoint - _rotationPivot.position;

        if (directionToTarget.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Quaternion targetRotation;
        bool hasTargetRotation = _aimRotationSolver.TryGetRotation(
            _rotationPivot.position,
            _rotationPivot.root.up,
            targetPoint,
            out targetRotation);

        if (hasTargetRotation == false)
        {
            return;
        }

        float step = _rotationSpeed * Time.deltaTime;

        _rotationPivot.rotation = Quaternion.Lerp(
            _rotationPivot.rotation,
            targetRotation,
            step
        );
    }

    private bool TryGetTargetPoint(out Vector3 targetPoint)
    {
        if (_targetVision.IsTargetVisible)
        {
            Transform currentTarget = _targetVision.CurrentTarget;

            if (currentTarget != null)
            {
                targetPoint = _targetVision.CurrentTargetPoint;

                return true;
            }
        }

        if (_hasAimPoint)
        {
            targetPoint = _aimPoint;

            return true;
        }

        targetPoint = Vector3.zero;

        return false;
    }
}

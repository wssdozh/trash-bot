using System;
using UnityEngine;

public sealed class AimRotationSolver
{
    private const float ZeroThreshold = 0.0001f;

    private readonly float _maxPitchAngle;

    public AimRotationSolver(float maxPitchAngle)
    {
        if (maxPitchAngle <= 0f)
        {
            throw new InvalidOperationException(nameof(maxPitchAngle));
        }

        _maxPitchAngle = maxPitchAngle;
    }

    public bool TryGetRotation(Vector3 originPoint, Vector3 upAxis, Vector3 aimPoint, out Quaternion targetRotation)
    {
        Vector3 desiredDirection = aimPoint - originPoint;

        if (desiredDirection.sqrMagnitude <= ZeroThreshold)
        {
            targetRotation = Quaternion.identity;

            return false;
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(desiredDirection, upAxis);

        if (flatDirection.sqrMagnitude <= ZeroThreshold)
        {
            targetRotation = Quaternion.identity;

            return false;
        }

        Quaternion yawRotation = Quaternion.LookRotation(flatDirection, upAxis);
        Vector3 rightAxis = yawRotation * Vector3.right;
        float pitchAngle = Vector3.SignedAngle(flatDirection, desiredDirection, rightAxis);
        float clampedPitchAngle = Mathf.Clamp(pitchAngle, -_maxPitchAngle, _maxPitchAngle);
        Quaternion pitchRotation = Quaternion.AngleAxis(clampedPitchAngle, Vector3.right);
        targetRotation = yawRotation * pitchRotation;

        return true;
    }
}

using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private Vector3 GetMoveDirection(Vector3 currentPoint)
        {
            Vector3 pathDirection = GetPathDirection(currentPoint);
            Vector3 desiredDirection = GetPlanarDirection(_navMeshAgent.desiredVelocity);
            Vector3 moveDirection = Vector3.zero;

            if (pathDirection.sqrMagnitude > MinSqrMagnitude)
            {
                if (desiredDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    float directionDot = Vector3.Dot(pathDirection, desiredDirection);

                    if (directionDot > 0f)
                    {
                        moveDirection = GetPlanarDirection(pathDirection + desiredDirection);
                    }
                }

                if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
                {
                    moveDirection = pathDirection;
                }
            }

            else if (desiredDirection.sqrMagnitude > MinSqrMagnitude)
            {
                moveDirection = desiredDirection;
            }

            else if (_navMeshAgent.hasPath)
            {
                Vector3 steeringPoint = GetPlanarPosition(_navMeshAgent.steeringTarget);
                Vector3 steeringDirection = GetPlanarDirection(steeringPoint - currentPoint);

                if (steeringDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    moveDirection = steeringDirection;
                }
            }

            if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                moveDirection = GetPlanarDirection(_desiredPoint - currentPoint);
            }

            if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            Vector3 steerDirection = GetSteerDirection(currentPoint, moveDirection);

            if (steerDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return moveDirection;
            }

            return steerDirection;
        }

        private Vector3 GetPathDirection(Vector3 currentPoint)
        {
            if (_navMeshAgent.hasPath == false)
            {
                return Vector3.zero;
            }

            Vector3[] corners = _navMeshAgent.path.corners;

            if (corners == null)
            {
                return Vector3.zero;
            }

            if (corners.Length == 0)
            {
                return Vector3.zero;
            }

            Vector3 segmentStart = currentPoint;
            Vector3 lookPoint = currentPoint;
            float remainingDistance = Mathf.Max(PathLookDistance, _config.ProbeRadius * 2f);
            int cornerIndex = 0;

            while (cornerIndex < corners.Length)
            {
                Vector3 cornerPoint = GetPlanarPosition(corners[cornerIndex]);
                Vector3 segment = cornerPoint - segmentStart;
                float segmentLength = segment.magnitude;

                if (segmentLength <= MinSqrMagnitude)
                {
                    cornerIndex += 1;

                    continue;
                }

                if (remainingDistance <= segmentLength)
                {
                    Vector3 segmentDirection = segment / segmentLength;
                    lookPoint = segmentStart + segmentDirection * remainingDistance;

                    break;
                }

                remainingDistance -= segmentLength;
                segmentStart = cornerPoint;
                lookPoint = cornerPoint;
                cornerIndex += 1;
            }

            return GetPlanarDirection(lookPoint - currentPoint);
        }

        private void RotateBase(Vector3 currentPoint, Vector3 targetPoint, Vector3 moveDirection, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 desiredLookDirection = ResolveFacingDirection(currentPoint, targetPoint, moveDirection, targetPointType);

            if (desiredLookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            float turnSpeed = GetTurnSpeed(currentPoint, targetPoint, desiredLookDirection);
            RotateBaseTowardsDirection(desiredLookDirection, turnSpeed);
        }

        private Vector3 ResolveFacingDirection(Vector3 currentPoint, Vector3 targetPoint, Vector3 moveDirection, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 targetDirection = GetPlanarDirection(targetPoint - currentPoint);

            if (targetDirection.sqrMagnitude > MinSqrMagnitude)
            {
                if (ShouldUseTargetFacing(targetPointType))
                {
                    return targetDirection;
                }
            }

            if (moveDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return ResolveTravelFacingDirection(moveDirection);
            }

            if (targetDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return targetDirection;
            }

            return Vector3.zero;
        }

        private Vector3 ResolveTravelFacingDirection(Vector3 desiredTravelDirection)
        {
            Vector3 planarDirection = GetPlanarDirection(desiredTravelDirection);

            if (planarDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            return planarDirection;
        }

        private bool ShouldUseTargetFacing(BossExcavatorTargetPoint targetPointType)
        {
            if (targetPointType == BossExcavatorTargetPoint.ChargeAlign)
            {
                return true;
            }

            if (_attackIntent == BossExcavatorAttack.BucketStrike)
            {
                return true;
            }

            if (_attackIntent == BossExcavatorAttack.Sweep)
            {
                return true;
            }

            if (_attackIntent == BossExcavatorAttack.ThrowScrap)
            {
                return true;
            }

            if (_attackIntent == BossExcavatorAttack.Charge)
            {
                return true;
            }

            return false;
        }

        private Vector3 GetNavigationLookDirection(Vector3 currentPoint, Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return moveDirection;
            }

            Vector3 steeringDirection = Vector3.zero;

            if (_navMeshAgent != null)
            {
                Vector3 steeringPoint = GetPlanarPosition(_navMeshAgent.steeringTarget);
                steeringDirection = GetPlanarDirection(steeringPoint - currentPoint);
            }

            if (steeringDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return steeringDirection;
            }

            return Vector3.zero;
        }

        private float GetTurnSpeed(Vector3 currentPoint, Vector3 targetPoint, Vector3 lookDirection)
        {
            Vector3 forward = GetPlanarForward(_base.forward);
            float turnAngle = Vector3.Angle(forward, lookDirection);
            float angleBlend = Mathf.InverseLerp(_config.MoveStartAngle, 180f, turnAngle);
            float angleMultiplier = Mathf.Lerp(1f, 1.35f, angleBlend);
            float targetDistance = Vector3.Distance(currentPoint, targetPoint);
            float attackDistance = GetAttackChaseDistance() + GetDistanceTolerance();
            float pressureMultiplier = 1f;

            if (targetDistance <= attackDistance)
            {
                float nearDistance = GetMinMoveDistance();
                float distanceBlend = 1f;

                if (attackDistance > nearDistance)
                {
                    distanceBlend = 1f - Mathf.InverseLerp(nearDistance, attackDistance, targetDistance);
                }

                pressureMultiplier = Mathf.Lerp(1f, 1.12f, distanceBlend);
            }

            if (_targetPoint == BossExcavatorTargetPoint.ChargeAlign)
            {
                pressureMultiplier *= 1.3f;
            }

            if (_attackIntent == BossExcavatorAttack.BucketStrike)
            {
                if (targetDistance <= _config.BucketMaxDistance + GetDistanceTolerance())
                {
                    pressureMultiplier *= 1.18f;
                }
            }

            return _config.BaseTurnSpeed * angleMultiplier * pressureMultiplier;
        }

        private void MoveBase(
            Vector3 currentPoint,
            Vector3 moveDirection,
            float stopDistance)
        {
            float targetDistance = Vector3.Distance(currentPoint, _desiredPoint);

            if (targetDistance <= stopDistance)
            {
                _currentPlanarSpeed = 0f;
                _currentMoveDirection = Vector3.zero;
                Vector3 currentVelocity = _baseRigidbody.linearVelocity;
                currentVelocity.x = 0f;
                currentVelocity.z = 0f;
                _baseRigidbody.linearVelocity = currentVelocity;

                return;
            }

            if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                _currentPlanarSpeed = 0f;
                _currentMoveDirection = Vector3.zero;
                ResetPlanarVelocity();

                return;
            }

            Vector3 desiredDriveDirection = moveDirection.normalized;
            Vector3 driveDirection = GetPlanarForward(_base.forward);
            float facingAngle = Vector3.Angle(driveDirection, desiredDriveDirection);
            float bodyDriveFactor = GetBodyDriveSpeedFactor(facingAngle);

            if (bodyDriveFactor <= 0f)
            {
                _currentPlanarSpeed = 0f;
                _currentMoveDirection = Vector3.zero;
                ResetPlanarVelocity();

                return;
            }

            float speedFactor = GetPathTurnSpeedFactor(currentPoint, desiredDriveDirection) * bodyDriveFactor;
            float moveDistance = _config.BaseMoveSpeed * speedFactor * Time.fixedDeltaTime;
            float maxDistance = Mathf.Max(0f, targetDistance - stopDistance);

            if (moveDistance > maxDistance)
            {
                moveDistance = maxDistance;
            }

            if (moveDistance <= 0f)
            {
                _currentPlanarSpeed = 0f;
                _currentMoveDirection = Vector3.zero;
                return;
            }

            Vector3 nextPlanarPosition = currentPoint + driveDirection * moveDistance;
            Vector3 nextPosition = new Vector3(nextPlanarPosition.x, _baseRigidbody.position.y, nextPlanarPosition.z);

            _currentPlanarSpeed = moveDistance / Time.fixedDeltaTime;
            _currentMoveDirection = driveDirection;
            _baseRigidbody.MovePosition(nextPosition);
            ResetPlanarVelocity();
            _navMeshAgent.nextPosition = GetPlanarPosition(nextPosition);
        }

        private void ResetPlanarVelocity()
        {
            Vector3 currentVelocity = _baseRigidbody.linearVelocity;
            currentVelocity.x = 0f;
            currentVelocity.z = 0f;
            _baseRigidbody.linearVelocity = currentVelocity;
        }

        private float GetPathTurnSpeedFactor(Vector3 currentPoint, Vector3 moveDirection)
        {
            Vector3 navigationDirection = GetNavigationLookDirection(currentPoint, moveDirection);

            if (navigationDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return 1f;
            }

            float turnAngle = Vector3.Angle(moveDirection, navigationDirection);
            float turnBlend = Mathf.InverseLerp(PathTurnSlowStartAngle, PathTurnSlowStopAngle, turnAngle);

            return Mathf.Lerp(1f, MinPathTurnSpeedFactor, turnBlend);
        }

        private float GetBodyDriveSpeedFactor(float facingAngle)
        {
            float driveBlend = Mathf.InverseLerp(_config.MoveStartAngle, _config.MoveStopAngle, facingAngle);

            return 1f - driveBlend;
        }
    }
}

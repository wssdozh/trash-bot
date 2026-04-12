using UnityEngine;

public sealed partial class EnemySteering
{
    public bool ChaseTarget(Vector3 targetPoint, float ringDistance, float ringTolerance, float lookBlend)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = GetFlatPoint(targetPoint);
        Vector3 toTarget = flatTargetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 chasePoint = GetChasePoint(currentPoint, flatTargetPoint, targetDirection, ringDistance, toTarget.magnitude);
        Vector3 safePoint = GetSafePoint(chasePoint, _probeRadius);
        float stopDistance = Mathf.Max(ringTolerance, 0.05f);

        if (lookBlend > 0f)
        {
            return MoveToPoint(safePoint, stopDistance, lookBlend, flatTargetPoint);
        }

        return MoveToPoint(safePoint, stopDistance);
    }

    public bool OrbitTarget(Transform target, float ringDistance, float ringTolerance, bool isClockwise)
    {
        if (target == null)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 targetPoint = GetFlatPoint(target.position);
        Vector3 toTarget = targetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 tangentDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 ringDirection = Vector3.zero;
        float targetDistance = toTarget.magnitude;

        if (targetDistance > ringDistance + ringTolerance)
        {
            ringDirection = targetDirection;
        }

        else if (targetDistance < ringDistance - ringTolerance)
        {
            ringDirection = -targetDirection;
        }

        Vector3 desiredDirection = (tangentDirection * _orbitWeight) + (ringDirection * _ringWeight);
        desiredDirection = GetFlatDirection(desiredDirection);

        if (desiredDirection.sqrMagnitude <= MinDistance)
        {
            desiredDirection = tangentDirection;
        }

        Vector3 movePoint = currentPoint + (desiredDirection * Mathf.Max(_probeDistance, 0.6f));

        return MoveToPoint(movePoint, 0.05f, targetPoint);
    }

    public bool RecoverTarget(Transform target, bool isClockwise)
    {
        if (target == null)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 targetPoint = GetFlatPoint(target.position);
        Vector3 toTarget = targetPoint - currentPoint;

        if (toTarget.sqrMagnitude <= MinDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = toTarget.normalized;
        Vector3 sideDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 desiredDirection = (-targetDirection * _recoverBack) + (sideDirection * _recoverSide);
        desiredDirection = GetFlatDirection(desiredDirection);

        if (desiredDirection.sqrMagnitude <= MinDistance)
        {
            desiredDirection = -targetDirection;
        }

        Vector3 movePoint = currentPoint + (desiredDirection * Mathf.Max(_probeDistance, 0.6f));

        return MoveToPoint(movePoint, 0.05f, targetPoint);
    }

    private Vector3 GetSeparationDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (_allyMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_separationRadius <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _separationRadius,
            _allyBuffer,
            _allyMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 separationDirection = Vector3.zero;
        int uniqueCount = 0;
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _allyBuffer[hitIndex];

            if (hitCollider != null)
            {
                Enemy otherEnemy = hitCollider.GetComponentInParent<Enemy>();

                if (otherEnemy != null)
                {
                    if (otherEnemy.gameObject != _root.gameObject && otherEnemy.IsDead == false)
                    {
                        int otherId = otherEnemy.gameObject.GetInstanceID();

                        if (ContainsAlly(otherId, uniqueCount) == false)
                        {
                            if (uniqueCount < _allyIdBuffer.Length)
                            {
                                _allyIdBuffer[uniqueCount] = otherId;
                                uniqueCount += 1;
                            }

                            Vector3 otherPoint = GetFlatPoint(otherEnemy.transform.position);
                            Vector3 awayDirection = currentPoint - otherPoint;
                            float distance = awayDirection.magnitude;

                            if (distance > MinDistance)
                            {
                                Vector3 lateralDirection = awayDirection / distance;
                                float frontWeight = 1f;

                                if (moveDirection.sqrMagnitude > MinDistance)
                                {
                                    Vector3 toOtherDirection = (otherPoint - currentPoint) / distance;
                                    frontWeight = Mathf.Clamp01(Vector3.Dot(moveDirection, toOtherDirection));

                                    if (frontWeight <= MinDistance)
                                    {
                                        hitIndex += 1;

                                        continue;
                                    }

                                    lateralDirection = Vector3.ProjectOnPlane(lateralDirection, moveDirection);
                                    lateralDirection = GetFlatDirection(lateralDirection);

                                    if (lateralDirection.sqrMagnitude <= MinDistance)
                                    {
                                        hitIndex += 1;

                                        continue;
                                    }
                                }

                                float weight = 1f - Mathf.Clamp01(distance / _separationRadius);
                                weight *= frontWeight;
                                separationDirection += lateralDirection * weight;
                            }
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        return GetFlatVector(separationDirection);
    }

    private bool ContainsAlly(int allyId, int uniqueCount)
    {
        int allyIndex = 0;

        while (allyIndex < uniqueCount)
        {
            if (_allyIdBuffer[allyIndex] == allyId)
            {
                return true;
            }

            allyIndex += 1;
        }

        return false;
    }

    private Vector3 GetSlotPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 targetDirection, float ringDistance, float slotOffset)
    {
        Vector3 fromTargetDirection = -targetDirection;
        Vector3 slotDirection = RotateDirection(fromTargetDirection, slotOffset);
        Vector3 slotPoint = targetPoint + (slotDirection * ringDistance);
        slotPoint.y = currentPoint.y;

        return slotPoint;
    }

    private float GetChaseDistance(float targetDistance, float ringDistance)
    {
        float maxDistance = Mathf.Max(_slotRadius, ringDistance);
        float blendMaxDistance = maxDistance + ringDistance;

        if (targetDistance >= blendMaxDistance)
        {
            return maxDistance;
        }

        if (targetDistance <= ringDistance)
        {
            return ringDistance;
        }

        float blend = Mathf.InverseLerp(ringDistance, blendMaxDistance, targetDistance);

        return Mathf.Lerp(ringDistance, maxDistance, blend);
    }

    private Vector3 GetChasePoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 targetDirection, float ringDistance, float targetDistance)
    {
        Vector3 chasePoint = targetPoint - (targetDirection * ringDistance);
        chasePoint.y = currentPoint.y;

        if (CanUseSlot(targetDistance, ringDistance) == false)
        {
            return chasePoint;
        }

        float slotOffset = GetSlotOffset(targetPoint);

        if (Mathf.Abs(slotOffset) <= SlotOffsetMin)
        {
            return chasePoint;
        }

        if (_slotWeight <= MinDistance)
        {
            return chasePoint;
        }

        slotOffset *= _slotWeight;
        float chaseDistance = GetChaseDistance(targetDistance, ringDistance);

        return GetSlotPoint(currentPoint, targetPoint, targetDirection, chaseDistance, slotOffset);
    }

    private bool CanUseSlot(float targetDistance, float ringDistance)
    {
        float slotUseDistance = Mathf.Max(_slotRadius, ringDistance) + _separationRadius;

        if (targetDistance > slotUseDistance)
        {
            return false;
        }

        return true;
    }

    private float GetSlotOffset(Vector3 targetPoint)
    {
        if (_slotCount <= 1)
        {
            return 0f;
        }

        if (_allyMask.value == 0)
        {
            return 0f;
        }

        if (_slotRadius <= MinDistance)
        {
            return 0f;
        }

        if (_slotAngle <= 0f)
        {
            return 0f;
        }

        float searchRadius = Mathf.Max(_slotRadius * 2f, _separationRadius * 2f);
        Vector3 origin = targetPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            searchRadius,
            _allyBuffer,
            _allyMask,
            QueryTriggerInteraction.Ignore);
        int uniqueCount = 0;
        int rank = 0;
        int currentId = _root.gameObject.GetInstanceID();
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _allyBuffer[hitIndex];

            if (hitCollider != null)
            {
                Enemy otherEnemy = hitCollider.GetComponentInParent<Enemy>();

                if (otherEnemy != null)
                {
                    if (otherEnemy.gameObject != _root.gameObject && otherEnemy.IsDead == false)
                    {
                        int otherId = otherEnemy.gameObject.GetInstanceID();

                        if (ContainsAlly(otherId, uniqueCount) == false)
                        {
                            if (uniqueCount < _allyIdBuffer.Length)
                            {
                                _allyIdBuffer[uniqueCount] = otherId;
                                uniqueCount += 1;
                            }

                            if (otherId < currentId)
                            {
                                rank += 1;
                            }
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        int slotIndex = rank % _slotCount;

        if (slotIndex == 0)
        {
            return 0f;
        }

        int offsetStep = ((slotIndex - 1) / 2) + 1;
        float angle = _slotAngle * offsetStep;

        if (slotIndex % 2 == 0)
        {
            angle = -angle;
        }

        return angle;
    }

    private Vector3 GetOrbitDirection(Vector3 targetDirection, bool isClockwise)
    {
        if (isClockwise)
        {
            return Vector3.Cross(targetDirection, Vector3.up);
        }

        return Vector3.Cross(Vector3.up, targetDirection);
    }
}

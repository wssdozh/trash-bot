using System;
using UnityEngine;

public sealed class EnemySteering
{
    private const int ProbeBufferSize = 8;
    private const int AllyBufferSize = 16;

    private readonly Transform _root;
    private readonly EnemyMove _enemyMove;
    private readonly EnemyRotator _enemyRotator;
    private readonly RaycastHit[] _probeBuffer = new RaycastHit[ProbeBufferSize];
    private readonly Collider[] _allyBuffer = new Collider[AllyBufferSize];
    private readonly int[] _allyIdBuffer = new int[AllyBufferSize];

    private LayerMask _obstacleMask;
    private float _probeRadius;
    private float _probeHeight;
    private float _probeDistance;
    private float _probeAngle;
    private float _avoidWeight;
    private LayerMask _allyMask;
    private float _separationRadius;
    private float _separationWeight;
    private float _orbitWeight;
    private float _ringWeight;
    private float _slotWeight;
    private float _slotAngle;
    private float _slotRadius;
    private int _slotCount;
    private float _recoverBack;
    private float _recoverSide;

    public EnemySteering(Transform root, EnemyMove enemyMove, EnemyRotator enemyRotator)
    {
        if (root == null)
        {
            throw new InvalidOperationException(nameof(root));
        }

        if (enemyMove == null)
        {
            throw new InvalidOperationException(nameof(enemyMove));
        }

        if (enemyRotator == null)
        {
            throw new InvalidOperationException(nameof(enemyRotator));
        }

        _root = root;
        _enemyMove = enemyMove;
        _enemyRotator = enemyRotator;
        _slotCount = 1;
    }

    public void SetObstacle(LayerMask obstacleMask, float probeRadius, float probeHeight, float probeDistance, float probeAngle, float avoidWeight)
    {
        _obstacleMask = obstacleMask;
        _probeRadius = probeRadius;
        _probeHeight = probeHeight;
        _probeDistance = probeDistance;
        _probeAngle = probeAngle;
        _avoidWeight = avoidWeight;
    }

    public void SetSpacing(LayerMask allyMask, float separationRadius, float separationWeight)
    {
        _allyMask = allyMask;
        _separationRadius = separationRadius;
        _separationWeight = separationWeight;
    }

    public void SetCombat(float orbitWeight, float ringWeight, float slotWeight, float slotAngle, float slotRadius, int slotCount, float recoverBack, float recoverSide)
    {
        _orbitWeight = orbitWeight;
        _ringWeight = ringWeight;
        _slotWeight = slotWeight;
        _slotAngle = slotAngle;
        _slotRadius = slotRadius;
        _slotCount = Mathf.Max(slotCount, 1);
        _recoverBack = recoverBack;
        _recoverSide = recoverSide;
    }

    public bool MoveToPoint(Vector3 targetPoint, float stopDistance)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = GetFlatPoint(targetPoint);
        Vector3 vectorToPoint = flatTargetPoint - currentPoint;
        float distance = vectorToPoint.magnitude;

        _enemyRotator.RotateToPoint(flatTargetPoint);

        if (distance <= stopDistance)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 seekDirection = vectorToPoint / distance;
        Vector3 desiredDirection = seekDirection;
        desiredDirection += GetAvoidDirection(seekDirection) * _avoidWeight;
        desiredDirection += GetSeparationDirection(currentPoint) * _separationWeight;

        return TryApplyDirection(desiredDirection, seekDirection);
    }

    public bool ChaseTarget(Transform target, float ringDistance, float ringTolerance, bool isClockwise)
    {
        if (target == null)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 targetPoint = GetFlatPoint(target.position);
        Vector3 vectorToTarget = targetPoint - currentPoint;
        float distance = vectorToTarget.magnitude;

        _enemyRotator.RotateToPoint(targetPoint);

        if (distance <= 0.0001f)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = vectorToTarget / distance;
        Vector3 slotPoint = GetSlotPoint(currentPoint, targetPoint, targetDirection, ringDistance, isClockwise);
        Vector3 slotDirection = GetFlatDirection(slotPoint - currentPoint);
        Vector3 ringDirection = Vector3.zero;

        if (distance > ringDistance + ringTolerance)
        {
            ringDirection = targetDirection;
        }

        else if (distance < ringDistance - ringTolerance)
        {
            ringDirection = -targetDirection;
        }

        Vector3 desiredDirection = slotDirection;
        desiredDirection += ringDirection * _ringWeight;
        desiredDirection += GetAvoidDirection(slotDirection) * _avoidWeight;
        desiredDirection += GetSeparationDirection(currentPoint) * _separationWeight;

        return TryApplyDirection(desiredDirection, slotDirection);
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
        Vector3 vectorToTarget = targetPoint - currentPoint;
        float distance = vectorToTarget.magnitude;

        _enemyRotator.RotateToPoint(targetPoint);

        if (distance <= 0.0001f)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = vectorToTarget / distance;
        Vector3 slotPoint = GetSlotPoint(currentPoint, targetPoint, targetDirection, ringDistance, isClockwise);
        Vector3 slotDirection = GetFlatDirection(slotPoint - currentPoint);
        Vector3 tangentDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 ringDirection = Vector3.zero;

        if (distance > ringDistance + ringTolerance)
        {
            ringDirection = targetDirection;
        }

        else if (distance < ringDistance - ringTolerance)
        {
            ringDirection = -targetDirection;
        }

        Vector3 desiredDirection = tangentDirection * _orbitWeight;
        desiredDirection += slotDirection * _slotWeight;
        desiredDirection += ringDirection * _ringWeight;
        desiredDirection += GetAvoidDirection(tangentDirection) * _avoidWeight;
        desiredDirection += GetSeparationDirection(currentPoint) * _separationWeight;

        return TryApplyDirection(desiredDirection, tangentDirection);
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
        Vector3 vectorToTarget = targetPoint - currentPoint;
        float distance = vectorToTarget.magnitude;

        _enemyRotator.RotateToPoint(targetPoint);

        if (distance <= 0.0001f)
        {
            _enemyMove.StopMove();

            return false;
        }

        Vector3 targetDirection = vectorToTarget / distance;
        Vector3 sideDirection = GetOrbitDirection(targetDirection, isClockwise);
        Vector3 desiredDirection = (-targetDirection * _recoverBack) + (sideDirection * _recoverSide);
        desiredDirection += GetAvoidDirection(-targetDirection) * _avoidWeight;
        desiredDirection += GetSeparationDirection(currentPoint) * _separationWeight;

        return TryApplyDirection(desiredDirection, -targetDirection);
    }

    public void LookToPoint(Vector3 targetPoint)
    {
        _enemyMove.StopMove();
        _enemyRotator.RotateToPoint(GetFlatPoint(targetPoint));
    }

    public void Stop()
    {
        _enemyMove.StopMove();
    }

    public bool HasPointClearance(Vector3 point)
    {
        Vector3 origin = GetFlatPoint(point) + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _probeRadius,
            _allyBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _allyBuffer[hitIndex];

            if (hitCollider != null)
            {
                if (hitCollider.transform.IsChildOf(_root) == false)
                {
                    return false;
                }
            }

            hitIndex += 1;
        }

        return true;
    }

    public bool IsLineBlocked(Vector3 targetPoint)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 flatTargetPoint = GetFlatPoint(targetPoint);
        Vector3 direction = flatTargetPoint - currentPoint;
        float distance = direction.magnitude;

        if (distance <= 0.0001f)
        {
            return false;
        }

        direction /= distance;

        return IsDirectionBlocked(currentPoint, direction, distance);
    }

    private bool TryApplyDirection(Vector3 desiredDirection, Vector3 fallbackDirection)
    {
        Vector3 safeDirection = GetSafeDirection(desiredDirection, fallbackDirection);

        if (safeDirection.sqrMagnitude <= 0.0001f)
        {
            _enemyMove.StopMove();

            return false;
        }

        _enemyMove.SetDirection(safeDirection);

        return true;
    }

    private Vector3 GetSafeDirection(Vector3 desiredDirection, Vector3 fallbackDirection)
    {
        Vector3 baseDirection = GetFlatDirection(desiredDirection);

        if (baseDirection.sqrMagnitude <= 0.0001f)
        {
            baseDirection = GetFlatDirection(fallbackDirection);
        }

        if (baseDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);

        if (IsDirectionBlocked(currentPoint, baseDirection, _probeDistance) == false)
        {
            return baseDirection;
        }

        Vector3 secondDirection = GetFlatDirection(fallbackDirection);

        if (secondDirection.sqrMagnitude > 0.0001f)
        {
            if (IsDirectionBlocked(currentPoint, secondDirection, _probeDistance) == false)
            {
                return secondDirection;
            }
        }

        int turnStep = 1;

        while (turnStep <= 2)
        {
            float turnAngle = _probeAngle * turnStep;
            Vector3 leftDirection = RotateDirection(baseDirection, -turnAngle);

            if (IsDirectionBlocked(currentPoint, leftDirection, _probeDistance) == false)
            {
                return leftDirection;
            }

            Vector3 rightDirection = RotateDirection(baseDirection, turnAngle);

            if (IsDirectionBlocked(currentPoint, rightDirection, _probeDistance) == false)
            {
                return rightDirection;
            }

            turnStep += 1;
        }

        Vector3 leftSideDirection = RotateDirection(baseDirection, -90f);

        if (IsDirectionBlocked(currentPoint, leftSideDirection, _probeDistance) == false)
        {
            return leftSideDirection;
        }

        Vector3 rightSideDirection = RotateDirection(baseDirection, 90f);

        if (IsDirectionBlocked(currentPoint, rightSideDirection, _probeDistance) == false)
        {
            return rightSideDirection;
        }

        return Vector3.zero;
    }

    private Vector3 GetAvoidDirection(Vector3 forwardDirection)
    {
        Vector3 baseDirection = GetFlatDirection(forwardDirection);

        if (baseDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 avoidDirection = GetProbePush(baseDirection, _probeDistance);
        Vector3 leftDirection = RotateDirection(baseDirection, -_probeAngle);
        Vector3 rightDirection = RotateDirection(baseDirection, _probeAngle);
        avoidDirection += GetProbePush(leftDirection, _probeDistance * 0.85f) * 0.75f;
        avoidDirection += GetProbePush(rightDirection, _probeDistance * 0.85f) * 0.75f;

        return GetFlatDirection(avoidDirection);
    }

    private Vector3 GetProbePush(Vector3 direction, float distance)
    {
        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _probeRadius,
            direction,
            _probeBuffer,
            distance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        Vector3 nearestNormal = Vector3.zero;
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            RaycastHit hit = _probeBuffer[hitIndex];
            Collider hitCollider = hit.collider;

            if (hitCollider != null)
            {
                if (hitCollider.transform.IsChildOf(_root) == false)
                {
                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        nearestNormal = hit.normal;
                    }
                }
            }

            hitIndex += 1;
        }

        if (nearestDistance == float.MaxValue)
        {
            return Vector3.zero;
        }

        float hitFactor = 1f - Mathf.Clamp01(nearestDistance / distance);
        Vector3 pushDirection = GetFlatDirection(nearestNormal);

        if (pushDirection.sqrMagnitude <= 0.0001f)
        {
            pushDirection = -direction;
        }

        return pushDirection * hitFactor;
    }

    private Vector3 GetSeparationDirection(Vector3 currentPoint)
    {
        if (_allyMask.value == 0)
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
                            _allyIdBuffer[uniqueCount] = otherId;
                            uniqueCount += 1;
                            Vector3 awayDirection = currentPoint - GetFlatPoint(otherEnemy.transform.position);
                            float distance = awayDirection.magnitude;

                            if (distance > 0.0001f)
                            {
                                float weight = 1f - Mathf.Clamp01(distance / _separationRadius);
                                separationDirection += (awayDirection / distance) * weight;
                            }
                        }
                    }
                }
            }

            hitIndex += 1;
        }

        return GetFlatDirection(separationDirection);
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

    private Vector3 GetSlotPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 targetDirection, float ringDistance, bool isClockwise)
    {
        Vector3 fromTargetDirection = -targetDirection;
        float slotOffset = GetSlotOffset(targetPoint, isClockwise);
        Vector3 slotDirection = RotateDirection(fromTargetDirection, slotOffset);
        Vector3 slotPoint = targetPoint + (slotDirection * ringDistance);
        slotPoint.y = currentPoint.y;

        return slotPoint;
    }

    private float GetSlotOffset(Vector3 targetPoint, bool isClockwise)
    {
        if (_slotCount <= 1)
        {
            return 0f;
        }

        if (_allyMask.value == 0)
        {
            return 0f;
        }

        Vector3 origin = targetPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _slotRadius,
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
                            _allyIdBuffer[uniqueCount] = otherId;
                            uniqueCount += 1;

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

        if (isClockwise == false)
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

    private bool IsDirectionBlocked(Vector3 currentPoint, Vector3 direction, float distance)
    {
        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _probeRadius,
            direction,
            _probeBuffer,
            distance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _probeBuffer[hitIndex].collider;

            if (hitCollider != null)
            {
                if (hitCollider.transform.IsChildOf(_root) == false)
                {
                    return true;
                }
            }

            hitIndex += 1;
        }

        return false;
    }

    private Vector3 RotateDirection(Vector3 direction, float angle)
    {
        Vector3 flatDirection = GetFlatDirection(direction);

        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

        return GetFlatDirection(rotation * flatDirection);
    }

    private Vector3 GetFlatPoint(Vector3 point)
    {
        point.y = _root.position.y;

        return point;
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }
}

using UnityEngine;
using UnityEngine.AI;

public sealed partial class EnemySteering
{
    public bool ResolveOverlap()
    {
        Vector3 overlapPush = GetOverlapPush();

        if (overlapPush.sqrMagnitude <= MinDistance)
        {
            return false;
        }

        float pushDistance = overlapPush.magnitude;
        float resolveDistance = Mathf.Clamp(pushDistance, ResolveMinStep, ResolveMaxStep);
        Vector3 resolveVector = (overlapPush / pushDistance) * resolveDistance;

        SetDebugStatus("ResolveOverlap");
        SetDebugMoveResult(GetFlatPoint(_root.position) + resolveVector, overlapPush, resolveVector);
        ForceStop();
        ApplyResolve(resolveVector);
        SyncAgent(GetFlatPoint(_root.position));

        return true;
    }

    private Vector3 GetSteerDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        Vector3 baseDirection = GetFlatDirection(moveDirection);

        if (baseDirection.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 desiredDirection = baseDirection;
        Vector3 separationDirection = GetSeparationDirection(currentPoint, baseDirection);

        if (separationDirection.sqrMagnitude > MinDistance)
        {
            desiredDirection += separationDirection * _separationWeight;
        }

        if (ShouldUseObstacleAvoidance(currentPoint, baseDirection))
        {
            Vector3 avoidDirection = GetAvoidDirection(currentPoint, baseDirection);

            if (avoidDirection.sqrMagnitude > MinDistance)
            {
                desiredDirection += avoidDirection * _avoidWeight;
            }
        }

        Vector3 steerDirection = GetFlatDirection(desiredDirection);

        if (steerDirection.sqrMagnitude <= MinDistance)
        {
            steerDirection = baseDirection;
        }

        steerDirection = ResolveBlockedDirection(currentPoint, baseDirection, steerDirection);

        return StabilizeSteerDirection(currentPoint, baseDirection, steerDirection);
    }

    private bool ShouldUseObstacleAvoidance(Vector3 currentPoint, Vector3 baseDirection)
    {
        if (_navMeshAgent == null)
        {
            return true;
        }

        if (_navMeshAgent.enabled == false)
        {
            return true;
        }

        if (_navMeshAgent.pathPending)
        {
            return true;
        }

        if (_navMeshAgent.hasPath == false)
        {
            return true;
        }

        if (_navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            return true;
        }

        if (IsBlocked(currentPoint, baseDirection, GetResolveProbeDistance()))
        {
            return true;
        }

        return false;
    }

    private Vector3 GetAvoidDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (_obstacleMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_probeDistance <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 baseDirection = GetFlatDirection(moveDirection);

        if (baseDirection.sqrMagnitude <= MinDistance)
        {
            return Vector3.zero;
        }

        Vector3 avoidDirection = GetProbePush(currentPoint, baseDirection, _probeDistance);
        Vector3 leftDirection = RotateDirection(baseDirection, -_probeAngle);
        Vector3 rightDirection = RotateDirection(baseDirection, _probeAngle);

        avoidDirection += GetProbePush(currentPoint, leftDirection, _probeDistance * 0.85f) * 0.75f;
        avoidDirection += GetProbePush(currentPoint, rightDirection, _probeDistance * 0.85f) * 0.75f;

        return GetFlatVector(avoidDirection);
    }

    private Vector3 ResolveBlockedDirection(Vector3 currentPoint, Vector3 baseDirection, Vector3 desiredDirection)
    {
        Vector3 flatDesiredDirection = GetFlatDirection(desiredDirection);

        if (flatDesiredDirection.sqrMagnitude <= MinDistance)
        {
            return baseDirection;
        }

        float probeDistance = GetResolveProbeDistance();

        if (IsBlocked(currentPoint, flatDesiredDirection, probeDistance) == false)
        {
            return flatDesiredDirection;
        }

        Vector3 flatBaseDirection = GetFlatDirection(baseDirection);

        if (flatBaseDirection.sqrMagnitude > MinDistance)
        {
            if (IsBlocked(currentPoint, flatBaseDirection, probeDistance) == false)
            {
                return flatBaseDirection;
            }
        }

        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue;
        int directionIndex = 1;

        while (directionIndex <= AvoidDirectionCount)
        {
            float positiveAngle = _probeAngle * directionIndex;
            EvaluateDirectionCandidate(
                currentPoint,
                flatDesiredDirection,
                positiveAngle,
                probeDistance,
                ref bestDirection,
                ref bestScore);

            float negativeAngle = -_probeAngle * directionIndex;
            EvaluateDirectionCandidate(
                currentPoint,
                flatDesiredDirection,
                negativeAngle,
                probeDistance,
                ref bestDirection,
                ref bestScore);

            directionIndex += 1;
        }

        if (bestDirection.sqrMagnitude > MinDistance)
        {
            return bestDirection;
        }

        Vector3 slideDirection = GetSlideDirection(currentPoint, flatDesiredDirection, probeDistance);

        if (slideDirection.sqrMagnitude > MinDistance)
        {
            return slideDirection;
        }

        return Vector3.zero;
    }

    private void EvaluateDirectionCandidate(
        Vector3 currentPoint,
        Vector3 desiredDirection,
        float angle,
        float probeDistance,
        ref Vector3 bestDirection,
        ref float bestScore)
    {
        Vector3 candidateDirection = RotateDirection(desiredDirection, angle);

        if (candidateDirection.sqrMagnitude <= MinDistance)
        {
            return;
        }

        float clearDistance = GetClearDistance(currentPoint, candidateDirection, probeDistance);
        float minClearDistance = GetNearProbeDistance();

        if (clearDistance < minClearDistance)
        {
            return;
        }

        float safeProbeDistance = Mathf.Max(probeDistance - minClearDistance, ProbeSkin);
        float distanceScore = (clearDistance - minClearDistance) / safeProbeDistance;
        distanceScore = Mathf.Clamp01(distanceScore);
        float directionScore = Vector3.Dot(desiredDirection, candidateDirection);
        float candidateScore = (distanceScore * 1.5f) + directionScore;

        if (candidateScore <= bestScore)
        {
            return;
        }

        bestScore = candidateScore;
        bestDirection = candidateDirection;
    }

    private Vector3 GetProbePush(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
    {
        if (probeDistance <= MinDistance)
        {
            return Vector3.zero;
        }

        float nearestDistance;
        Vector3 nearestNormal;

        if (TryGetNearestProbeHit(currentPoint, probeDirection, probeDistance, out nearestDistance, out nearestNormal) == false)
        {
            return Vector3.zero;
        }

        Vector3 pushDirection = GetFlatDirection(nearestNormal);

        if (pushDirection.sqrMagnitude <= MinDistance)
        {
            pushDirection = -probeDirection;
        }

        float hitFactor = 1f - Mathf.Clamp01(nearestDistance / probeDistance);

        return pushDirection * hitFactor;
    }

    private float GetClearDistance(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
    {
        if (probeDistance <= MinDistance)
        {
            return 0f;
        }

        float nearestDistance = probeDistance;
        Vector3 nearestNormal;

        if (TryGetNearestProbeHit(currentPoint, probeDirection, probeDistance, out nearestDistance, out nearestNormal) == false)
        {
            return probeDistance;
        }

        return nearestDistance;
    }

    private Vector3 GetSlideDirection(Vector3 currentPoint, Vector3 moveDirection, float probeDistance)
    {
        if (_obstacleMask.value == 0)
        {
            return Vector3.zero;
        }

        if (probeDistance <= MinDistance)
        {
            return Vector3.zero;
        }

        float nearestDistance;
        Vector3 nearestNormal;

        if (TryGetNearestProbeHit(currentPoint, moveDirection, probeDistance, out nearestDistance, out nearestNormal) == false)
        {
            return Vector3.zero;
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, nearestNormal);

        return GetFlatDirection(slideDirection);
    }

    private bool TryGetNearestProbeHit(Vector3 currentPoint, Vector3 probeDirection, float probeDistance, out float nearestDistance, out Vector3 nearestNormal)
    {
        nearestDistance = float.MaxValue;
        nearestNormal = Vector3.zero;
        float highProbeHeight = _probeHeight;
        float lowProbeHeight = GetLowProbeHeight();

        CollectNearestProbeHit(currentPoint, probeDirection, probeDistance, highProbeHeight, ref nearestDistance, ref nearestNormal);

        if (Mathf.Abs(lowProbeHeight - highProbeHeight) > MinDistance)
        {
            CollectNearestProbeHit(currentPoint, probeDirection, probeDistance, lowProbeHeight, ref nearestDistance, ref nearestNormal);
        }

        return nearestDistance < float.MaxValue;
    }

    private void CollectNearestProbeHit(
        Vector3 currentPoint,
        Vector3 probeDirection,
        float probeDistance,
        float probeHeight,
        ref float nearestDistance,
        ref Vector3 nearestNormal)
    {
        CollectNearProbeOverlap(currentPoint, probeDirection, probeHeight, ref nearestDistance, ref nearestNormal);

        Vector3 origin = GetProbeOrigin(currentPoint, probeHeight);
        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            _probeRadius,
            probeDirection,
            _probeBuffer,
            probeDistance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            RaycastHit hit = _probeBuffer[hitIndex];
            Collider hitCollider = hit.collider;

            if (CanUseProbeObstacle(hitCollider))
            {
                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestNormal = hit.normal;
                }
            }

            hitIndex += 1;
        }
    }

    private void CollectNearProbeOverlap(
        Vector3 currentPoint,
        Vector3 probeDirection,
        float probeHeight,
        ref float nearestDistance,
        ref Vector3 nearestNormal)
    {
        Vector3 overlapPoint = GetProbeOrigin(currentPoint, probeHeight) + (probeDirection * GetNearProbeDistance());
        int hitCount = Physics.OverlapSphereNonAlloc(
            overlapPoint,
            _probeRadius,
            _pointBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _pointBuffer[hitIndex];

            if (CanUseProbeObstacle(hitCollider))
            {
                Vector3 obstaclePoint = GetClosestPoint(hitCollider, overlapPoint);
                Vector3 overlapNormal = overlapPoint - obstaclePoint;
                overlapNormal = GetFlatDirection(overlapNormal);

                if (overlapNormal.sqrMagnitude <= MinDistance)
                {
                    overlapNormal = -probeDirection;
                }

                nearestDistance = 0f;
                nearestNormal = overlapNormal;

                return;
            }

            hitIndex += 1;
        }
    }

    private float GetResolveProbeDistance()
    {
        return Mathf.Max(_probeRadius + ProbeSkin, _probeDistance * 0.85f);
    }

    private float GetNearProbeDistance()
    {
        float minGap = _probeRadius + (ProbeSkin * NearProbeSkinScale);

        return Mathf.Max(minGap, _probeRadius * NearProbeDistanceScale);
    }

    private float GetLowProbeHeight()
    {
        return Mathf.Max(MinProbeHeight, _probeHeight * LowProbeHeightScale);
    }

    private bool IsBlocked(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
    {
        return GetClearDistance(currentPoint, probeDirection, probeDistance) < probeDistance - ProbeSkin;
    }

    private bool HasObstaclePoint(Vector3 point)
    {
        if (_obstacleMask.value == 0)
        {
            return false;
        }

        Vector3 origin = GetFlatPoint(point) + (Vector3.up * _probeHeight);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            _probeRadius,
            _pointBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _pointBuffer[hitIndex];

            if (CanUseStaticObstacle(hitCollider))
            {
                return true;
            }

            hitIndex += 1;
        }

        return false;
    }

    private Vector3 GetOverlapPush()
    {
        if (_obstacleMask.value == 0)
        {
            return Vector3.zero;
        }

        if (_bodyBuffer == null)
        {
            return Vector3.zero;
        }

        Vector3 currentPoint = GetFlatPoint(_root.position);
        Vector3 origin = currentPoint + (Vector3.up * _probeHeight);
        float probeRadius = Mathf.Max(_probeRadius, 0.2f);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            probeRadius,
            _pointBuffer,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hitCount == 0)
        {
            return Vector3.zero;
        }

        Vector3 pushDirection = Vector3.zero;
        int bodyIndex = 0;

        while (bodyIndex < _bodyBuffer.Length)
        {
            Collider bodyCollider = _bodyBuffer[bodyIndex];

            if (CanUseBodyCollider(bodyCollider))
            {
                int hitIndex = 0;

                while (hitIndex < hitCount)
                {
                    Collider hitCollider = _pointBuffer[hitIndex];

                    if (CanUseStaticObstacle(hitCollider))
                    {
                        if (hitCollider.isTrigger == false)
                        {
                            Vector3 overlapDirection;
                            float overlapDistance;
                            bool hasOverlap = Physics.ComputePenetration(
                                bodyCollider,
                                bodyCollider.transform.position,
                                bodyCollider.transform.rotation,
                                hitCollider,
                                hitCollider.transform.position,
                                hitCollider.transform.rotation,
                                out overlapDirection,
                                out overlapDistance);

                            if (hasOverlap)
                            {
                                if (overlapDistance > 0f)
                                {
                                    overlapDirection.y = 0f;

                                    if (overlapDirection.sqrMagnitude > MinDistance)
                                    {
                                        overlapDirection.Normalize();
                                        pushDirection += overlapDirection * overlapDistance;
                                    }
                                }
                            }
                        }
                    }

                    hitIndex += 1;
                }
            }

            bodyIndex += 1;
        }

        pushDirection.y = 0f;

        return pushDirection;
    }

    private bool CanUseBodyCollider(Collider bodyCollider)
    {
        if (bodyCollider == null)
        {
            return false;
        }

        if (bodyCollider.enabled == false)
        {
            return false;
        }

        if (bodyCollider.isTrigger)
        {
            return false;
        }

        Rigidbody bodyRigidbody = bodyCollider.attachedRigidbody;

        if (bodyRigidbody != null && bodyRigidbody != _rigidbody)
        {
            return false;
        }

        return true;
    }

    private bool IsEnemyCollider(Collider hitCollider)
    {
        Enemy hitEnemy = hitCollider.GetComponentInParent<Enemy>();

        if (hitEnemy == null)
        {
            return false;
        }

        return hitEnemy.gameObject != _root.gameObject;
    }

    private void ApplyResolve(Vector3 resolveVector)
    {
        Vector3 nextPosition = _root.position + resolveVector;

        if (_rigidbody != null)
        {
            _rigidbody.position = nextPosition;

            if (_rigidbody.isKinematic == false)
            {
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            }

            return;
        }

        _root.position = nextPosition;
    }
}

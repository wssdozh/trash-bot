using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private bool ResolveOverlap(Vector3 currentPoint)
        {
            Vector3 overlapPush = GetOverlapPush(currentPoint);

            if (overlapPush.sqrMagnitude <= MinSqrMagnitude)
            {
                return false;
            }

            float pushDistance = overlapPush.magnitude;
            float resolveDistance = Mathf.Clamp(pushDistance, ResolveMinStep, ResolveMaxStep);
            Vector3 resolveVector = (overlapPush / pushDistance) * resolveDistance;
            Vector3 nextPosition = _baseRigidbody.position + resolveVector;
            Vector3 currentVelocity = _baseRigidbody.linearVelocity;

            _baseRigidbody.position = nextPosition;
            _baseRigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            InvalidatePath();

            return true;
        }

        private Vector3 GetSteerDirection(Vector3 currentPoint, Vector3 moveDirection)
        {
            Vector3 baseDirection = GetPlanarDirection(moveDirection);

            if (baseDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            float probeDistance = GetResolveProbeDistance();

            if (IsBlocked(currentPoint, baseDirection, probeDistance) == false)
            {
                return baseDirection;
            }

            Vector3 bestDirection = Vector3.zero;
            float bestScore = float.MinValue;
            int directionIndex = 1;

            while (directionIndex <= AvoidDirectionCount)
            {
                float positiveAngle = _config.FlankAngle * directionIndex;
                EvaluateDirectionCandidate(currentPoint, baseDirection, positiveAngle, probeDistance, ref bestDirection, ref bestScore);

                float negativeAngle = -_config.FlankAngle * directionIndex;
                EvaluateDirectionCandidate(currentPoint, baseDirection, negativeAngle, probeDistance, ref bestDirection, ref bestScore);
                directionIndex += 1;
            }

            if (bestDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return bestDirection;
            }

            Vector3 slideDirection = GetSlideDirection(currentPoint, baseDirection, probeDistance);

            if (slideDirection.sqrMagnitude > MinSqrMagnitude)
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

            if (candidateDirection.sqrMagnitude <= MinSqrMagnitude)
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

        private Vector3 GetSlideDirection(Vector3 currentPoint, Vector3 moveDirection, float probeDistance)
        {
            float nearestDistance;
            Vector3 nearestNormal;

            if (TryGetNearestProbeHit(currentPoint, moveDirection, probeDistance, out nearestDistance, out nearestNormal) == false)
            {
                return Vector3.zero;
            }

            Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, nearestNormal);

            return GetPlanarDirection(slideDirection);
        }

        private bool TryGetNearestProbeHit(
            Vector3 currentPoint,
            Vector3 probeDirection,
            float probeDistance,
            out float nearestDistance,
            out Vector3 nearestNormal)
        {
            nearestDistance = float.MaxValue;
            nearestNormal = Vector3.zero;
            float highProbeHeight = _config.ProbeHeight;
            float lowProbeHeight = GetLowProbeHeight();

            CollectProbeHit(currentPoint, probeDirection, probeDistance, highProbeHeight, ref nearestDistance, ref nearestNormal);

            if (Mathf.Abs(lowProbeHeight - highProbeHeight) > MinSqrMagnitude)
            {
                CollectProbeHit(currentPoint, probeDirection, probeDistance, lowProbeHeight, ref nearestDistance, ref nearestNormal);
            }

            return nearestDistance < float.MaxValue;
        }

        private void CollectProbeHit(
            Vector3 currentPoint,
            Vector3 probeDirection,
            float probeDistance,
            float probeHeight,
            ref float nearestDistance,
            ref Vector3 nearestNormal)
        {
            CollectNearProbeOverlap(currentPoint, probeDirection, probeHeight, ref nearestDistance, ref nearestNormal);

            Vector3 origin = currentPoint + (Vector3.up * probeHeight);
            RaycastHit hit;

            if (Physics.SphereCast(origin, _config.ProbeRadius, probeDirection, out hit, probeDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return;
            }

            if (hit.collider == null)
            {
                return;
            }

            if (hit.collider.transform.IsChildOf(_base))
            {
                return;
            }

            if (hit.distance >= nearestDistance)
            {
                return;
            }

            nearestDistance = hit.distance;
            nearestNormal = hit.normal;
        }

        private void CollectNearProbeOverlap(
            Vector3 currentPoint,
            Vector3 probeDirection,
            float probeHeight,
            ref float nearestDistance,
            ref Vector3 nearestNormal)
        {
            Vector3 overlapPoint = currentPoint + (Vector3.up * probeHeight) + (probeDirection * GetNearProbeDistance());
            int hitCount = Physics.OverlapSphereNonAlloc(overlapPoint, _config.ProbeRadius, _overlapBuffer, _obstacleMask, QueryTriggerInteraction.Ignore);
            int hitIndex = 0;

            while (hitIndex < hitCount)
            {
                Collider hitCollider = _overlapBuffer[hitIndex];
                _overlapBuffer[hitIndex] = null;
                hitIndex += 1;

                if (hitCollider == null)
                {
                    continue;
                }

                if (hitCollider.transform.IsChildOf(_base))
                {
                    continue;
                }

                Vector3 obstaclePoint = GetClosestPoint(hitCollider, overlapPoint);
                Vector3 overlapNormal = overlapPoint - obstaclePoint;
                overlapNormal = GetPlanarDirection(overlapNormal);

                if (overlapNormal.sqrMagnitude <= MinSqrMagnitude)
                {
                    overlapNormal = -probeDirection;
                }

                nearestDistance = 0f;
                nearestNormal = overlapNormal;

                return;
            }
        }

        private Vector3 GetClosestPoint(Collider hitCollider, Vector3 point)
        {
            if (hitCollider is BoxCollider
                || hitCollider is SphereCollider
                || hitCollider is CapsuleCollider)
            {
                return hitCollider.ClosestPoint(point);
            }

            MeshCollider meshCollider = hitCollider as MeshCollider;

            if (meshCollider != null && meshCollider.convex)
            {
                return hitCollider.ClosestPoint(point);
            }

            return hitCollider.bounds.ClosestPoint(point);
        }

        private float GetClearDistance(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
        {
            float nearestDistance;
            Vector3 nearestNormal;

            if (TryGetNearestProbeHit(currentPoint, probeDirection, probeDistance, out nearestDistance, out nearestNormal) == false)
            {
                return probeDistance;
            }

            return nearestDistance;
        }

        private bool IsBlocked(Vector3 currentPoint, Vector3 probeDirection, float probeDistance)
        {
            return GetClearDistance(currentPoint, probeDirection, probeDistance) < probeDistance - ProbeSkin;
        }

        private float GetResolveProbeDistance()
        {
            return Mathf.Max(_config.ProbeRadius + ProbeSkin, _config.ForwardProbeDistance * 0.85f);
        }

        private float GetNearProbeDistance()
        {
            return Mathf.Max(_config.ProbeRadius + ProbeSkin, _config.ProbeRadius * 1.5f);
        }

        private float GetLowProbeHeight()
        {
            return Mathf.Max(MinProbeHeight, _config.ProbeHeight * LowProbeHeightScale);
        }

        private Vector3 RotateDirection(Vector3 direction, float angle)
        {
            if (direction.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

            return GetPlanarDirection(rotation * direction);
        }

        private Vector3 GetOverlapPush(Vector3 currentPoint)
        {
            if (_obstacleMask.value == 0)
            {
                return Vector3.zero;
            }

            if (_bodyColliders == null)
            {
                return Vector3.zero;
            }

            Vector3 origin = currentPoint + (Vector3.up * _config.ProbeHeight);
            int hitCount = Physics.OverlapSphereNonAlloc(origin, Mathf.Max(_config.ProbeRadius, 0.2f), _overlapBuffer, _obstacleMask, QueryTriggerInteraction.Ignore);

            if (hitCount == 0)
            {
                return Vector3.zero;
            }

            Vector3 pushDirection = Vector3.zero;
            int bodyIndex = 0;

            while (bodyIndex < _bodyColliders.Length)
            {
                Collider bodyCollider = _bodyColliders[bodyIndex];
                bodyIndex += 1;

                if (CanUseBodyCollider(bodyCollider) == false)
                {
                    continue;
                }

                int hitIndex = 0;

                while (hitIndex < hitCount)
                {
                    Collider hitCollider = _overlapBuffer[hitIndex];
                    hitIndex += 1;

                    if (hitCollider == null)
                    {
                        continue;
                    }

                    if (hitCollider.transform.IsChildOf(_base))
                    {
                        continue;
                    }

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

                    if (hasOverlap == false)
                    {
                        continue;
                    }

                    if (overlapDistance <= 0f)
                    {
                        continue;
                    }

                    overlapDirection.y = 0f;

                    if (overlapDirection.sqrMagnitude <= MinSqrMagnitude)
                    {
                        continue;
                    }

                    pushDirection += overlapDirection.normalized * overlapDistance;
                }
            }

            int clearIndex = 0;

            while (clearIndex < hitCount)
            {
                _overlapBuffer[clearIndex] = null;
                clearIndex += 1;
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

            if (bodyRigidbody != null && bodyRigidbody != _baseRigidbody)
            {
                return false;
            }

            return true;
        }

        private int TryAddWallNormal(Vector3 position, Vector3 direction, ref Vector3 normalSum)
        {
            Vector3 rayOrigin = position + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, direction, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return 0;
            }

            normalSum += hit.normal;

            return 1;
        }

        private bool IsNearWall(Vector3 point)
        {
            return GetWallHitCount(point) > 0;
        }

        private bool IsNearCorner(Vector3 point)
        {
            return GetWallHitCount(point) > 1;
        }

        private int GetWallHitCount(Vector3 point)
        {
            int hitCount = 0;
            Vector3 rayOrigin = point + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.forward, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.back, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.left, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.right, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            return hitCount;
        }
    }
}

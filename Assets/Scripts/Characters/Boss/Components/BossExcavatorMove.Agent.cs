using UnityEngine;
using UnityEngine.AI;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private bool SyncAgent(Vector3 currentPoint)
        {
            InitializeNavPaths();

            if (_navMeshAgent == null)
            {
                return false;
            }

            if (HasAnyNavMesh() == false)
            {
                return false;
            }

            if (_navMeshAgent.enabled == false)
            {
                return TryActivateAgent(currentPoint);
            }

            if (_navMeshAgent.isOnNavMesh == false)
            {
                Vector3 navPoint;

                if (TryGetRecoverPoint(currentPoint, out navPoint) == false)
                {
                    _navMeshAgent.enabled = false;

                    return false;
                }

                if (Vector3.Distance(currentPoint, navPoint) > NavSnapGap)
                {
                    SnapToPoint(navPoint);
                    currentPoint = navPoint;
                }

                if (_navMeshAgent.Warp(navPoint) == false)
                {
                    _navMeshAgent.enabled = false;

                    return false;
                }

                CacheNavPoint(navPoint);

                return true;
            }

            _navMeshAgent.nextPosition = currentPoint;
            CacheNavPoint(currentPoint);
            PullAgent(currentPoint);

            return true;
        }

        private bool TryActivateAgent(Vector3 currentPoint)
        {
            if (HasAnyNavMesh() == false)
            {
                return false;
            }

            Vector3 navPoint;

            if (TryGetNavPoint(currentPoint, out navPoint) == false)
            {
                return false;
            }

            SnapToPoint(navPoint);
            currentPoint = GetBasePoint();

            _navMeshAgent.enabled = true;

            if (_navMeshAgent.isOnNavMesh == false)
            {
                if (_navMeshAgent.Warp(navPoint) == false)
                {
                    _navMeshAgent.enabled = false;

                    return false;
                }
            }

            _navMeshAgent.nextPosition = currentPoint;
            CacheNavPoint(navPoint);

            return true;
        }

        private bool TryGetRecoverPoint(Vector3 currentPoint, out Vector3 navPoint)
        {
            if (TryGetNavPoint(currentPoint, out navPoint))
            {
                return true;
            }

            if (TryGetNavPoint(currentPoint, NavRecoverGap, out navPoint))
            {
                return true;
            }

            if (_hasLastNavPoint)
            {
                if (TryGetNavPoint(_lastNavPoint, NavRecoverGap, out navPoint))
                {
                    return true;
                }
            }

            navPoint = currentPoint;

            return false;
        }

        private void SnapToPoint(Vector3 navPoint)
        {
            Vector3 nextPosition = _baseRigidbody.position;
            nextPosition.x = navPoint.x;
            nextPosition.z = navPoint.z;

            _baseRigidbody.position = nextPosition;
            Vector3 currentVelocity = _baseRigidbody.linearVelocity;
            _baseRigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
        }

        private void CacheNavPoint(Vector3 navPoint)
        {
            _lastNavPoint = navPoint;
            _hasLastNavPoint = true;
        }

        public void InvalidatePath()
        {
            _hasPathTarget = false;
            _pathTargetPoint = GetBasePoint();
            _pathStopDistance = _config != null ? _config.StopDistance : 0f;

            if (_navMeshAgent == null)
            {
                return;
            }

            if (_navMeshAgent.enabled == false)
            {
                return;
            }

            if (_navMeshAgent.isOnNavMesh == false)
            {
                return;
            }

            _navMeshAgent.ResetPath();
            _navMeshAgent.nextPosition = GetBasePoint();
        }

        private void PullAgent(Vector3 currentPoint)
        {
            Vector3 worldDeltaPosition = _navMeshAgent.nextPosition - currentPoint;
            worldDeltaPosition.y = 0f;
            float agentRadius = Mathf.Max(_navMeshAgent.radius, _config.ProbeRadius);

            if (worldDeltaPosition.sqrMagnitude <= agentRadius * agentRadius)
            {
                return;
            }

            _navMeshAgent.nextPosition = currentPoint + (worldDeltaPosition * 0.9f);
        }

        private bool HasCompletePath(Vector3 currentPoint, Vector3 targetPoint)
        {
            Vector3 currentNavPoint;
            Vector3 targetNavPoint;

            if (TryGetNavPoint(currentPoint, NavRecoverGap, out currentNavPoint) == false)
            {
                return false;
            }

            if (TryGetNavPoint(targetPoint, NavRecoverGap, out targetNavPoint) == false)
            {
                return false;
            }

            bool hasPath = NavMesh.CalculatePath(currentNavPoint, targetNavPoint, NavMesh.AllAreas, _scorePath);

            if (hasPath == false)
            {
                return false;
            }

            return _scorePath.status == NavMeshPathStatus.PathComplete;
        }

        private float GetPathLength(Vector3 currentPoint, Vector3 targetPoint, NavMeshPath navPath)
        {
            Vector3 currentNavPoint;
            Vector3 targetNavPoint;

            if (TryGetNavPoint(currentPoint, NavRecoverGap, out currentNavPoint) == false)
            {
                return float.MaxValue;
            }

            if (TryGetNavPoint(targetPoint, NavRecoverGap, out targetNavPoint) == false)
            {
                return float.MaxValue;
            }

            bool hasPath = NavMesh.CalculatePath(currentNavPoint, targetNavPoint, NavMesh.AllAreas, navPath);

            if (hasPath == false)
            {
                return float.MaxValue;
            }

            if (navPath.status != NavMeshPathStatus.PathComplete)
            {
                return float.MaxValue;
            }

            Vector3[] corners = navPath.corners;

            if (corners == null)
            {
                return float.MaxValue;
            }

            if (corners.Length == 0)
            {
                return float.MaxValue;
            }

            float pathLength = 0f;
            Vector3 segmentStart = currentNavPoint;
            int cornerIndex = 0;

            while (cornerIndex < corners.Length)
            {
                Vector3 segmentEnd = GetPlanarPosition(corners[cornerIndex]);
                pathLength += Vector3.Distance(segmentStart, segmentEnd);
                segmentStart = segmentEnd;
                cornerIndex += 1;
            }

            return pathLength;
        }

        private bool TryGetNavPoint(Vector3 point, out Vector3 navPoint)
        {
            return TryGetNavPoint(point, GetNavSampleGap(), out navPoint);
        }

        private bool TryGetNavPoint(Vector3 point, float sampleGap, out Vector3 navPoint)
        {
            NavMeshHit navMeshHit;

            if (NavMesh.SamplePosition(point, out navMeshHit, sampleGap, NavMesh.AllAreas) == false)
            {
                navPoint = point;

                return false;
            }

            navPoint = GetPlanarPosition(navMeshHit.position);

            return true;
        }

        private float GetNavSampleGap()
        {
            float minSampleGap = Mathf.Max(NavSampleGap, _config.ProbeRadius * 4f);

            return Mathf.Max(minSampleGap, _config.ProbeHeight * 4f);
        }

        private bool TryGetReachPoint(Vector3 currentPoint, Vector3 targetPoint, out Vector3 reachPoint)
        {
            Vector3 currentNavPoint;
            Vector3 targetNavPoint;

            if (TryGetNavPoint(currentPoint, NavRecoverGap, out currentNavPoint) == false)
            {
                reachPoint = currentPoint;

                return false;
            }

            if (TryGetNavPoint(targetPoint, NavRecoverGap, out targetNavPoint) == false)
            {
                reachPoint = currentPoint;

                return false;
            }

            NavMeshHit navMeshHit;

            if (NavMesh.Raycast(currentNavPoint, targetNavPoint, out navMeshHit, NavMesh.AllAreas) == false)
            {
                reachPoint = targetNavPoint;

                return true;
            }

            Vector3 rawReachPoint = GetPlanarPosition(navMeshHit.position);

            if (Vector3.Distance(currentNavPoint, rawReachPoint) <= _config.StopDistance + _config.DesiredPointDeadZone)
            {
                reachPoint = currentPoint;

                return false;
            }

            if (TryGetNavPoint(rawReachPoint, NavRecoverGap, out reachPoint) == false)
            {
                reachPoint = currentPoint;

                return false;
            }

            return true;
        }
    }
}

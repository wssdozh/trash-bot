using System;
using UnityEngine;
using UnityEngine.AI;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private void ResolveNavMeshAgent(Transform baseTransform)
        {
            if (_navMeshAgent != null)
            {
                if (_navMeshAgent.transform == baseTransform)
                {
                    return;
                }

                _navMeshAgent.enabled = false;
                _navMeshAgent = null;
            }

            _navMeshAgent = baseTransform.GetComponent<NavMeshAgent>();

            if (_navMeshAgent == null)
            {
                throw new InvalidOperationException(nameof(_navMeshAgent));
            }
        }

        private void ConfigureNavMeshAgent()
        {
            if (_navMeshAgent == null)
            {
                throw new InvalidOperationException(nameof(_navMeshAgent));
            }

            _navMeshAgent.updatePosition = false;
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.autoBraking = true;
            _navMeshAgent.autoRepath = true;
            _navMeshAgent.angularSpeed = 0f;
            _navMeshAgent.speed = _config.BaseMoveSpeed;
            _navMeshAgent.acceleration = Mathf.Max(_config.BaseMoveSpeed * 8f, 8f);
            _navMeshAgent.avoidancePriority = GetAvoidPriority();
            _navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            _navMeshAgent.radius = Mathf.Max(_config.ProbeRadius, 0.2f);
            _navMeshAgent.height = Mathf.Max(_config.ProbeHeight * 2f, 0.8f);
            _navMeshAgent.stoppingDistance = _config.StopDistance;
            _navMeshAgent.enabled = false;
        }

        private void CacheBodyColliders(Transform baseTransform)
        {
            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            _bodyColliders = baseTransform.GetComponentsInChildren<Collider>(true);
        }

        private void InitializeNavPaths()
        {
            if (_navPath == null)
            {
                _navPath = new NavMeshPath();
            }

            if (_scorePath == null)
            {
                _scorePath = new NavMeshPath();
            }
        }

        private void ValidateRuntime()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_base == null)
            {
                throw new InvalidOperationException(nameof(_base));
            }

            if (_baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(_baseRigidbody));
            }

            if (_navMeshAgent == null)
            {
                throw new InvalidOperationException(nameof(_navMeshAgent));
            }

            InitializeNavPaths();
        }

        private bool RefreshPath(Vector3 targetPoint, float stopDistance)
        {
            if (NeedPathRefresh(targetPoint, stopDistance))
            {
                _navMeshAgent.stoppingDistance = stopDistance;

                if (TrySetPath(targetPoint) == false)
                {
                    return HasActivePath();
                }

                _pathTargetPoint = targetPoint;
                _pathStopDistance = stopDistance;
                _hasPathTarget = true;
            }

            return HasActivePath();
        }

        private bool NeedPathRefresh(Vector3 targetPoint, float stopDistance)
        {
            if (_hasPathTarget == false)
            {
                return true;
            }

            if (_navMeshAgent.pathPending)
            {
                return false;
            }

            if (Vector3.Distance(_pathTargetPoint, targetPoint) > PathRefreshGap)
            {
                return true;
            }

            if (Mathf.Abs(_pathStopDistance - stopDistance) > 0.05f)
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

            if (_navMeshAgent.isPathStale)
            {
                return true;
            }

            return false;
        }

        private bool TrySetPath(Vector3 targetPoint)
        {
            Vector3 navTargetPoint;

            if (TryGetNavPoint(targetPoint, NavRecoverGap, out navTargetPoint) == false)
            {
                return false;
            }

            if (TrySetPathInternal(navTargetPoint))
            {
                return true;
            }

            Vector3 reachPoint;

            if (TryGetReachPoint(GetBasePoint(), navTargetPoint, out reachPoint) == false)
            {
                return false;
            }

            return TrySetPathInternal(reachPoint);
        }

        private bool TrySetPathInternal(Vector3 targetPoint)
        {
            bool hasPath = _navMeshAgent.CalculatePath(targetPoint, _navPath);

            if (hasPath == false)
            {
                return false;
            }

            if (_navPath.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            Vector3[] corners = _navPath.corners;

            if (corners == null)
            {
                return false;
            }

            if (corners.Length == 0)
            {
                return false;
            }

            return _navMeshAgent.SetPath(_navPath);
        }

        private bool HasActivePath()
        {
            if (_navMeshAgent.pathPending)
            {
                return true;
            }

            if (_navMeshAgent.hasPath == false)
            {
                return false;
            }

            if (_navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                return false;
            }

            return true;
        }
    }
}

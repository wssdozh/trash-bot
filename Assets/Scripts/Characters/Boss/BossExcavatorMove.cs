using System;
using UnityEngine;
using UnityEngine.AI;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove : MonoBehaviour
    {
        private const float MinSqrMagnitude = 0.0001f;
        private const float ProbeSkin = 0.05f;
        private const float MinProbeHeight = 0.2f;
        private const float LowProbeHeightScale = 0.45f;
        private const float ResolveMinStep = 0.2f;
        private const float ResolveMaxStep = 1.2f;
        private const float NavSampleGap = 2f;
        private const float NavRecoverGap = 6f;
        private const float NavSnapGap = 0.35f;
        private const float PathRefreshGap = 0.5f;
        private const float PathLookDistance = 0.9f;
        private const float MoveSmoothTime = 0.14f;
        private const float PathTurnSlowStartAngle = 12f;
        private const float PathTurnSlowStopAngle = 70f;
        private const float MinPathTurnSpeedFactor = 0.78f;
        private const float GizmoPointSize = 0.35f;
        private const float GizmoSmallPointSize = 0.2f;
        private const int AvoidDirectionCount = 3;
        private const int OverlapBufferCount = 24;

        [SerializeField] private Transform _arenaCenter;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private NavMeshAgent _navMeshAgent;

        private NavMeshPath _navPath;
        private NavMeshPath _scorePath;

        private BossExcavatorConfig _config;
        private RoomRuntimeState _roomRuntimeState;
        private Transform _base;
        private Rigidbody _baseRigidbody;
        private Transform _target;
        private BossExcavatorAttack _attackIntent;
        private BossExcavatorTargetPoint _targetPoint;
        private Vector3 _desiredPoint;
        private Vector3 _pathTargetPoint;
        private Vector3 _runtimeArenaCenter;
        private Vector3 _fallbackArenaCenter;
        private bool _isChargeAlign;
        private bool _hasPathTarget;
        private bool _hasRuntimeArenaCenter;
        private bool _hasFallbackArenaCenter;
        private float _pathStopDistance;
        private float _flankSwitchTimer;
        private float _targetSwitchTimer;
        private float _combatTargetTimer;
        private float _currentPlanarSpeed;
        private int _flankSign = 1;
        private Vector3 _lastNavPoint;
        private Vector3 _currentMoveDirection;
        private Vector3 _smoothedMoveDirection;
        private bool _hasLastNavPoint;
        private readonly Collider[] _overlapBuffer = new Collider[OverlapBufferCount];
        private Collider[] _bodyColliders;

        public BossExcavatorTargetPoint TargetPoint => _targetPoint;
        public Vector3 DesiredPoint => _desiredPoint;
        public float MediumDistance => GetMediumDistance();
        public float RetreatDistance => GetRetreatDistance();
        public float MinMoveDistance => GetMinMoveDistance();
        public float AttackChaseDistance => GetAttackChaseDistance();
        public LayerMask ObstacleMask => _obstacleMask;
        public Collider[] BodyColliders => _bodyColliders;
        public float CurrentPlanarSpeed => _currentPlanarSpeed;
        public Vector3 CurrentMoveDirection => _currentMoveDirection;

        private void Awake()
        {
            InitializeNavPaths();
            ResolveRoomState();
        }

        public void Setup(BossExcavatorConfig config, Transform baseTransform, Rigidbody baseRigidbody, Transform target)
        {
            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            if (baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(baseRigidbody));
            }

            _config = config;
            _base = baseTransform;
            _baseRigidbody = baseRigidbody;
            _target = target;

            InitializeNavPaths();
            ResolveRoomState();
            ResolveNavMeshAgent(baseTransform);
            ConfigureNavMeshAgent();
            CacheBodyColliders(baseTransform);
            ResetRuntime();
        }

        public void ResetRuntime()
        {
            _targetPoint = BossExcavatorTargetPoint.ArenaCenter;
            _attackIntent = BossExcavatorAttack.None;
            _desiredPoint = GetBasePoint();
            _pathTargetPoint = _desiredPoint;
            _pathStopDistance = _config != null ? _config.StopDistance : 0f;
            _hasPathTarget = false;
            _isChargeAlign = false;
            _targetSwitchTimer = 0f;
            _flankSwitchTimer = 0f;
            _combatTargetTimer = 0f;
            _currentPlanarSpeed = 0f;
            _flankSign = 1;
            _fallbackArenaCenter = _desiredPoint;
            _hasFallbackArenaCenter = true;
            _lastNavPoint = Vector3.zero;
            _currentMoveDirection = Vector3.zero;
            _smoothedMoveDirection = GetPlanarForward(_base != null ? _base.forward : transform.forward);
            _hasLastNavPoint = false;

            Stop();
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _isChargeAlign = isChargeAlign;
        }

        public void SetAttackIntent(BossExcavatorAttack attackIntent)
        {
            _attackIntent = attackIntent;
        }

        public void SetArenaCenter(Vector3 arenaCenter)
        {
            _runtimeArenaCenter = GetPlanarPosition(arenaCenter);
            _hasRuntimeArenaCenter = true;
        }

        public void Stop()
        {
            if (_navMeshAgent == null)
            {
                return;
            }

            if (_navMeshAgent.enabled)
            {
                if (_navMeshAgent.isOnNavMesh)
                {
                    _navMeshAgent.ResetPath();
                    _navMeshAgent.nextPosition = GetBasePoint();
                }
            }

            _hasPathTarget = false;
            _currentPlanarSpeed = 0f;
            _currentMoveDirection = Vector3.zero;

            if (_baseRigidbody != null)
            {
                Vector3 currentVelocity = _baseRigidbody.linearVelocity;
                currentVelocity.x = 0f;
                currentVelocity.z = 0f;
                _baseRigidbody.linearVelocity = currentVelocity;
            }
        }

        public void FixedTick()
        {
            ValidateRuntime();

            if (_target == null)
            {
                Stop();

                return;
            }

            Vector3 currentPoint = GetBasePoint();

            if (ResolveOverlap(currentPoint))
            {
                currentPoint = GetBasePoint();
            }

            if (SyncAgent(currentPoint) == false)
            {
                Stop();

                return;
            }

            currentPoint = GetBasePoint();

            UpdateTimers();

            Vector3 targetPoint = GetPlanarPosition(_target.position);
            Vector3 arenaCenterPoint = GetArenaCenterPosition();
            BossExcavatorTargetPoint nextTargetPoint = SelectTargetPoint(currentPoint, targetPoint, arenaCenterPoint);

            if (ShouldKeepTargetPoint(nextTargetPoint))
            {
                nextTargetPoint = _targetPoint;
            }

            Vector3 nextDesiredPoint = BuildDesiredPoint(currentPoint, targetPoint, arenaCenterPoint, nextTargetPoint);
            float stopDistance = GetStopDistance(nextTargetPoint);

            if (nextTargetPoint != _targetPoint)
            {
                _targetSwitchTimer = _config.TargetSwitchCooldown;
                _combatTargetTimer = GetTargetPointCommitTime(nextTargetPoint);
            }

            _targetPoint = nextTargetPoint;
            _desiredPoint = nextDesiredPoint;

            if (RefreshPath(nextDesiredPoint, stopDistance) == false)
            {
                Stop();
                RotateBase(currentPoint, targetPoint, Vector3.zero, nextTargetPoint);

                return;
            }

            Vector3 moveDirection = GetMoveDirection(currentPoint);
            RotateBase(currentPoint, targetPoint, moveDirection, nextTargetPoint);
            MoveBase(currentPoint, moveDirection, stopDistance);
        }

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
                return GetSmoothedMoveDirection(moveDirection);
            }

            return GetSmoothedMoveDirection(steerDirection);
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
            Vector3 desiredLookDirection = GetLookDirection(currentPoint, targetPoint, moveDirection, targetPointType);

            if (desiredLookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            Vector3 lookDirection = desiredLookDirection;

            if (lookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            Quaternion currentRotation = _baseRigidbody.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float turnSpeed = GetTurnSpeed(currentPoint, targetPoint, lookDirection);
            Quaternion nextRotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * Time.fixedDeltaTime);

            _baseRigidbody.MoveRotation(nextRotation);
        }

        private Vector3 GetLookDirection(Vector3 currentPoint, Vector3 targetPoint, Vector3 moveDirection, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 targetDirection = GetPlanarDirection(targetPoint - currentPoint);

            if (targetPointType == BossExcavatorTargetPoint.ChargeAlign)
            {
                if (targetDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    return targetDirection;
                }
            }

            if (moveDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return moveDirection;
            }

            if (ShouldUseTargetFacing(targetPointType))
            {
                if (targetDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    return targetDirection;
                }
            }

            return Vector3.zero;
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

        private Vector3 GetSmoothedMoveDirection(Vector3 desiredMoveDirection)
        {
            Vector3 planarDesiredDirection = GetPlanarDirection(desiredMoveDirection);

            if (planarDesiredDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            if (_smoothedMoveDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                _smoothedMoveDirection = planarDesiredDirection;

                return _smoothedMoveDirection;
            }

            float smooth = Mathf.Min(1f, Time.fixedDeltaTime / MoveSmoothTime);
            _smoothedMoveDirection = Vector3.Slerp(_smoothedMoveDirection, planarDesiredDirection, smooth);
            _smoothedMoveDirection = GetPlanarDirection(_smoothedMoveDirection);

            return _smoothedMoveDirection;
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

            Vector3 driveDirection = moveDirection.normalized;
            float speedFactor = GetPathTurnSpeedFactor(currentPoint, driveDirection);
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

                Vector3 obstaclePoint = hitCollider.ClosestPoint(overlapPoint);
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

        private bool ShouldReturnToCenter(Vector3 currentPoint, Vector3 arenaCenterPoint)
        {
            float distanceToCenter = Vector3.Distance(currentPoint, arenaCenterPoint);

            if (distanceToCenter >= GetArenaReturnDistance())
            {
                return true;
            }

            return false;
        }

        private float GetMediumDistance()
        {
            return _config.MediumDistance;
        }

        private float GetRetreatDistance()
        {
            return _config.RetreatDistance;
        }

        private float GetAttackChaseDistance()
        {
            return _config.AttackChaseDistance;
        }

        private float GetArenaReturnDistance()
        {
            return _config.ArenaReturnDistance;
        }

        private float GetChargeAlignDistance()
        {
            return _config.ChargeAlignDistance;
        }

        private float GetWallEscapeDistance()
        {
            return _config.WallEscapeDistance;
        }

        private float GetCornerEscapeDistance()
        {
            return _config.CornerEscapeDistance;
        }

        private float GetDistanceTolerance()
        {
            return _config.DistanceTolerance;
        }

        private float GetDistanceHysteresis()
        {
            return _config.DistanceHysteresis;
        }

        private float GetMinMoveDistance()
        {
            return Mathf.Max(0.1f, GetMediumDistance() - GetDistanceTolerance());
        }

        private float GetOrbitStepDistance()
        {
            return Mathf.Max(_config.ForwardProbeDistance, _config.StopDistance * 2f);
        }

        private Vector3 GetOrbitDirection(Vector3 targetDirection, float sideSign)
        {
            if (sideSign < 0f)
            {
                return GetPlanarDirection(Vector3.Cross(Vector3.up, targetDirection));
            }

            return GetPlanarDirection(Vector3.Cross(targetDirection, Vector3.up));
        }

        private Vector3 ClampRoomPoint(Vector3 point)
        {
            ResolveRoomState();

            if (_roomRuntimeState == null)
            {
                return GetPlanarPosition(point);
            }

            return GetPlanarPosition(_roomRuntimeState.ClampMovePoint(point));
        }

        private int GetAvoidPriority()
        {
            GameObject priorityGameObject = gameObject;

            if (_base != null)
            {
                priorityGameObject = _base.gameObject;
            }

            int priorityId = priorityGameObject.GetInstanceID();

            if (priorityId == int.MinValue)
            {
                priorityId = int.MaxValue;
            }

            if (priorityId < 0)
            {
                priorityId = -priorityId;
            }

            return 20 + (priorityId % 60);
        }

        private bool HasAnyNavMesh()
        {
            NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();

            if (navMeshTriangulation.vertices == null)
            {
                return false;
            }

            return navMeshTriangulation.vertices.Length > 0;
        }

        private Vector3 GetBasePoint()
        {
            return GetPlanarPosition(_baseRigidbody.position);
        }

        private Vector3 GetArenaCenterPosition()
        {
            if (_hasRuntimeArenaCenter)
            {
                return _runtimeArenaCenter;
            }

            if (_arenaCenter != null)
            {
                return GetPlanarPosition(_arenaCenter.position);
            }

            ResolveRoomState();

            if (_roomRuntimeState != null)
            {
                Bounds roomBounds = _roomRuntimeState.GetRoomBounds();

                return GetPlanarPosition(roomBounds.center);
            }

            if (_hasFallbackArenaCenter == false)
            {
                _fallbackArenaCenter = GetBasePoint();
                _hasFallbackArenaCenter = true;
            }

            return _fallbackArenaCenter;
        }

        private void ResolveRoomState()
        {
            if (_roomRuntimeState != null)
            {
                return;
            }

            if (_base != null)
            {
                _roomRuntimeState = _base.GetComponentInParent<RoomRuntimeState>();

                if (_roomRuntimeState != null)
                {
                    return;
                }
            }

            RoomCombatLock roomCombatLock = null;

            if (_base != null)
            {
                roomCombatLock = _base.GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock == null)
            {
                roomCombatLock = GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock != null)
            {
                RoomRuntimeState roomRuntimeState = roomCombatLock.GetComponent<RoomRuntimeState>();

                if (roomRuntimeState != null)
                {
                    _roomRuntimeState = roomRuntimeState;
                }
            }
        }

        private Vector3 GetPlanarPosition(Vector3 position)
        {
            position.y = GetMoveHeight();

            return position;
        }

        private float GetMoveHeight()
        {
            if (_baseRigidbody != null)
            {
                return _baseRigidbody.position.y;
            }

            if (_base != null)
            {
                return _base.position.y;
            }

            return transform.position.y;
        }

        private Vector3 GetPlanarDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }

        private Vector3 GetPlanarForward(Vector3 forward)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }
}

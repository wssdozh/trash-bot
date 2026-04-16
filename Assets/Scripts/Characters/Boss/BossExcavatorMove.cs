using System;
using UnityEngine;
using UnityEngine.AI;

namespace JunkyardBoss
{
    public sealed class BossExcavatorMove : MonoBehaviour
    {
        private const float MinSqrMagnitude = 0.0001f;
        private const float NavSampleGap = 2f;
        private const float NavRecoverGap = 6f;
        private const float NavSnapGap = 0.35f;
        private const float PathRefreshGap = 0.35f;
        private const float PathLookDistance = 0.9f;
        private const float LookBlend = 0.6f;
        private const float MinMoveSpeedFactor = 0.22f;
        private const float GizmoPointSize = 0.35f;
        private const float GizmoSmallPointSize = 0.2f;
        private const float BossRoomRadius = 30f;
        private const float MediumDistanceFactor = 0.38f;
        private const float RetreatDistanceFactor = 0.5f;
        private const float AttackChaseDistanceFactor = 0.42f;
        private const float ArenaReturnDistanceFactor = 0.63f;
        private const float ChargeAlignDistanceFactor = 0.46f;
        private const float WallEscapeDistanceFactor = 0.2f;
        private const float CornerEscapeDistanceFactor = 0.3f;
        private const float DistanceToleranceFactor = 0.05f;
        private const float DistanceHysteresisFactor = 0.025f;
        private const float DesiredPointDeadZoneFactor = 0.03f;

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
        private int _flankSign = 1;
        private Vector3 _lastNavPoint;
        private bool _hasLastNavPoint;

        public BossExcavatorTargetPoint TargetPoint => _targetPoint;
        public Vector3 DesiredPoint => _desiredPoint;
        public float MediumDistance => GetMediumDistance();
        public float RetreatDistance => GetRetreatDistance();
        public float MinMoveDistance => GetMinMoveDistance();
        public float AttackChaseDistance => GetAttackChaseDistance();

        private void Awake()
        {
            InitializeNavPaths();
            ResolveRoomState();
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null)
            {
                return;
            }

            Vector3 currentPoint = GetGizmoBasePoint();
            Vector3 arenaCenterPoint = GetGizmoArenaCenterPoint(currentPoint);

            DrawArenaGizmo(currentPoint, arenaCenterPoint);
            DrawBaseGizmo(currentPoint);

            if (_target == null)
            {
                return;
            }

            Vector3 targetPoint = GetPlanarPosition(_target.position);

            DrawTargetDistanceGizmo(targetPoint);
            DrawCandidatePointGizmo(currentPoint, targetPoint, arenaCenterPoint);
            DrawDesiredPointGizmo(currentPoint, targetPoint);
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
            ResetRuntime();
        }

        public void ResetRuntime()
        {
            _targetPoint = BossExcavatorTargetPoint.ArenaCenter;
            _desiredPoint = GetBasePoint();
            _pathTargetPoint = _desiredPoint;
            _pathStopDistance = _config != null ? _config.StopDistance : 0f;
            _hasPathTarget = false;
            _isChargeAlign = false;
            _targetSwitchTimer = 0f;
            _flankSwitchTimer = 0f;
            _flankSign = 1;
            _fallbackArenaCenter = _desiredPoint;
            _hasFallbackArenaCenter = true;
            _lastNavPoint = Vector3.zero;
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

            if (SyncAgent(currentPoint) == false)
            {
                Stop();

                return;
            }

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

        private void DrawArenaGizmo(Vector3 currentPoint, Vector3 arenaCenterPoint)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawLine(currentPoint, arenaCenterPoint);
            Gizmos.DrawWireSphere(arenaCenterPoint, GizmoPointSize);
            Gizmos.DrawWireSphere(arenaCenterPoint, GetArenaReturnDistance());
        }

        private void DrawBaseGizmo(Vector3 currentPoint)
        {
            Transform forwardTransform = transform;

            if (_base != null)
            {
                forwardTransform = _base;
            }

            Vector3 forwardPoint = currentPoint + (GetPlanarForward(forwardTransform.forward) * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(currentPoint, forwardPoint);
            Gizmos.DrawWireSphere(forwardPoint, GizmoSmallPointSize);
        }

        private void DrawTargetDistanceGizmo(Vector3 targetPoint)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, _config.BucketMaxDistance);

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetAttackChaseDistance());

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetMediumDistance());

            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetRetreatDistance());

            Gizmos.color = new Color(0.3f, 1f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetMinMoveDistance());
        }

        private void DrawCandidatePointGizmo(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint)
        {
            Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
            Vector3 leftPoint = BuildOrbitPoint(currentPoint, targetPoint, -1f);
            Vector3 rightPoint = BuildOrbitPoint(currentPoint, targetPoint, 1f);
            Vector3 backPoint = BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());
            Vector3 chargePoint = BuildCenterPoint(currentPoint, targetPoint, GetChargeAlignDistance());

            DrawPointGizmo(targetPoint, centerPoint, Color.white, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, leftPoint, Color.yellow, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, rightPoint, Color.yellow, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, backPoint, new Color(1f, 0.45f, 0.2f, 0.95f), GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, chargePoint, Color.red, GizmoSmallPointSize);

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            Gizmos.DrawLine(arenaCenterPoint, currentPoint);
        }

        private void DrawDesiredPointGizmo(Vector3 currentPoint, Vector3 targetPoint)
        {
            Vector3 desiredPoint = _desiredPoint;

            if (Application.isPlaying == false)
            {
                desiredPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(currentPoint, desiredPoint);
            Gizmos.DrawWireSphere(desiredPoint, GizmoPointSize);
        }

        private void DrawPointGizmo(Vector3 fromPoint, Vector3 toPoint, Color color, float radius)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(fromPoint, toPoint);
            Gizmos.DrawWireSphere(toPoint, radius);
        }

        private Vector3 GetGizmoBasePoint()
        {
            if (_baseRigidbody != null)
            {
                return GetPlanarPosition(_baseRigidbody.position);
            }

            if (_base != null)
            {
                return GetPlanarPosition(_base.position);
            }

            return GetPlanarPosition(transform.position);
        }

        private Vector3 GetGizmoArenaCenterPoint(Vector3 currentPoint)
        {
            if (Application.isPlaying)
            {
                return GetArenaCenterPosition();
            }

            if (_arenaCenter != null)
            {
                return GetPlanarPosition(_arenaCenter.position);
            }

            return currentPoint;
        }

        private void UpdateTimers()
        {
            _flankSwitchTimer = Mathf.Max(0f, _flankSwitchTimer - Time.fixedDeltaTime);
            _targetSwitchTimer = Mathf.Max(0f, _targetSwitchTimer - Time.fixedDeltaTime);
        }

        private BossExcavatorTargetPoint SelectTargetPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint)
        {
            if (ShouldReturnToCenter(currentPoint, arenaCenterPoint))
            {
                return BossExcavatorTargetPoint.ArenaCenter;
            }

            if (IsNearCorner(currentPoint))
            {
                return BossExcavatorTargetPoint.CornerEscape;
            }

            if (IsNearWall(currentPoint))
            {
                float distanceToTarget = Vector3.Distance(currentPoint, targetPoint);

                if (distanceToTarget < GetMediumDistance())
                {
                    return BossExcavatorTargetPoint.WallEscape;
                }
            }

            if (_isChargeAlign)
            {
                return BossExcavatorTargetPoint.ChargeAlign;
            }

            float targetDistance = Vector3.Distance(currentPoint, targetPoint);

            if (ShouldUseRetreat(targetDistance))
            {
                return BossExcavatorTargetPoint.PlayerBack;
            }

            if (ShouldUseCenter(targetDistance))
            {
                return BossExcavatorTargetPoint.PlayerCenter;
            }

            return SelectFlankPoint(currentPoint, targetPoint);
        }

        private bool ShouldUseRetreat(float targetDistance)
        {
            if (targetDistance < GetMinMoveDistance())
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                if (targetDistance < GetMediumDistance())
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldUseCenter(float targetDistance)
        {
            if (targetDistance > GetAttackChaseDistance())
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                if (targetDistance > GetMediumDistance() - GetDistanceHysteresis())
                {
                    return true;
                }
            }

            return false;
        }

        private BossExcavatorTargetPoint SelectFlankPoint(Vector3 currentPoint, Vector3 targetPoint)
        {
            Vector3 leftPoint = BuildOrbitPoint(currentPoint, targetPoint, -1f);
            Vector3 rightPoint = BuildOrbitPoint(currentPoint, targetPoint, 1f);
            float leftScore = GetPointScore(currentPoint, leftPoint);
            float rightScore = GetPointScore(currentPoint, rightPoint);
            int currentFlankSign = GetFlankSign(_targetPoint);

            if (currentFlankSign != 0)
            {
                float currentScore = currentFlankSign < 0 ? leftScore : rightScore;
                float otherScore = currentFlankSign < 0 ? rightScore : leftScore;
                float requiredGain = _config.FlankSwitchThreshold;

                if (_flankSwitchTimer > 0f)
                {
                    requiredGain += _config.FlankSwitchThreshold;
                }

                if (currentScore <= otherScore + requiredGain)
                {
                    return GetFlankTargetPoint(currentFlankSign);
                }
            }

            if (leftScore <= rightScore)
            {
                SetFlankSign(-1);

                return BossExcavatorTargetPoint.PlayerLeft;
            }

            SetFlankSign(1);

            return BossExcavatorTargetPoint.PlayerRight;
        }

        private void SetFlankSign(int flankSign)
        {
            if (_flankSign == flankSign)
            {
                if (GetFlankSign(_targetPoint) == 0)
                {
                    _flankSwitchTimer = _config.FlankSwitchCooldown;
                }

                return;
            }

            _flankSign = flankSign;
            _flankSwitchTimer = _config.FlankSwitchCooldown;
        }

        private int GetFlankSign(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerLeft)
            {
                return -1;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return 1;
            }

            return 0;
        }

        private BossExcavatorTargetPoint GetFlankTargetPoint(int flankSign)
        {
            if (flankSign < 0)
            {
                return BossExcavatorTargetPoint.PlayerLeft;
            }

            return BossExcavatorTargetPoint.PlayerRight;
        }

        private bool ShouldKeepTargetPoint(BossExcavatorTargetPoint nextTargetPoint)
        {
            if (nextTargetPoint == _targetPoint)
            {
                return false;
            }

            if (_targetSwitchTimer <= 0f)
            {
                return false;
            }

            if (IsImmediatePoint(nextTargetPoint))
            {
                return false;
            }

            if (IsImmediatePoint(_targetPoint))
            {
                return false;
            }

            return true;
        }

        private bool IsImmediatePoint(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.WallEscape)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.CornerEscape)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.ArenaCenter)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.ChargeAlign)
            {
                return true;
            }

            return false;
        }

        private Vector3 BuildDesiredPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 rawPoint = BuildRawPoint(currentPoint, targetPoint, arenaCenterPoint, targetPointType);
            Vector3 resolvedPoint;

            if (TryResolvePoint(currentPoint, rawPoint, out resolvedPoint))
            {
                return resolvedPoint;
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerLeft || targetPointType == BossExcavatorTargetPoint.PlayerRight)
            {
                Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());

                if (TryResolvePoint(currentPoint, centerPoint, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (targetPointType != BossExcavatorTargetPoint.PlayerBack)
            {
                Vector3 backPoint = BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());

                if (TryResolvePoint(currentPoint, backPoint, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (TryResolvePoint(currentPoint, arenaCenterPoint, out resolvedPoint))
            {
                return resolvedPoint;
            }

            return currentPoint;
        }

        private Vector3 BuildRawPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint, BossExcavatorTargetPoint targetPointType)
        {
            if (targetPointType == BossExcavatorTargetPoint.PlayerCenter)
            {
                return BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerLeft)
            {
                return BuildOrbitPoint(currentPoint, targetPoint, -1f);
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerRight)
            {
                return BuildOrbitPoint(currentPoint, targetPoint, 1f);
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerBack)
            {
                return BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());
            }

            if (targetPointType == BossExcavatorTargetPoint.WallEscape)
            {
                return BuildEscapePoint(currentPoint, arenaCenterPoint, GetWallEscapeDistance());
            }

            if (targetPointType == BossExcavatorTargetPoint.CornerEscape)
            {
                return BuildEscapePoint(currentPoint, arenaCenterPoint, GetCornerEscapeDistance());
            }

            if (targetPointType == BossExcavatorTargetPoint.ChargeAlign)
            {
                return BuildCenterPoint(currentPoint, targetPoint, GetChargeAlignDistance());
            }

            return arenaCenterPoint;
        }

        private Vector3 BuildCenterPoint(Vector3 currentPoint, Vector3 targetPoint, float distance)
        {
            Vector3 fromTarget = currentPoint - targetPoint;

            if (fromTarget.sqrMagnitude <= MinSqrMagnitude)
            {
                fromTarget = -GetPlanarForward(_target.forward);
            }

            return targetPoint + fromTarget.normalized * distance;
        }

        private Vector3 BuildOrbitPoint(Vector3 currentPoint, Vector3 targetPoint, float sideSign)
        {
            Vector3 fromTarget = currentPoint - targetPoint;

            if (fromTarget.sqrMagnitude <= MinSqrMagnitude)
            {
                fromTarget = -GetPlanarForward(_target.forward);
            }

            Vector3 orbitDirection = RotateDirection(fromTarget.normalized, _config.FlankAngle * sideSign);

            return targetPoint + orbitDirection * GetMediumDistance();
        }

        private Vector3 BuildEscapePoint(Vector3 currentPoint, Vector3 arenaCenterPoint, float escapeDistance)
        {
            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            hitCount += TryAddWallNormal(currentPoint, Vector3.forward, ref normalSum);
            hitCount += TryAddWallNormal(currentPoint, Vector3.back, ref normalSum);
            hitCount += TryAddWallNormal(currentPoint, Vector3.left, ref normalSum);
            hitCount += TryAddWallNormal(currentPoint, Vector3.right, ref normalSum);

            Vector3 escapeDirection = GetPlanarDirection(normalSum);
            Vector3 centerDirection = GetPlanarDirection(arenaCenterPoint - currentPoint);

            if (centerDirection.sqrMagnitude > MinSqrMagnitude)
            {
                escapeDirection += centerDirection.normalized * _config.EscapeCenterWeight;
            }

            escapeDirection = GetPlanarDirection(escapeDirection);

            if (escapeDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                escapeDirection = centerDirection;
            }

            if (escapeDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return arenaCenterPoint;
            }

            if (hitCount <= 0)
            {
                return arenaCenterPoint;
            }

            return currentPoint + escapeDirection.normalized * escapeDistance;
        }

        private bool TryResolvePoint(Vector3 currentPoint, Vector3 point, out Vector3 resolvedPoint)
        {
            Vector3 safePoint = ClampRoomPoint(point);

            if (TryGetNavPoint(safePoint, out resolvedPoint) == false)
            {
                return false;
            }

            if (HasCompletePath(currentPoint, resolvedPoint) == false)
            {
                return false;
            }

            return true;
        }

        private float GetPointScore(Vector3 currentPoint, Vector3 point)
        {
            Vector3 resolvedPoint;

            if (TryResolvePoint(currentPoint, point, out resolvedPoint) == false)
            {
                return float.MaxValue;
            }

            float pointScore = GetPathLength(currentPoint, resolvedPoint, _scorePath);

            if (IsNearWall(resolvedPoint))
            {
                pointScore += _config.WallPenalty;
            }

            if (IsNearCorner(resolvedPoint))
            {
                pointScore += _config.CornerPenalty;
            }

            return pointScore;
        }

        private float GetStopDistance(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerLeft || targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return _config.StopDistance + GetDesiredPointDeadZone();
            }

            return _config.StopDistance;
        }

        private bool RefreshPath(Vector3 targetPoint, float stopDistance)
        {
            if (NeedPathRefresh(targetPoint, stopDistance))
            {
                _navMeshAgent.stoppingDistance = stopDistance;

                if (TrySetPath(targetPoint) == false)
                {
                    return false;
                }

                _pathTargetPoint = targetPoint;
                _pathStopDistance = stopDistance;
                _hasPathTarget = true;
            }

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

        private bool NeedPathRefresh(Vector3 targetPoint, float stopDistance)
        {
            if (_hasPathTarget == false)
            {
                return true;
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

        private Vector3 GetMoveDirection(Vector3 currentPoint)
        {
            Vector3 pathDirection = GetPathDirection(currentPoint);
            Vector3 desiredDirection = GetPlanarDirection(_navMeshAgent.desiredVelocity);

            if (pathDirection.sqrMagnitude > MinSqrMagnitude)
            {
                if (desiredDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    float directionDot = Vector3.Dot(pathDirection, desiredDirection);

                    if (directionDot > 0f)
                    {
                        return GetPlanarDirection(pathDirection + desiredDirection);
                    }
                }

                return pathDirection;
            }

            if (desiredDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return desiredDirection;
            }

            if (_navMeshAgent.hasPath)
            {
                Vector3 steeringPoint = GetPlanarPosition(_navMeshAgent.steeringTarget);
                Vector3 steeringDirection = GetPlanarDirection(steeringPoint - currentPoint);

                if (steeringDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    return steeringDirection;
                }
            }

            return GetPlanarDirection(_desiredPoint - currentPoint);
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
            Vector3 lookDirection = GetLookDirection(currentPoint, targetPoint, moveDirection, targetPointType);

            if (lookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            Quaternion currentRotation = _baseRigidbody.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(currentRotation, targetRotation, _config.BaseTurnSpeed * Time.fixedDeltaTime);

            _baseRigidbody.MoveRotation(nextRotation);
        }

        private Vector3 GetLookDirection(Vector3 currentPoint, Vector3 targetPoint, Vector3 moveDirection, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 targetDirection = GetPlanarDirection(targetPoint - currentPoint);

            if (targetPointType == BossExcavatorTargetPoint.PlayerLeft
                || targetPointType == BossExcavatorTargetPoint.PlayerRight
                || targetPointType == BossExcavatorTargetPoint.PlayerCenter
                || targetPointType == BossExcavatorTargetPoint.ChargeAlign)
            {
                if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
                {
                    return targetDirection;
                }

                if (targetDirection.sqrMagnitude <= MinSqrMagnitude)
                {
                    return moveDirection;
                }

                Vector3 blendedDirection = moveDirection * (1f - LookBlend);
                blendedDirection += targetDirection * LookBlend;

                return GetPlanarDirection(blendedDirection);
            }

            if (moveDirection.sqrMagnitude > MinSqrMagnitude)
            {
                return moveDirection;
            }

            return targetDirection;
        }

        private void MoveBase(Vector3 currentPoint, Vector3 moveDirection, float stopDistance)
        {
            float targetDistance = Vector3.Distance(currentPoint, _desiredPoint);

            if (targetDistance <= stopDistance)
            {
                Vector3 currentVelocity = _baseRigidbody.linearVelocity;
                currentVelocity.x = 0f;
                currentVelocity.z = 0f;
                _baseRigidbody.linearVelocity = currentVelocity;

                return;
            }

            if (moveDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            Vector3 forward = GetPlanarForward(_base.forward);
            float angle = Vector3.Angle(forward, moveDirection);

            if (angle > _config.MoveStopAngle)
            {
                return;
            }

            float speedFactor = GetMoveSpeedFactor(angle);
            float moveDistance = _config.BaseMoveSpeed * speedFactor * Time.fixedDeltaTime;
            float maxDistance = Mathf.Max(0f, targetDistance - stopDistance);

            if (moveDistance > maxDistance)
            {
                moveDistance = maxDistance;
            }

            if (moveDistance <= 0f)
            {
                return;
            }

            Vector3 nextPlanarPosition = currentPoint + forward * moveDistance;
            Vector3 nextPosition = new Vector3(nextPlanarPosition.x, _baseRigidbody.position.y, nextPlanarPosition.z);

            _baseRigidbody.MovePosition(nextPosition);
            _navMeshAgent.nextPosition = GetPlanarPosition(nextPosition);
        }

        private float GetMoveSpeedFactor(float angle)
        {
            if (angle <= _config.MoveStartAngle)
            {
                return 1f;
            }

            float angleRange = Mathf.Max(0.01f, _config.MoveStopAngle - _config.MoveStartAngle);
            float angleProgress = (angle - _config.MoveStartAngle) / angleRange;
            float angleFactor = 1f - Mathf.Clamp01(angleProgress);

            return Mathf.Lerp(MinMoveSpeedFactor, 1f, angleFactor);
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
                _navMeshAgent.enabled = false;

                return false;
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
            return BossRoomRadius * MediumDistanceFactor;
        }

        private float GetRetreatDistance()
        {
            return BossRoomRadius * RetreatDistanceFactor;
        }

        private float GetAttackChaseDistance()
        {
            return BossRoomRadius * AttackChaseDistanceFactor;
        }

        private float GetArenaReturnDistance()
        {
            return BossRoomRadius * ArenaReturnDistanceFactor;
        }

        private float GetChargeAlignDistance()
        {
            return BossRoomRadius * ChargeAlignDistanceFactor;
        }

        private float GetWallEscapeDistance()
        {
            return BossRoomRadius * WallEscapeDistanceFactor;
        }

        private float GetCornerEscapeDistance()
        {
            return BossRoomRadius * CornerEscapeDistanceFactor;
        }

        private float GetDistanceTolerance()
        {
            return BossRoomRadius * DistanceToleranceFactor;
        }

        private float GetDistanceHysteresis()
        {
            return BossRoomRadius * DistanceHysteresisFactor;
        }

        private float GetDesiredPointDeadZone()
        {
            return BossRoomRadius * DesiredPointDeadZoneFactor;
        }

        private float GetMinMoveDistance()
        {
            return Mathf.Max(0.1f, GetMediumDistance() - GetDistanceTolerance());
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

        private Vector3 RotateDirection(Vector3 direction, float angle)
        {
            Vector3 flatDirection = GetPlanarDirection(direction);

            if (flatDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);

            return GetPlanarDirection(rotation * flatDirection);
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

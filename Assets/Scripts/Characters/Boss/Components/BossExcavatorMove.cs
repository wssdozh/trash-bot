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
        private bool _isChargeAlign;
        private bool _hasPathTarget;
        private bool _hasRuntimeArenaCenter;
        private float _pathStopDistance;
        private float _flankSwitchTimer;
        private float _targetSwitchTimer;
        private float _combatTargetTimer;
        private float _currentPlanarSpeed;
        private int _flankSign = 1;
        private Vector3 _lastNavPoint;
        private Vector3 _currentMoveDirection;
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
            _lastNavPoint = Vector3.zero;
            _currentMoveDirection = Vector3.zero;
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

        public void RotateBaseTowardsDirection(Vector3 direction, float turnSpeed)
        {
            ValidateRuntime();

            Vector3 planarDirection = GetPlanarDirection(direction);

            if (planarDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return;
            }

            Quaternion currentRotation = _base.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(planarDirection, Vector3.up);
            float turnStep = Mathf.Max(turnSpeed, 0f) * Time.fixedDeltaTime;
            Quaternion nextRotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnStep);

            _baseRigidbody.angularVelocity = Vector3.zero;
            _base.rotation = nextRotation;
            _baseRigidbody.rotation = nextRotation;
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

    }
}

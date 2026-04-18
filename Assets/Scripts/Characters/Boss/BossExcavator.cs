using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavator : MonoBehaviour
    {
        [SerializeField] private BossExcavatorConfig _config;
        [SerializeField] private BossExcavatorMove _move;
        [SerializeField] private BossExcavatorAim _aim;
        [SerializeField] private BossExcavatorArm _arm;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _base;
        [SerializeField] private Rigidbody _baseRigidbody;
        [SerializeField] private Transform _cabin;
        [SerializeField] private Transform _boom;
        [SerializeField] private Transform _stick;
        [SerializeField] private Transform _bucket;
        [SerializeField] private Health _health;

        private BossExcavatorBrain _brain;
        private BossExcavatorStateMachine _stateMachine;
        private BossExcavatorPhase _phase;
        private BossExcavatorState _state;

        public event Action<BossExcavatorPhase, BossExcavatorPhase> PhaseChanged;
        public event Action<BossExcavatorState, BossExcavatorState> StateChanged;
        public event Action Died;

        public BossExcavatorConfig Config => _config;
        public BossExcavatorMove Move => _move;
        public BossExcavatorAim Aim => _aim;
        public BossExcavatorArm Arm => _arm;
        public Transform Target => _target;
        public Transform Base => _base;
        public Rigidbody BaseRigidbody => _baseRigidbody;
        public Transform Cabin => _cabin;
        public Transform Boom => _boom;
        public Transform Stick => _stick;
        public Transform Bucket => _bucket;
        public Health Health => _health;
        public float CurrentHealth => _health.Value;
        public BossExcavatorPhase Phase => _phase;
        public BossExcavatorState State => _state;
        public BossExcavatorAttack CurrentAttack => _brain.CurrentAttack;
        public BossExcavatorAttack TargetAttack => _brain.TargetAttack;
        public bool IsDead => CurrentHealth <= _health.MinValue;

        private void Awake()
        {
            ValidateDependencies();

            if (IsTargetSelf(_target))
            {
                _target = null;
            }

            _brain = new BossExcavatorBrain(this);
            _stateMachine = new BossExcavatorStateMachine(this);
            _move.Setup(_config, _base, _baseRigidbody, _target);
            _aim.Setup(this, _config, _cabin, _target);
            _arm.Setup(this, _config, _boom, _stick, _bucket);
            TryResolveTarget();

            ResetBoss();
        }

        private void OnEnable()
        {
            _health.Ended += OnHealthEnded;
        }

        private void OnDisable()
        {
            _health.Ended -= OnHealthEnded;
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null)
            {
                return;
            }

            DrawBucketAttackGizmos();
            DrawSweepAttackGizmos();
            DrawChargeAttackGizmos();
            DrawThrowAttackGizmos();
        }

        private void Update()
        {
            _brain.Tick();
            _stateMachine.Tick();
        }

        private void LateUpdate()
        {
            _aim.Tick();
            _arm.Tick();
        }

        private void FixedUpdate()
        {
            if (CanUseTarget() == false)
            {
                TryResolveTarget();
            }

            _brain.FixedTick();
            UpdateMove();
        }

        public void ResetBoss()
        {
            ResetHealth();
            _phase = BossExcavatorPhase.PhaseOne;

            _stateMachine.Reset();
            ApplyState(_config.StartState);
            _move.ResetRuntime();
            _move.SetChargeAlign(false);
            _move.SetAttackIntent(BossExcavatorAttack.None);
            _aim.SetLocked(false);
            _arm.SetLocked(false);
            _arm.SetDefaultPoseImmediate();
            _brain.Reset();
        }

        public void RequestState(BossExcavatorState state)
        {
            _stateMachine.RequestState(state);
            _stateMachine.Tick();
        }

        internal void RequestAutoState(BossExcavatorState state)
        {
            _stateMachine.RequestAutoState(state);
        }

        public void CompletePhaseChange()
        {
            _stateMachine.CompletePhaseChange();
            _stateMachine.Tick();
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            if (IsTargetSelf(target))
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
            _move.SetTarget(target);
            _aim.SetTarget(target);
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _move.SetChargeAlign(isChargeAlign);
        }

        public void SetMoveAttackIntent(BossExcavatorAttack attackIntent)
        {
            _move.SetAttackIntent(attackIntent);
        }

        public void SetAimLocked(bool isLocked)
        {
            _aim.SetLocked(isLocked);
        }

        public void SetArmLocked(bool isLocked)
        {
            _arm.SetLocked(isLocked);
        }

        public void SetArmDefaultPose()
        {
            _arm.SetDefaultPose();
        }

        public BossExcavatorArmPose GetArmPose()
        {
            return _arm.CurrentPose;
        }

        public void SetArmNeutralPose()
        {
            _arm.SetNeutralPose();
        }

        public void SetArmBucketPreparePose()
        {
            _arm.SetBucketPreparePose();
        }

        public void SetArmBucketStrikePose()
        {
            _arm.SetBucketStrikePose();
        }

        public void SetArmGrabScrapPose()
        {
            _arm.SetGrabScrapPose();
        }

        public void SetArmThrowScrapPose()
        {
            _arm.SetThrowScrapPose();
        }

        public void SetArmPose(BossExcavatorArmPose pose)
        {
            _arm.SetPose(pose);
        }

        public void SetArmPose(BossExcavatorArmPose pose, float poseSpeedMult)
        {
            _arm.SetPose(pose, poseSpeedMult);
        }

        public void SetArmPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            _arm.SetPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }

        public void SetArmPose(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler, float poseSpeedMult)
        {
            _arm.SetPose(boomLocalEuler, stickLocalEuler, bucketLocalEuler, poseSpeedMult);
        }

        public void SetArmPoseImmediate(Vector3 boomLocalEuler, Vector3 stickLocalEuler, Vector3 bucketLocalEuler)
        {
            _arm.SetPoseImmediate(boomLocalEuler, stickLocalEuler, bucketLocalEuler);
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0f)
            {
                return;
            }

            if (_state == BossExcavatorState.Dead)
            {
                return;
            }

            _health.Decrease(damage);
            _stateMachine.Tick();
        }

        public float GetHealthRatio()
        {
            return CurrentHealth / _config.MaxHealth;
        }

        internal bool ShouldStartPhaseChange()
        {
            if (_phase != BossExcavatorPhase.PhaseOne)
            {
                return false;
            }

            return GetHealthRatio() <= _config.PhaseTwoRatio;
        }

        internal void ApplyState(BossExcavatorState state)
        {
            if (_state == state)
            {
                return;
            }

            BossExcavatorState previousState = _state;
            _state = state;

            StateChanged?.Invoke(previousState, _state);

            if (_state == BossExcavatorState.Dead)
            {
                Died?.Invoke();
            }
        }

        internal void ApplyPhase(BossExcavatorPhase phase)
        {
            if (_phase == phase)
            {
                return;
            }

            BossExcavatorPhase previousPhase = _phase;
            _phase = phase;

            PhaseChanged?.Invoke(previousPhase, _phase);
        }

        private void UpdateMove()
        {
            if (_state == BossExcavatorState.Dead)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.PhaseChange)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.Idle)
            {
                _move.Stop();

                return;
            }

            if (_state == BossExcavatorState.Reposition)
            {
                _move.FixedTick();

                return;
            }

            if (_state == BossExcavatorState.Chase)
            {
                _move.FixedTick();

                return;
            }

            if (_state == BossExcavatorState.Attack)
            {
                if (CurrentAttack == BossExcavatorAttack.ThrowScrap)
                {
                    _move.FixedTick();

                    return;
                }

                _move.Stop();
            }
        }

        private void ValidateDependencies()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_move == null)
            {
                throw new InvalidOperationException(nameof(_move));
            }

            if (_aim == null)
            {
                throw new InvalidOperationException(nameof(_aim));
            }

            if (_arm == null)
            {
                throw new InvalidOperationException(nameof(_arm));
            }

            if (_base == null)
            {
                throw new InvalidOperationException(nameof(_base));
            }

            if (_baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(_baseRigidbody));
            }

            if (_cabin == null)
            {
                throw new InvalidOperationException(nameof(_cabin));
            }

            if (_boom == null)
            {
                throw new InvalidOperationException(nameof(_boom));
            }

            if (_stick == null)
            {
                throw new InvalidOperationException(nameof(_stick));
            }

            if (_bucket == null)
            {
                throw new InvalidOperationException(nameof(_bucket));
            }

            if (_health == null)
            {
                throw new InvalidOperationException(nameof(_health));
            }
        }

        private void ResetHealth()
        {
            _health.SetAutoRegen(false);
            _health.SetMaxValue(_config.MaxHealth);
            _health.Fill();
        }

        private void OnHealthEnded()
        {
            if (_stateMachine == null)
            {
                return;
            }

            _stateMachine.Tick();
        }

        private bool CanUseTarget()
        {
            if (_target == null)
            {
                return false;
            }

            if (IsTargetSelf(_target))
            {
                return false;
            }

            return true;
        }

        private bool TryResolveTarget()
        {
            if (CanUseTarget())
            {
                return true;
            }

            Player player = FindFirstObjectByType<Player>();

            if (player == null)
            {
                return false;
            }

            Transform playerBody = GetPlayerBody(player);

            if (playerBody == null)
            {
                return false;
            }

            if (IsTargetSelf(playerBody))
            {
                return false;
            }

            _target = playerBody;
            _move.SetTarget(playerBody);
            _aim.SetTarget(playerBody);

            return true;
        }

        private Transform GetPlayerBody(Player player)
        {
            if (player == null)
            {
                return null;
            }

            Transform playerTransform = player.transform;
            Transform playerBody = playerTransform.Find("Body");

            if (playerBody == null)
            {
                return null;
            }

            return playerBody;
        }

        private bool IsTargetSelf(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (target == transform)
            {
                return true;
            }

            return target.IsChildOf(transform);
        }

        private void DrawBucketAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            if (_base == null)
            {
                return;
            }

            Vector3 strikeForward = ResolveAttackForward();
            Vector3 hitCenter = ResolveImpactCenter(_bucket.position, strikeForward, _config.BucketHitOffset, _config.BucketHitRadius);
            Vector3 shockwaveCenter = ResolveImpactCenter(hitCenter, strikeForward, _config.BucketShockwaveOffset, _config.BucketShockwaveRadius);

            Gizmos.color = new Color(1f, 0.22f, 0.12f, 0.95f);
            Gizmos.DrawLine(_bucket.position, hitCenter);
            Gizmos.DrawWireSphere(hitCenter, _config.BucketHitRadius);
            DrawAttackSector(hitCenter, strikeForward, _config.BucketHitRadius, _config.BucketHitAngle);

            Gizmos.color = new Color(1f, 0.66f, 0.18f, 0.9f);
            Gizmos.DrawLine(hitCenter, shockwaveCenter);
            Gizmos.DrawWireSphere(shockwaveCenter, _config.BucketShockwaveRadius);
        }

        private void DrawSweepAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            Vector3 sweepForward = ResolveAttackForward();
            Vector3 sweepCenter = _bucket.position + sweepForward * _config.SweepHitOffset;

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
            Gizmos.DrawLine(_bucket.position, sweepCenter);
            Gizmos.DrawWireSphere(sweepCenter, _config.SweepHitRadius);
        }

        private void DrawChargeAttackGizmos()
        {
            if (_base == null)
            {
                return;
            }

            Vector3 chargeDirection = GetPlanarDirection(_base.forward);

            if (chargeDirection.sqrMagnitude <= 0.0001f)
            {
                chargeDirection = Vector3.forward;
            }

            Vector3 basePosition = _base.position;
            Vector3 hitCenter = basePosition + chargeDirection * _config.ChargeHitOffset;
            float chargeDistance = GetCurrentChargeSpeed() * GetCurrentChargeAttackTime();
            Vector3 chargeEndPoint = basePosition + chargeDirection * chargeDistance;

            Gizmos.color = new Color(1f, 0.1f, 0.85f, 0.9f);
            Gizmos.DrawLine(basePosition, chargeEndPoint);
            Gizmos.DrawWireSphere(hitCenter, _config.ChargeHitRadius);
            Gizmos.DrawWireSphere(chargeEndPoint, 0.22f);
        }

        private void DrawThrowAttackGizmos()
        {
            if (_bucket == null)
            {
                return;
            }

            if (_base == null)
            {
                return;
            }

            Vector3 launchForward = ResolveAttackForward();
            Vector3 spawnPosition = ResolveThrowSpawnPosition(_bucket.position, launchForward);
            int projectileCount = Mathf.Max(_config.ThrowProjectileCount, 1);
            int projectileIndex = 0;

            Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.92f);
            Gizmos.DrawWireSphere(spawnPosition, 0.18f);

            while (projectileIndex < projectileCount)
            {
                float angleOffset = GetThrowAngleOffset(projectileIndex, projectileCount);
                Vector3 projectileDirection = Quaternion.Euler(0f, angleOffset, 0f) * launchForward.normalized;
                Vector3 previewPoint = spawnPosition + projectileDirection * 3.5f;

                Gizmos.DrawLine(spawnPosition, previewPoint);
                Gizmos.DrawWireSphere(previewPoint, 0.1f);
                projectileIndex += 1;
            }
        }

        private void DrawAttackSector(Vector3 origin, Vector3 forward, float radius, float angle)
        {
            Vector3 planarForward = GetPlanarDirection(forward);

            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float halfAngle = angle * 0.5f;
            Vector3 leftDirection = Quaternion.AngleAxis(halfAngle, Vector3.up) * planarForward;
            Vector3 rightDirection = Quaternion.AngleAxis(halfAngle * -1f, Vector3.up) * planarForward;

            Gizmos.DrawLine(origin, origin + planarForward * radius);
            Gizmos.DrawLine(origin, origin + leftDirection * radius);
            Gizmos.DrawLine(origin, origin + rightDirection * radius);
        }

        private Vector3 ResolveAttackForward()
        {
            if (_bucket != null)
            {
                Vector3 bucketForward = GetPlanarDirection(_bucket.forward);

                if (bucketForward.sqrMagnitude > 0.0001f)
                {
                    return bucketForward;
                }
            }

            if (_cabin != null)
            {
                Vector3 cabinForward = GetPlanarDirection(_cabin.forward);

                if (cabinForward.sqrMagnitude > 0.0001f)
                {
                    return cabinForward;
                }
            }

            if (_base != null)
            {
                Vector3 baseForward = GetPlanarDirection(_base.forward);

                if (baseForward.sqrMagnitude > 0.0001f)
                {
                    return baseForward;
                }
            }

            return Vector3.forward;
        }

        private Vector3 ResolveImpactCenter(Vector3 origin, Vector3 forward, float offset, float radius)
        {
            Vector3 impactCenter = origin + forward * offset;
            float impactHeight = _base.position.y + Mathf.Min(radius * 0.3f, 0.8f);
            impactCenter.y = impactHeight;

            return impactCenter;
        }

        private Vector3 ResolveThrowSpawnPosition(Vector3 bucketPosition, Vector3 launchForward)
        {
            Vector3 spawnPosition = bucketPosition + launchForward * (_config.ThrowSpawnOffset + 0.55f);
            float minSpawnHeight = _base.position.y + 0.75f;

            if (spawnPosition.y < minSpawnHeight)
            {
                spawnPosition.y = minSpawnHeight;
            }

            return spawnPosition;
        }

        private float GetThrowAngleOffset(int projectileIndex, int projectileCount)
        {
            if (projectileCount <= 1)
            {
                return 0f;
            }

            float spreadAngle = _config.ThrowProjectileSpreadAngle;
            float step = spreadAngle / (projectileCount - 1);
            float minAngle = spreadAngle * -0.5f;

            return minAngle + step * projectileIndex;
        }

        private Vector3 GetPlanarDirection(Vector3 direction)
        {
            Vector3 planarDirection = direction;
            planarDirection.y = 0f;

            if (planarDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return planarDirection.normalized;
        }

        private float GetCurrentChargeSpeed()
        {
            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                return _config.ChargeSpeed * _config.PhaseTwoChargeSpeedMult;
            }

            return _config.ChargeSpeed;
        }

        private float GetCurrentChargeAttackTime()
        {
            float attackTime = _config.ChargeAttackTime;

            if (_phase == BossExcavatorPhase.PhaseTwo)
            {
                attackTime /= _config.PhaseTwoAttackSpeedMult;
            }

            return attackTime;
        }
    }
}

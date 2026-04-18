using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private const float MinDirectionSqr = 0.0001f;
        private const float AttackQueueCommitTime = 0.18f;
        private const float PhaseTwoDecisionSpeedMult = 4f;
        private const float PreviousAttackRepeatPenalty = 1.4f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorBucketAttack _bucketAttack;
        private readonly BossExcavatorThrowAttack _throwAttack;
        private readonly BossExcavatorChargeAttack _chargeAttack;
        private readonly BossExcavatorSweepAttack _sweepAttack;
        private readonly BossExcavatorScrapTrailAttack _scrapTrailAttack;
        private readonly RoomCombatLock _roomCombatLock;

        private float _sweepCooldownTimer;
        private float _bucketCooldownTimer;
        private float _throwCooldownTimer;
        private float _chargeCooldownTimer;
        private float _scrapTrailCooldownTimer;
        private float _postAttackTimer;
        private float _forcedChaseTimer;
        private float _moveStateTimer;
        private float _phaseChangeTimer;
        private float _queuedAttackTimer;
        private BossExcavatorAttack _currentAttack;
        private BossExcavatorAttack _pendingAttack;
        private BossExcavatorAttack _queuedAttack;
        private BossExcavatorAttack _lastAttack;
        private BossExcavatorAttack _previousAttack;
        private BossExcavatorState _moveState;
        private bool _isPhaseChangeActive;

        public BossExcavatorAttack CurrentAttack => _currentAttack;
        public BossExcavatorAttack TargetAttack => GetTargetAttack();

        public BossExcavatorBrain(BossExcavator boss)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            _boss = boss;
            _bucketAttack = new BossExcavatorBucketAttack(_boss, _boss.Config);
            _throwAttack = new BossExcavatorThrowAttack(_boss, _boss.Config);
            _chargeAttack = new BossExcavatorChargeAttack(_boss, _boss.Config);
            _sweepAttack = new BossExcavatorSweepAttack(_boss, _boss.Config);
            _scrapTrailAttack = new BossExcavatorScrapTrailAttack(_boss, _boss.Config);
            _roomCombatLock = ResolveRoomCombatLock();
            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
        }

        public void Reset()
        {
            _sweepCooldownTimer = 0f;
            _bucketCooldownTimer = 0f;
            _throwCooldownTimer = 0f;
            _chargeCooldownTimer = 0f;
            _scrapTrailCooldownTimer = 0f;
            _postAttackTimer = 0f;
            _forcedChaseTimer = 0f;
            _moveStateTimer = 0f;
            _phaseChangeTimer = 0f;
            _queuedAttackTimer = 0f;
            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
            _queuedAttack = BossExcavatorAttack.None;
            _lastAttack = BossExcavatorAttack.None;
            _previousAttack = BossExcavatorAttack.None;
            _moveState = BossExcavatorState.Chase;
            _isPhaseChangeActive = false;
            _sweepAttack.Reset();
            _bucketAttack.Reset();
            _throwAttack.Reset();
            _chargeAttack.Reset();
            _scrapTrailAttack.Reset();
            ApplyCombatDefaults();
        }

        public void Tick()
        {
            if (_boss.IsDead)
            {
                ClearAttackRuntime(false);
                CancelScrapTrail(false);
                ClearQueuedAttack();
                _boss.SetMoveAttackIntent(BossExcavatorAttack.None);
                ResetPhaseChangeRuntime();

                return;
            }

            if (_boss.State == BossExcavatorState.PhaseChange)
            {
                ClearAttackRuntime(false);
                CancelScrapTrail(false);
                ClearQueuedAttack();
                _boss.SetMoveAttackIntent(BossExcavatorAttack.None);
                TickPhaseChange();

                return;
            }

            if (_isPhaseChangeActive)
            {
                ResetPhaseChangeRuntime();
            }

            UpdateTimers();

            if (_currentAttack != BossExcavatorAttack.None)
            {
                _boss.RequestAutoState(BossExcavatorState.Attack);

                if (UpdateAttackRuntime())
                {
                    return;
                }
            }

            BossExcavatorState nextState = SelectAutoState();

            if (nextState == BossExcavatorState.Attack)
            {
                BossExcavatorAttack attack = _pendingAttack;
                _pendingAttack = BossExcavatorAttack.None;

                if (attack == BossExcavatorAttack.None)
                {
                    attack = BossExcavatorAttack.BucketStrike;
                }

                StartAttack(attack);
                _boss.RequestAutoState(BossExcavatorState.Attack);

                return;
            }

            UpdateScrapTrailRuntime(nextState);
            ApplyMovementCommands();
            _boss.RequestAutoState(nextState);
        }

        public void FixedTick()
        {
            if (_currentAttack == BossExcavatorAttack.Charge)
            {
                _chargeAttack.FixedTick();
            }
        }

        private void UpdateTimers()
        {
            float deltaTime = Time.deltaTime;
            float decisionDeltaTime = deltaTime * GetDecisionSpeedMult();

            _sweepCooldownTimer = Mathf.Max(0f, _sweepCooldownTimer - deltaTime);
            _bucketCooldownTimer = Mathf.Max(0f, _bucketCooldownTimer - deltaTime);
            _throwCooldownTimer = Mathf.Max(0f, _throwCooldownTimer - deltaTime);
            _chargeCooldownTimer = Mathf.Max(0f, _chargeCooldownTimer - deltaTime);
            _scrapTrailCooldownTimer = Mathf.Max(0f, _scrapTrailCooldownTimer - deltaTime);
            _postAttackTimer = Mathf.Max(0f, _postAttackTimer - decisionDeltaTime);
            _forcedChaseTimer = Mathf.Max(0f, _forcedChaseTimer - decisionDeltaTime);
            _moveStateTimer = Mathf.Max(0f, _moveStateTimer - decisionDeltaTime);
            _queuedAttackTimer = Mathf.Max(0f, _queuedAttackTimer - decisionDeltaTime);
        }

        private float GetDecisionSpeedMult()
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                return PhaseTwoDecisionSpeedMult;
            }

            return 1f;
        }

        private float GetTargetDistance()
        {
            Vector3 basePosition = _boss.Base.position;
            Vector3 targetPosition = _boss.Target.position;
            basePosition.y = 0f;
            targetPosition.y = 0f;

            return Vector3.Distance(basePosition, targetPosition);
        }

        private float GetTargetAngle(Transform pivot)
        {
            if (pivot == null)
            {
                return 180f;
            }

            Vector3 pivotPosition = pivot.position;
            Vector3 targetPosition = _boss.Target.position;
            targetPosition.y = pivotPosition.y;
            Vector3 lookDirection = targetPosition - pivotPosition;

            if (lookDirection.sqrMagnitude <= MinDirectionSqr)
            {
                return 0f;
            }

            return Vector3.Angle(pivot.forward, lookDirection.normalized);
        }

        private void TickPhaseChange()
        {
            if (_isPhaseChangeActive == false)
            {
                _isPhaseChangeActive = true;
                _phaseChangeTimer = _boss.Config.PhaseChangeDuration;
                _boss.SetAimLocked(false);
                _boss.SetArmLocked(false);
                _boss.SetChargeAlign(false);
                _boss.SetArmPose(BossExcavatorArmPose.BucketPrepare, _boss.Config.AttackPoseSpeedMult);
            }

            _phaseChangeTimer = Mathf.Max(0f, _phaseChangeTimer - Time.deltaTime);

            if (_phaseChangeTimer > 0f)
            {
                return;
            }

            ResetPhaseChangeRuntime();
            _boss.CompletePhaseChange();
        }

        private void ResetPhaseChangeRuntime()
        {
            _isPhaseChangeActive = false;
            _phaseChangeTimer = 0f;
        }

        private BossExcavatorAttack GetTargetAttack()
        {
            if (_currentAttack != BossExcavatorAttack.None)
            {
                return _currentAttack;
            }

            if (_queuedAttack != BossExcavatorAttack.None)
            {
                return _queuedAttack;
            }

            if (CanUseCombatData() == false)
            {
                return BossExcavatorAttack.None;
            }

            return GetStagingAttackIntent(GetTargetDistance());
        }

        private RoomCombatLock ResolveRoomCombatLock()
        {
            if (_boss.Base != null)
            {
                RoomCombatLock baseRoomCombatLock = _boss.Base.GetComponentInParent<RoomCombatLock>();

                if (baseRoomCombatLock != null)
                {
                    return baseRoomCombatLock;
                }
            }

            return _boss.GetComponentInParent<RoomCombatLock>();
        }

        private bool IsCombatRoomActive()
        {
            if (_roomCombatLock == null)
            {
                return true;
            }

            return _roomCombatLock.IsLocked;
        }
    }
}

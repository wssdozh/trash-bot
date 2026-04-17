using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private const float MinDirectionSqr = 0.0001f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorBucketAttack _bucketAttack;
        private readonly BossExcavatorThrowAttack _throwAttack;
        private readonly BossExcavatorChargeAttack _chargeAttack;
        private readonly BossExcavatorSweepAttack _sweepAttack;

        private float _sweepCooldownTimer;
        private float _bucketCooldownTimer;
        private float _throwCooldownTimer;
        private float _chargeCooldownTimer;
        private float _postAttackTimer;
        private float _forcedChaseTimer;
        private float _moveStateTimer;
        private float _phaseChangeTimer;
        private BossExcavatorAttack _currentAttack;
        private BossExcavatorAttack _pendingAttack;
        private BossExcavatorState _moveState;
        private bool _isPhaseChangeActive;

        public BossExcavatorAttack CurrentAttack => _currentAttack;

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
            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
        }

        public void Reset()
        {
            _sweepCooldownTimer = 0f;
            _bucketCooldownTimer = 0f;
            _throwCooldownTimer = 0f;
            _chargeCooldownTimer = 0f;
            _postAttackTimer = 0f;
            _forcedChaseTimer = 0f;
            _moveStateTimer = 0f;
            _phaseChangeTimer = 0f;
            _currentAttack = BossExcavatorAttack.None;
            _pendingAttack = BossExcavatorAttack.None;
            _moveState = BossExcavatorState.Chase;
            _isPhaseChangeActive = false;
            _sweepAttack.Reset();
            _bucketAttack.Reset();
            _throwAttack.Reset();
            _chargeAttack.Reset();
            ApplyCombatDefaults();
        }

        public void Tick()
        {
            if (_boss.IsDead)
            {
                ClearAttackRuntime(false);
                ResetPhaseChangeRuntime();

                return;
            }

            if (_boss.State == BossExcavatorState.PhaseChange)
            {
                ClearAttackRuntime(false);
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

            _sweepCooldownTimer = Mathf.Max(0f, _sweepCooldownTimer - deltaTime);
            _bucketCooldownTimer = Mathf.Max(0f, _bucketCooldownTimer - deltaTime);
            _throwCooldownTimer = Mathf.Max(0f, _throwCooldownTimer - deltaTime);
            _chargeCooldownTimer = Mathf.Max(0f, _chargeCooldownTimer - deltaTime);
            _postAttackTimer = Mathf.Max(0f, _postAttackTimer - deltaTime);
            _forcedChaseTimer = Mathf.Max(0f, _forcedChaseTimer - deltaTime);
            _moveStateTimer = Mathf.Max(0f, _moveStateTimer - deltaTime);
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
    }
}

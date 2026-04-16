using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorBrain
    {
        private const float MinDirectionSqr = 0.0001f;
        private const float CloseBucketFactor = 1f;
        private const float ChargePreferOffset = -0.75f;
        private const int FlowOrbit = 0;
        private const int FlowPressure = 1;
        private const int FlowRetreat = 2;

        private readonly BossExcavator _boss;
        private bool _attackStageEntered;
        private float _attackTimer;
        private float _bucketCooldownTimer;
        private float _throwCooldownTimer;
        private float _chargeCooldownTimer;
        private float _postAttackTimer;
        private float _moveFlowTimer;
        private float _moveStateCommitTimer;
        private int _moveFlow;
        private BossExcavatorState _committedMoveState;
        private bool _hasCommittedMoveState;
        private BossExcavatorAttack _currentAttack;
        private BossExcavatorAttack _lastAttack;

        public BossExcavatorAttack CurrentAttack => _currentAttack;

        public BossExcavatorBrain(BossExcavator boss)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            _boss = boss;
            _currentAttack = BossExcavatorAttack.None;
            _lastAttack = BossExcavatorAttack.None;
        }

        public void Reset()
        {
            _attackStageEntered = false;
            _attackTimer = 0f;
            _bucketCooldownTimer = 0f;
            _throwCooldownTimer = 0f;
            _chargeCooldownTimer = 0f;
            _postAttackTimer = 0f;
            _moveStateCommitTimer = 0f;
            _hasCommittedMoveState = false;
            _currentAttack = BossExcavatorAttack.None;
            _lastAttack = BossExcavatorAttack.None;
            SetMoveFlow(FlowOrbit);

            ApplyCombatDefaults();
        }

        public void Tick()
        {
            if (_boss.IsDead)
            {
                ClearAttackRuntime(false);

                return;
            }

            UpdateCooldowns();
            UpdatePostAttackTimer();

            if (_boss.State == BossExcavatorState.PhaseChange)
            {
                ClearAttackRuntime(false);

                return;
            }

            if (_currentAttack != BossExcavatorAttack.None)
            {
                _boss.RequestAutoState(BossExcavatorState.Attack);

                bool isAttackRunning = UpdateAttackRuntime();

                if (isAttackRunning)
                {
                    return;
                }
            }

            BossExcavatorState nextState = SelectAutoState();

            if (nextState == BossExcavatorState.Attack)
            {
                BossExcavatorAttack attack = SelectAttack();

                if (attack == BossExcavatorAttack.None)
                {
                    ApplyMovementCommands(BossExcavatorState.Reposition);
                    _boss.RequestAutoState(BossExcavatorState.Reposition);

                    return;
                }

                StartAttack(attack);
                _boss.RequestAutoState(BossExcavatorState.Attack);

                return;
            }

            ApplyMovementCommands(nextState);
            _boss.RequestAutoState(nextState);
        }

        private void UpdateCooldowns()
        {
            float deltaTime = Time.deltaTime;

            _bucketCooldownTimer = Mathf.Max(0f, _bucketCooldownTimer - deltaTime);
            _throwCooldownTimer = Mathf.Max(0f, _throwCooldownTimer - deltaTime);
            _chargeCooldownTimer = Mathf.Max(0f, _chargeCooldownTimer - deltaTime);
        }

        private void UpdatePostAttackTimer()
        {
            _postAttackTimer = Mathf.Max(0f, _postAttackTimer - Time.deltaTime);
        }

        private BossExcavatorState SelectAutoState()
        {
            if (CanUseCombatData() == false)
            {
                return BossExcavatorState.Idle;
            }

            float distanceToTarget = GetTargetDistance();
            float baseAngle = GetTargetAngle(_boss.Base);
            float cabinAngle = GetTargetAngle(_boss.Cabin);
            bool hasAttack = HasReadyAttack(distanceToTarget, baseAngle, cabinAngle);

            if (ShouldHardReposition(distanceToTarget, baseAngle))
            {
                return CommitMoveState(BossExcavatorState.Reposition, true);
            }

            if (hasAttack)
            {
                return BossExcavatorState.Attack;
            }

            if (_postAttackTimer > 0f)
            {
                UpdateMoveFlow(distanceToTarget);
                BossExcavatorState recoveryState = BossExcavatorState.Chase;

                if (distanceToTarget < _boss.Config.MinMoveDistance)
                {
                    recoveryState = BossExcavatorState.Reposition;
                }

                return CommitMoveState(recoveryState, false);
            }

            UpdateMoveFlow(distanceToTarget);

            if (distanceToTarget > _boss.Config.AttackChaseDistance)
            {
                return CommitMoveState(BossExcavatorState.Chase, false);
            }

            if (_moveFlow == FlowPressure)
            {
                return CommitMoveState(BossExcavatorState.Chase, false);
            }

            if (_moveFlow == FlowRetreat)
            {
                return CommitMoveState(BossExcavatorState.Reposition, false);
            }

            if (distanceToTarget < _boss.Config.MinMoveDistance + _boss.Config.DistanceHysteresis)
            {
                return CommitMoveState(BossExcavatorState.Reposition, false);
            }

            return CommitMoveState(BossExcavatorState.Chase, false);
        }

        private bool CanUseCombatData()
        {
            if (_boss.Target == null)
            {
                return false;
            }

            if (_boss.Base == null)
            {
                return false;
            }

            if (_boss.Cabin == null)
            {
                return false;
            }

            return true;
        }

        private bool ShouldHardReposition(float distanceToTarget, float baseAngle)
        {
            if (HasBadTargetPoint())
            {
                return true;
            }

            if (distanceToTarget < _boss.Config.StopDistance * 1.5f)
            {
                return true;
            }

            if (baseAngle > _boss.Config.RepositionBaseAngle)
            {
                return true;
            }

            return false;
        }

        private bool HasBadTargetPoint()
        {
            BossExcavatorTargetPoint targetPoint = _boss.Move.TargetPoint;

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

            return false;
        }

        private bool HasReadyAttack(float distanceToTarget, float baseAngle, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            bool canUseBucket = CanUseBucket(distanceToTarget, baseAngle, cabinAngle);
            bool canUseThrow = CanUseThrow(distanceToTarget, baseAngle, cabinAngle);
            bool canUseCharge = CanUseCharge(distanceToTarget, baseAngle);

            if (_boss.Phase == BossExcavatorPhase.PhaseOne)
            {
                canUseCharge = false;
            }

            if (canUseBucket)
            {
                return true;
            }

            if (canUseThrow)
            {
                return true;
            }

            if (canUseCharge)
            {
                return true;
            }

            return false;
        }

        private void UpdateMoveFlow(float distanceToTarget)
        {
            if (distanceToTarget > _boss.Config.AttackChaseDistance + _boss.Config.DistanceTolerance)
            {
                SetMoveFlow(FlowPressure);

                return;
            }

            if (distanceToTarget < _boss.Config.MinMoveDistance)
            {
                SetMoveFlow(FlowRetreat);

                return;
            }

            _moveFlowTimer = Mathf.Max(0f, _moveFlowTimer - Time.deltaTime);

            if (_moveFlowTimer > 0f)
            {
                return;
            }

            if (_moveFlow == FlowOrbit)
            {
                SetMoveFlow(FlowPressure);

                return;
            }

            if (_moveFlow == FlowPressure)
            {
                SetMoveFlow(FlowOrbit);

                return;
            }

            if (_moveFlow == FlowRetreat)
            {
                SetMoveFlow(FlowPressure);

                return;
            }

            SetMoveFlow(FlowPressure);
        }

        private void SetMoveFlow(int moveFlow)
        {
            _moveFlow = moveFlow;
            _moveFlowTimer = GetMoveFlowTime(moveFlow);
        }

        private float GetMoveFlowTime(int moveFlow)
        {
            if (moveFlow == FlowPressure)
            {
                return _boss.Config.MovePressureTime;
            }

            if (moveFlow == FlowRetreat)
            {
                return _boss.Config.MoveRetreatTime;
            }

            return _boss.Config.MoveOrbitTime;
        }

        private BossExcavatorState CommitMoveState(BossExcavatorState moveState, bool isForceSwitch)
        {
            _moveStateCommitTimer = Mathf.Max(0f, _moveStateCommitTimer - Time.deltaTime);

            if (_hasCommittedMoveState == false)
            {
                _committedMoveState = moveState;
                _hasCommittedMoveState = true;
                _moveStateCommitTimer = GetMoveCommitTime(moveState);

                return moveState;
            }

            if (isForceSwitch)
            {
                _committedMoveState = moveState;
                _moveStateCommitTimer = GetMoveCommitTime(moveState);

                return moveState;
            }

            if (moveState != _committedMoveState)
            {
                if (_moveStateCommitTimer > 0f)
                {
                    return _committedMoveState;
                }

                _committedMoveState = moveState;
                _moveStateCommitTimer = GetMoveCommitTime(moveState);
            }

            return _committedMoveState;
        }

        private float GetMoveCommitTime(BossExcavatorState moveState)
        {
            if (moveState == BossExcavatorState.Chase)
            {
                return _boss.Config.MoveChaseCommitTime;
            }

            return _boss.Config.MoveRepositionCommitTime;
        }

        private BossExcavatorAttack SelectAttack()
        {
            if (CanUseCombatData() == false)
            {
                return BossExcavatorAttack.None;
            }

            float distanceToTarget = GetTargetDistance();
            float baseAngle = GetTargetAngle(_boss.Base);
            float cabinAngle = GetTargetAngle(_boss.Cabin);

            bool canUseBucket = CanUseBucket(distanceToTarget, baseAngle, cabinAngle);
            bool canUseThrow = CanUseThrow(distanceToTarget, baseAngle, cabinAngle);
            bool canUseCharge = CanUseCharge(distanceToTarget, baseAngle);

            if (_boss.Phase == BossExcavatorPhase.PhaseOne)
            {
                canUseCharge = false;
            }

            BossExcavatorAttack selectedAttack = ChooseAttackPriority(distanceToTarget, canUseBucket, canUseThrow, canUseCharge);

            if (selectedAttack == BossExcavatorAttack.None)
            {
                return BossExcavatorAttack.None;
            }

            if (selectedAttack == _lastAttack)
            {
                BossExcavatorAttack alternativeAttack = ChooseAlternativeAttack(selectedAttack, canUseBucket, canUseThrow, canUseCharge);

                if (alternativeAttack != BossExcavatorAttack.None)
                {
                    selectedAttack = alternativeAttack;
                }
            }

            return selectedAttack;
        }

        private BossExcavatorAttack ChooseAttackPriority(float distanceToTarget, bool canUseBucket, bool canUseThrow, bool canUseCharge)
        {
            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                if (canUseCharge)
                {
                    float chargePreferDistance = _boss.Config.ChargeMinDistance + ChargePreferOffset;

                    if (distanceToTarget >= chargePreferDistance)
                    {
                        return BossExcavatorAttack.Charge;
                    }
                }
            }

            if (canUseBucket)
            {
                float bucketCloseDistance = _boss.Config.BucketMaxDistance * CloseBucketFactor;

                if (distanceToTarget <= bucketCloseDistance)
                {
                    return BossExcavatorAttack.BucketStrike;
                }
            }

            if (canUseBucket)
            {
                return BossExcavatorAttack.BucketStrike;
            }

            if (canUseThrow)
            {
                return BossExcavatorAttack.ThrowScrap;
            }

            if (canUseCharge)
            {
                return BossExcavatorAttack.Charge;
            }

            return BossExcavatorAttack.None;
        }

        private BossExcavatorAttack ChooseAlternativeAttack(BossExcavatorAttack selectedAttack, bool canUseBucket, bool canUseThrow, bool canUseCharge)
        {
            if (selectedAttack != BossExcavatorAttack.BucketStrike)
            {
                if (canUseBucket)
                {
                    return BossExcavatorAttack.BucketStrike;
                }
            }

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                if (selectedAttack != BossExcavatorAttack.Charge)
                {
                    if (canUseCharge)
                    {
                        return BossExcavatorAttack.Charge;
                    }
                }
            }

            if (selectedAttack != BossExcavatorAttack.Charge)
            {
                if (canUseCharge)
                {
                    return BossExcavatorAttack.Charge;
                }
            }

            if (selectedAttack != BossExcavatorAttack.ThrowScrap)
            {
                if (canUseThrow)
                {
                    return BossExcavatorAttack.ThrowScrap;
                }
            }

            return BossExcavatorAttack.None;
        }

        private bool CanUseBucket(float distanceToTarget, float baseAngle, float cabinAngle)
        {
            if (_bucketCooldownTimer > 0f)
            {
                return false;
            }

            if (distanceToTarget > _boss.Config.BucketMaxDistance)
            {
                return false;
            }

            if (baseAngle > _boss.Config.BucketBaseAngle)
            {
                return false;
            }

            if (cabinAngle > _boss.Config.BucketCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseThrow(float distanceToTarget, float baseAngle, float cabinAngle)
        {
            if (_throwCooldownTimer > 0f)
            {
                return false;
            }

            if (distanceToTarget < _boss.Config.ThrowMinDistance)
            {
                return false;
            }

            if (distanceToTarget > _boss.Config.ThrowMaxDistance)
            {
                return false;
            }

            if (baseAngle > _boss.Config.ThrowBaseAngle)
            {
                return false;
            }

            if (cabinAngle > _boss.Config.ThrowCabinAngle)
            {
                return false;
            }

            return true;
        }

        private bool CanUseCharge(float distanceToTarget, float baseAngle)
        {
            if (_chargeCooldownTimer > 0f)
            {
                return false;
            }

            if (distanceToTarget < _boss.Config.ChargeMinDistance)
            {
                return false;
            }

            if (distanceToTarget > _boss.Config.ChargeMaxDistance)
            {
                return false;
            }

            if (baseAngle > _boss.Config.ChargeBaseAngle)
            {
                return false;
            }

            return true;
        }

        private void StartAttack(BossExcavatorAttack attack)
        {
            _currentAttack = attack;
            _attackStageEntered = false;
            _attackTimer = GetAttackTime(attack);

            _boss.SetArmLocked(false);

            if (attack == BossExcavatorAttack.BucketStrike)
            {
                _boss.SetChargeAlign(false);
                _boss.SetAimLocked(true);
                _boss.SetArmPose(BossExcavatorArmPose.BucketPrepare, _boss.Config.AttackPoseSpeedMult);
            }
            else if (attack == BossExcavatorAttack.ThrowScrap)
            {
                _boss.SetChargeAlign(false);
                _boss.SetAimLocked(true);
                _boss.SetArmPose(BossExcavatorArmPose.GrabScrap, _boss.Config.AttackPoseSpeedMult);
            }
            else
            {
                _boss.SetChargeAlign(true);
                _boss.SetAimLocked(false);
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _boss.Config.AttackPoseSpeedMult);
            }
        }

        private float GetAttackTime(BossExcavatorAttack attack)
        {
            if (attack == BossExcavatorAttack.BucketStrike)
            {
                return _boss.Config.BucketPrepareTime + _boss.Config.BucketStrikeTime;
            }

            if (attack == BossExcavatorAttack.ThrowScrap)
            {
                return _boss.Config.ThrowGrabTime + _boss.Config.ThrowReleaseTime;
            }

            if (attack == BossExcavatorAttack.Charge)
            {
                return _boss.Config.ChargeAttackTime;
            }

            return 0f;
        }

        private bool UpdateAttackRuntime()
        {
            if (_currentAttack == BossExcavatorAttack.None)
            {
                return false;
            }

            _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);

            if (_currentAttack == BossExcavatorAttack.BucketStrike)
            {
                UpdateBucketAttackStage();
            }
            else if (_currentAttack == BossExcavatorAttack.ThrowScrap)
            {
                UpdateThrowAttackStage();
            }

            if (_attackTimer > 0f)
            {
                return true;
            }

            FinishAttack();

            return false;
        }

        private void UpdateBucketAttackStage()
        {
            if (_attackStageEntered)
            {
                return;
            }

            if (_attackTimer > _boss.Config.BucketStrikeTime)
            {
                return;
            }

            _attackStageEntered = true;
            _boss.SetArmPose(BossExcavatorArmPose.BucketStrike, _boss.Config.AttackPoseSpeedMult);
        }

        private void UpdateThrowAttackStage()
        {
            if (_attackStageEntered)
            {
                return;
            }

            if (_attackTimer > _boss.Config.ThrowReleaseTime)
            {
                return;
            }

            _attackStageEntered = true;
            _boss.SetArmPose(BossExcavatorArmPose.ThrowScrap, _boss.Config.AttackPoseSpeedMult);
        }

        private void FinishAttack()
        {
            BossExcavatorAttack completedAttack = _currentAttack;
            float cooldown = GetAttackCooldown(completedAttack);

            if (completedAttack == BossExcavatorAttack.BucketStrike)
            {
                _bucketCooldownTimer = cooldown;
            }
            else if (completedAttack == BossExcavatorAttack.ThrowScrap)
            {
                _throwCooldownTimer = cooldown;
            }
            else if (completedAttack == BossExcavatorAttack.Charge)
            {
                _chargeCooldownTimer = cooldown;
            }

            _lastAttack = completedAttack;
            _postAttackTimer = _boss.Config.AttackRecoveryTime;
            SetMoveFlow(FlowPressure);
            ClearAttackRuntime(true);
        }

        private float GetAttackCooldown(BossExcavatorAttack attack)
        {
            float cooldown = 0f;

            if (attack == BossExcavatorAttack.BucketStrike)
            {
                cooldown = _boss.Config.BucketAttackCooldown;
            }
            else if (attack == BossExcavatorAttack.ThrowScrap)
            {
                cooldown = _boss.Config.ThrowAttackCooldown;
            }
            else if (attack == BossExcavatorAttack.Charge)
            {
                cooldown = _boss.Config.ChargeAttackCooldown;
            }

            if (_boss.Phase == BossExcavatorPhase.PhaseTwo)
            {
                cooldown *= _boss.Config.PhaseTwoCooldownMult;
            }

            return Mathf.Max(0f, cooldown);
        }

        private void ClearAttackRuntime(bool restorePose)
        {
            if (_currentAttack == BossExcavatorAttack.None)
            {
                return;
            }

            _currentAttack = BossExcavatorAttack.None;
            _attackTimer = 0f;
            _attackStageEntered = false;

            ApplyCombatDefaults();

            if (restorePose)
            {
                _boss.SetArmPose(BossExcavatorArmPose.Neutral, _boss.Config.AttackPoseSpeedMult);
            }
        }

        private void ApplyCombatDefaults()
        {
            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
        }

        private void ApplyMovementCommands(BossExcavatorState moveState)
        {
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);

            if (moveState == BossExcavatorState.Chase)
            {
                _boss.SetChargeAlign(_moveFlow != FlowRetreat);

                return;
            }

            _boss.SetChargeAlign(false);
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
    }
}

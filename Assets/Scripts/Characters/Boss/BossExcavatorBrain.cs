using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorBrain
    {
        private const float MinDirectionSqr = 0.0001f;

        private readonly BossExcavator _boss;
        private readonly BossExcavatorBucketAttack _bucketAttack;

        private float _bucketCooldownTimer;
        private float _postAttackTimer;
        private float _moveStateTimer;
        private BossExcavatorAttack _currentAttack;
        private BossExcavatorState _moveState;

        public BossExcavatorAttack CurrentAttack => _currentAttack;

        public BossExcavatorBrain(BossExcavator boss)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            _boss = boss;
            _bucketAttack = new BossExcavatorBucketAttack(_boss, _boss.Config);
            _currentAttack = BossExcavatorAttack.None;
        }

        public void Reset()
        {
            _bucketCooldownTimer = 0f;
            _postAttackTimer = 0f;
            _moveStateTimer = 0f;
            _currentAttack = BossExcavatorAttack.None;
            _moveState = BossExcavatorState.Chase;
            _bucketAttack.Reset();
            ApplyCombatDefaults();
        }

        public void Tick()
        {
            if (_boss.IsDead)
            {
                ClearAttackRuntime(false);

                return;
            }

            if (_boss.State == BossExcavatorState.PhaseChange)
            {
                ClearAttackRuntime(false);

                return;
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
                StartAttack(BossExcavatorAttack.BucketStrike);
                _boss.RequestAutoState(BossExcavatorState.Attack);

                return;
            }

            ApplyMovementCommands();
            _boss.RequestAutoState(nextState);
        }

        private void UpdateTimers()
        {
            float deltaTime = Time.deltaTime;

            _bucketCooldownTimer = Mathf.Max(0f, _bucketCooldownTimer - deltaTime);
            _postAttackTimer = Mathf.Max(0f, _postAttackTimer - deltaTime);
            _moveStateTimer = Mathf.Max(0f, _moveStateTimer - deltaTime);
        }

        private BossExcavatorState SelectAutoState()
        {
            if (CanUseCombatData() == false)
            {
                return BossExcavatorState.Idle;
            }

            float targetDistance = GetTargetDistance();
            float baseAngle = GetTargetAngle(_boss.Base);
            float cabinAngle = GetTargetAngle(_boss.Cabin);

            if (CanUseBucket(targetDistance, baseAngle, cabinAngle))
            {
                return BossExcavatorState.Attack;
            }

            BossExcavatorState desiredMoveState = SelectMoveState(targetDistance, baseAngle);

            return ResolveMoveState(desiredMoveState, targetDistance, baseAngle);
        }

        private BossExcavatorState SelectMoveState(float targetDistance, float baseAngle)
        {
            if (ShouldReposition(targetDistance, baseAngle))
            {
                return BossExcavatorState.Reposition;
            }

            if (_postAttackTimer > 0f)
            {
                return GetRecoveryState(targetDistance);
            }

            return BossExcavatorState.Chase;
        }

        private BossExcavatorState ResolveMoveState(BossExcavatorState desiredMoveState, float targetDistance, float baseAngle)
        {
            if (IsForcedReposition(targetDistance, baseAngle))
            {
                SetMoveState(BossExcavatorState.Reposition);

                return _moveState;
            }

            if (_moveState != BossExcavatorState.Chase && _moveState != BossExcavatorState.Reposition)
            {
                SetMoveState(desiredMoveState);

                return _moveState;
            }

            if (_moveState != desiredMoveState)
            {
                if (_moveStateTimer > 0f)
                {
                    return _moveState;
                }

                SetMoveState(desiredMoveState);
            }

            return _moveState;
        }

        private BossExcavatorState GetRecoveryState(float targetDistance)
        {
            if (targetDistance < _boss.Move.MinMoveDistance)
            {
                return BossExcavatorState.Reposition;
            }

            if (IsRepositionTargetPoint(_boss.Move.TargetPoint))
            {
                return BossExcavatorState.Reposition;
            }

            return BossExcavatorState.Chase;
        }

        private bool ShouldReposition(float targetDistance, float baseAngle)
        {
            if (IsRepositionTargetPoint(_boss.Move.TargetPoint))
            {
                return true;
            }

            if (baseAngle > _boss.Config.RepositionBaseAngle)
            {
                if (targetDistance < _boss.Move.AttackChaseDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsForcedReposition(float targetDistance, float baseAngle)
        {
            if (targetDistance < _boss.Move.MinMoveDistance)
            {
                return true;
            }

            if (IsRepositionTargetPoint(_boss.Move.TargetPoint))
            {
                return true;
            }

            if (baseAngle > _boss.Config.RepositionBaseAngle)
            {
                if (targetDistance < _boss.Move.AttackChaseDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetMoveState(BossExcavatorState moveState)
        {
            _moveState = moveState;

            if (moveState == BossExcavatorState.Reposition)
            {
                _moveStateTimer = _boss.Config.MoveRepositionCommitTime;

                return;
            }

            if (moveState == BossExcavatorState.Chase)
            {
                _moveStateTimer = _boss.Config.MoveChaseCommitTime;

                return;
            }

            _moveStateTimer = 0f;
        }

        private bool IsRepositionTargetPoint(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                return true;
            }

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

        private bool CanUseBucket(float targetDistance, float baseAngle, float cabinAngle)
        {
            if (_postAttackTimer > 0f)
            {
                return false;
            }

            if (_bucketCooldownTimer > 0f)
            {
                return false;
            }

            if (targetDistance > _boss.Config.BucketMaxDistance)
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

        private void StartAttack(BossExcavatorAttack attack)
        {
            _currentAttack = attack;
            _boss.SetChargeAlign(false);
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);

            if (attack == BossExcavatorAttack.BucketStrike)
            {
                _bucketAttack.StartAttack();
            }
        }

        private bool UpdateAttackRuntime()
        {
            if (_currentAttack != BossExcavatorAttack.BucketStrike)
            {
                return false;
            }

            if (_bucketAttack.Tick())
            {
                return true;
            }

            FinishAttack();

            return false;
        }

        private void FinishAttack()
        {
            _bucketCooldownTimer = _boss.Config.BucketAttackCooldown;
            _postAttackTimer = _boss.Config.AttackRecoveryTime;
            ClearAttackRuntime(true);
        }

        private void ClearAttackRuntime(bool restorePose)
        {
            if (_currentAttack == BossExcavatorAttack.None)
            {
                return;
            }

            _currentAttack = BossExcavatorAttack.None;
            _bucketAttack.Cancel(restorePose);
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

        private void ApplyMovementCommands()
        {
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
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

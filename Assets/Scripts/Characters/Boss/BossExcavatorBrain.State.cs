using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorBrain
    {
        private BossExcavatorState SelectAutoState()
        {
            if (CanUseCombatData() == false)
            {
                _pendingAttack = BossExcavatorAttack.None;
                ClearQueuedAttack();

                return BossExcavatorState.Idle;
            }

            float targetDistance = GetTargetDistance();
            float baseAngle = GetTargetAngle(_boss.Base);
            float cabinAngle = GetTargetAngle(_boss.Cabin);
            BossExcavatorAttack attack = SelectAttack(targetDistance, baseAngle, cabinAngle);

            if (attack != BossExcavatorAttack.None)
            {
                return BossExcavatorState.Attack;
            }

            BossExcavatorState desiredMoveState = SelectMoveState(targetDistance, baseAngle, _queuedAttack);

            return ResolveMoveState(desiredMoveState, targetDistance, baseAngle);
        }

        private BossExcavatorState SelectMoveState(float targetDistance, float baseAngle, BossExcavatorAttack queuedAttack)
        {
            BossExcavatorState plannedMoveState = GetQueuedAttackMoveState(queuedAttack, targetDistance, baseAngle);

            if (plannedMoveState != BossExcavatorState.Idle)
            {
                return plannedMoveState;
            }

            if (ShouldReposition(targetDistance, baseAngle))
            {
                return BossExcavatorState.Reposition;
            }

            if (_forcedChaseTimer > 0f)
            {
                return BossExcavatorState.Chase;
            }

            if (_postAttackTimer > 0f)
            {
                return GetRecoveryState(targetDistance);
            }

            return BossExcavatorState.Chase;
        }

        private BossExcavatorState GetQueuedAttackMoveState(BossExcavatorAttack queuedAttack, float targetDistance, float baseAngle)
        {
            if (queuedAttack == BossExcavatorAttack.Charge)
            {
                if (ShouldPrepareChargeAttack(targetDistance, baseAngle))
                {
                    return BossExcavatorState.Reposition;
                }

                return BossExcavatorState.Chase;
            }

            if (queuedAttack == BossExcavatorAttack.ThrowScrap)
            {
                if (ShouldPrepareThrowAttack(targetDistance))
                {
                    return BossExcavatorState.Reposition;
                }

                return BossExcavatorState.Chase;
            }

            if (queuedAttack == BossExcavatorAttack.BucketStrike || queuedAttack == BossExcavatorAttack.Sweep)
            {
                return BossExcavatorState.Chase;
            }

            return BossExcavatorState.Idle;
        }

        private bool ShouldPrepareChargeAttack(float targetDistance, float baseAngle)
        {
            if (targetDistance <= GetClosePressureDistance())
            {
                return false;
            }

            if (targetDistance < _boss.Config.ChargeMinDistance)
            {
                return true;
            }

            float alignAngle = GetChargeStartAngle() + 10f;

            if (baseAngle > alignAngle)
            {
                if (targetDistance <= _boss.Config.ChargeMaxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldPrepareThrowAttack(float targetDistance)
        {
            if (targetDistance <= GetClosePressureDistance())
            {
                return false;
            }

            if (targetDistance < _boss.Config.ThrowMinDistance)
            {
                return true;
            }

            return false;
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
            if (ShouldForceCloseReposition(targetDistance))
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

            if (ShouldRecoverAttackAngle(targetDistance, baseAngle))
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
            if (ShouldForceCloseReposition(targetDistance))
            {
                return true;
            }

            if (IsRepositionTargetPoint(_boss.Move.TargetPoint))
            {
                return true;
            }

            if (ShouldRecoverAttackAngle(targetDistance, baseAngle))
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

        private bool ShouldForceCloseReposition(float targetDistance)
        {
            if (targetDistance >= _boss.Move.MinMoveDistance)
            {
                return false;
            }

            BossExcavatorAttack moveAttackIntent = GetMoveAttackIntent();

            if (moveAttackIntent == BossExcavatorAttack.BucketStrike)
            {
                return false;
            }

            if (moveAttackIntent == BossExcavatorAttack.Sweep)
            {
                return false;
            }

            return true;
        }

        private bool ShouldRecoverAttackAngle(float targetDistance, float baseAngle)
        {
            BossExcavatorAttack moveAttackIntent = GetMoveAttackIntent();

            if (moveAttackIntent == BossExcavatorAttack.BucketStrike || moveAttackIntent == BossExcavatorAttack.Sweep)
            {
                float meleeRecoverDistance = _boss.Config.BucketMaxDistance + (_boss.Config.DistanceTolerance * 0.55f);

                if (targetDistance <= meleeRecoverDistance)
                {
                    return false;
                }
            }

            float recoverDistance = _boss.Config.BucketMaxDistance + _boss.Config.DistanceTolerance;

            if (targetDistance > recoverDistance)
            {
                return false;
            }

            if (targetDistance < _boss.Move.MinMoveDistance)
            {
                return false;
            }

            float recoverAngle = _boss.Config.BucketBaseAngle + 24f;
            float minRecoverAngle = _boss.Config.RepositionBaseAngle * 0.72f;

            if (recoverAngle < minRecoverAngle)
            {
                recoverAngle = minRecoverAngle;
            }

            if (baseAngle > recoverAngle)
            {
                return true;
            }

            return false;
        }

        private float GetClosePressureDistance()
        {
            return _boss.Config.BucketMaxDistance + (_boss.Config.DistanceTolerance * 0.35f);
        }

        private void SetMoveState(BossExcavatorState moveState)
        {
            _moveState = moveState;

            if (moveState == BossExcavatorState.Reposition)
            {
                _moveStateTimer = _boss.Config.MoveRepositionCommitTime / GetDecisionSpeedMult();

                return;
            }

            if (moveState == BossExcavatorState.Chase)
            {
                _moveStateTimer = _boss.Config.MoveChaseCommitTime / GetDecisionSpeedMult();

                return;
            }

            _moveStateTimer = 0f;
        }

        private bool IsRepositionTargetPoint(BossExcavatorTargetPoint targetPoint)
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

            return false;
        }

        private bool CanUseCombatData()
        {
            if (IsCombatRoomActive() == false)
            {
                return false;
            }

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

        private void ApplyMovementCommands()
        {
            _boss.SetMoveAttackIntent(GetMoveAttackIntent());
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
            _boss.SetChargeAlign(ShouldUseQueuedChargeAlign());
        }

        private BossExcavatorAttack GetMoveAttackIntent()
        {
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

        private BossExcavatorAttack GetStagingAttackIntent(float targetDistance)
        {
            BossExcavatorAttack preferredAttack = GetPreferredRhythmAttack(targetDistance);

            if (CanQueueAttack(preferredAttack))
            {
                return preferredAttack;
            }

            BossExcavatorAttack secondaryAttack = GetSecondaryRhythmAttack(preferredAttack, targetDistance);

            if (CanQueueAttack(secondaryAttack))
            {
                return secondaryAttack;
            }

            if (IsClosePriorityDistance(targetDistance))
            {
                return BossExcavatorAttack.BucketStrike;
            }

            return BossExcavatorAttack.Charge;
        }

        private bool ShouldUseQueuedChargeAlign()
        {
            if (_queuedAttack != BossExcavatorAttack.Charge)
            {
                return false;
            }

            if (CanUseCombatData() == false)
            {
                return false;
            }

            float targetDistance = GetTargetDistance();
            float baseAngle = GetTargetAngle(_boss.Base);

            if (ShouldPrepareChargeAttack(targetDistance, baseAngle) == false)
            {
                return false;
            }

            return true;
        }
    }
}

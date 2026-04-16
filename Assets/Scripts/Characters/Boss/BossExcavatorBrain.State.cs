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

            BossExcavatorState desiredMoveState = SelectMoveState(targetDistance, baseAngle);

            return ResolveMoveState(desiredMoveState, targetDistance, baseAngle);
        }

        private BossExcavatorState SelectMoveState(float targetDistance, float baseAngle)
        {
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
            if (targetDistance < _boss.Move.MinMoveDistance)
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

        private bool ShouldRecoverAttackAngle(float targetDistance, float baseAngle)
        {
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

        private void ApplyMovementCommands()
        {
            _boss.SetAimLocked(false);
            _boss.SetArmLocked(false);
            _boss.SetChargeAlign(false);
        }
    }
}

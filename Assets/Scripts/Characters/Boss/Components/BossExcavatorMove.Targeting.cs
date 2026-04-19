using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private void UpdateTimers()
        {
            _flankSwitchTimer = Mathf.Max(0f, _flankSwitchTimer - Time.fixedDeltaTime);
            _targetSwitchTimer = Mathf.Max(0f, _targetSwitchTimer - Time.fixedDeltaTime);
            _combatTargetTimer = Mathf.Max(0f, _combatTargetTimer - Time.fixedDeltaTime);
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
            BossExcavatorTargetPoint attackIntentPoint;

            if (TrySelectAttackIntentTargetPoint(currentPoint, targetPoint, targetDistance, out attackIntentPoint))
            {
                return attackIntentPoint;
            }

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

        private bool TrySelectAttackIntentTargetPoint(
            Vector3 currentPoint,
            Vector3 targetPoint,
            float targetDistance,
            out BossExcavatorTargetPoint attackIntentPoint)
        {
            attackIntentPoint = BossExcavatorTargetPoint.PlayerCenter;

            if (_attackIntent == BossExcavatorAttack.None)
            {
                return false;
            }

            if (_attackIntent == BossExcavatorAttack.Sweep)
            {
                float sweepIntentDistance = GetAttackIntentDistance();

                if (targetDistance > sweepIntentDistance + GetDistanceHysteresis())
                {
                    attackIntentPoint = BossExcavatorTargetPoint.PlayerCenter;

                    return true;
                }

                attackIntentPoint = SelectFlankPoint(currentPoint, targetPoint);

                return true;
            }

            if (_attackIntent == BossExcavatorAttack.ThrowScrap || _attackIntent == BossExcavatorAttack.Charge)
            {
                float rangedIntentDistance = GetAttackIntentDistance();

                if (targetDistance < rangedIntentDistance - GetDistanceHysteresis())
                {
                    attackIntentPoint = BossExcavatorTargetPoint.PlayerBack;

                    return true;
                }

                attackIntentPoint = BossExcavatorTargetPoint.PlayerCenter;

                return true;
            }

            attackIntentPoint = BossExcavatorTargetPoint.PlayerCenter;

            return true;
        }

        private bool ShouldUseRetreat(float targetDistance)
        {
            if (ShouldSuppressRetreat())
            {
                return false;
            }

            float retreatTriggerDistance = GetRetreatTriggerDistance();

            if (targetDistance < retreatTriggerDistance)
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                if (targetDistance < retreatTriggerDistance + GetDistanceHysteresis())
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldUseCenter(float targetDistance)
        {
            if (targetDistance > GetPressureCenterDistance())
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                if (targetDistance > GetCenterHoldDistance())
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

            if (ShouldForceAttackIntentRetarget(nextTargetPoint))
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

            if (_combatTargetTimer > 0f)
            {
                if (IsCombatPoint(nextTargetPoint))
                {
                    if (IsCombatPoint(_targetPoint))
                    {
                        return true;
                    }
                }
            }

            if (_targetSwitchTimer <= 0f)
            {
                return false;
            }

            return true;
        }

        private bool ShouldForceAttackIntentRetarget(BossExcavatorTargetPoint nextTargetPoint)
        {
            if (_attackIntent != BossExcavatorAttack.BucketStrike)
            {
                return false;
            }

            if (nextTargetPoint != BossExcavatorTargetPoint.PlayerCenter)
            {
                return false;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerLeft)
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                return true;
            }

            return false;
        }

        private bool IsCombatPoint(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerLeft)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return true;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                return true;
            }

            return false;
        }

        private float GetTargetPointCommitTime(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                return _config.MovePressureTime;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerLeft)
            {
                return _config.MoveOrbitTime;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return _config.MoveOrbitTime;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                return _config.MoveRetreatTime;
            }

            return 0f;
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

    }
}

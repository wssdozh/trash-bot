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
                    attackIntentPoint = SelectFlankPoint(currentPoint, targetPoint);

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

        private Vector3 BuildDesiredPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint, BossExcavatorTargetPoint targetPointType)
        {
            Vector3 rawPoint = BuildRawPoint(currentPoint, targetPoint, arenaCenterPoint, targetPointType);
            Vector3 resolvedPoint;
            float stopDistance = GetStopDistance(targetPointType);

            if (ShouldHoldCurrentPoint(currentPoint, rawPoint, targetPointType))
            {
                return currentPoint;
            }

            if (TryResolvePoint(currentPoint, rawPoint, stopDistance, out resolvedPoint))
            {
                return resolvedPoint;
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerLeft || targetPointType == BossExcavatorTargetPoint.PlayerRight)
            {
                Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetCenterDistance(currentPoint, targetPoint));

                if (TryResolvePoint(currentPoint, centerPoint, stopDistance, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerBack)
            {
                int oppositeRecoverSign = -GetRecoverSideSign(currentPoint, targetPoint);
                Vector3 oppositeRecoverPoint = BuildRecoverPoint(currentPoint, targetPoint, oppositeRecoverSign);

                if (TryResolvePoint(currentPoint, oppositeRecoverPoint, stopDistance, out resolvedPoint))
                {
                    return resolvedPoint;
                }

                Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetCenterDistance(currentPoint, targetPoint));

                if (TryResolvePoint(currentPoint, centerPoint, stopDistance, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            else
            {
                Vector3 chasePoint = BuildCenterPoint(currentPoint, targetPoint, GetAttackChaseDistance());

                if (TryResolvePoint(currentPoint, chasePoint, stopDistance, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (TryResolvePoint(currentPoint, arenaCenterPoint, stopDistance, out resolvedPoint))
            {
                return resolvedPoint;
            }

            return currentPoint;
        }

        private bool ShouldHoldCurrentPoint(Vector3 currentPoint, Vector3 rawPoint, BossExcavatorTargetPoint targetPointType)
        {
            if (targetPointType != BossExcavatorTargetPoint.PlayerCenter)
            {
                return false;
            }

            if (_attackIntent == BossExcavatorAttack.None)
            {
                return false;
            }

            Vector3 safePoint = ClampRoomPoint(rawPoint);
            float holdDistance = GetStopDistance(targetPointType) + _config.DesiredPointDeadZone;

            if (Vector3.Distance(currentPoint, safePoint) > holdDistance)
            {
                return false;
            }

            return true;
        }

        private Vector3 BuildRawPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint, BossExcavatorTargetPoint targetPointType)
        {
            if (targetPointType == BossExcavatorTargetPoint.PlayerCenter)
            {
                return BuildCenterPoint(currentPoint, targetPoint, GetCenterDistance(currentPoint, targetPoint));
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
                return BuildRecoverPoint(currentPoint, targetPoint, GetRecoverSideSign(currentPoint, targetPoint));
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
                return BuildCenterPoint(currentPoint, targetPoint, GetChargeAlignTargetDistance(currentPoint, targetPoint));
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
            Vector3 toTarget = targetPoint - currentPoint;
            Vector3 targetDirection = GetPlanarDirection(toTarget);

            if (targetDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                targetDirection = GetPlanarForward(_target.forward);
            }

            Vector3 tangentDirection = GetOrbitDirection(targetDirection, sideSign);
            float targetDistance = Vector3.Distance(currentPoint, targetPoint);
            Vector3 ringDirection = Vector3.zero;
            float ringWeight = 0f;

            if (targetDistance > GetMediumDistance() + GetDistanceHysteresis())
            {
                ringDirection = targetDirection;
                ringWeight = 0.75f;
            }

            else if (targetDistance < GetCloseRetreatDistance())
            {
                if (ShouldHoldOrbitPressure())
                {
                    ringDirection = targetDirection;
                    ringWeight = 0.28f;
                }

                else
                {
                    ringDirection = -targetDirection;
                    ringWeight = 0.75f;
                }
            }

            else if (targetDistance <= GetClosePressureDistance())
            {
                ringDirection = targetDirection;
                ringWeight = 0.45f;
            }

            Vector3 desiredDirection = tangentDirection + (ringDirection * ringWeight);
            desiredDirection = GetPlanarDirection(desiredDirection);

            if (desiredDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                desiredDirection = tangentDirection;
            }

            return currentPoint + desiredDirection * GetOrbitStepDistance();
        }

        private Vector3 BuildRecoverPoint(Vector3 currentPoint, Vector3 targetPoint, int recoverSideSign)
        {
            Vector3 toTarget = targetPoint - currentPoint;
            Vector3 targetDirection = GetPlanarDirection(toTarget);

            if (targetDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                targetDirection = GetPlanarForward(_target.forward);
            }

            Vector3 sideDirection = GetOrbitDirection(targetDirection, recoverSideSign);
            Vector3 recoverDirection = (-targetDirection * 1.1f) + (sideDirection * 0.9f);
            recoverDirection = GetPlanarDirection(recoverDirection);

            if (recoverDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                recoverDirection = -targetDirection;
            }

            float targetDistance = Vector3.Distance(currentPoint, targetPoint);
            float recoverStepDistance = GetRecoverStepDistance(targetDistance);

            return currentPoint + recoverDirection * recoverStepDistance;
        }

        private int GetRecoverSideSign(Vector3 currentPoint, Vector3 targetPoint)
        {
            int preferredSign = _flankSign;

            if (preferredSign == 0)
            {
                preferredSign = 1;
            }

            Vector3 preferredPoint = BuildRecoverPoint(currentPoint, targetPoint, preferredSign);
            Vector3 alternativePoint = BuildRecoverPoint(currentPoint, targetPoint, -preferredSign);
            float preferredScore = GetPointScore(currentPoint, preferredPoint);
            float alternativeScore = GetPointScore(currentPoint, alternativePoint);

            if (preferredScore <= alternativeScore + _config.FlankSwitchThreshold)
            {
                return preferredSign;
            }

            SetFlankSign(-preferredSign);

            return -preferredSign;
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

        private bool TryResolvePoint(Vector3 currentPoint, Vector3 point, float stopDistance, out Vector3 resolvedPoint)
        {
            Vector3 safePoint = ClampRoomPoint(point);

            if (TryGetNavPoint(safePoint, out resolvedPoint) == false)
            {
                return false;
            }

            if (HasCompletePath(currentPoint, resolvedPoint) == false)
            {
                return TryGetReachPoint(currentPoint, resolvedPoint, out resolvedPoint);
            }

            if (Vector3.Distance(currentPoint, resolvedPoint) <= stopDistance + _config.DesiredPointDeadZone)
            {
                resolvedPoint = currentPoint;

                return false;
            }

            return true;
        }

        private float GetPointScore(Vector3 currentPoint, Vector3 point)
        {
            Vector3 resolvedPoint;

            if (TryResolvePoint(currentPoint, point, _config.StopDistance, out resolvedPoint) == false)
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

            if (HasCompletePath(currentPoint, resolvedPoint) == false)
            {
                pointScore += _config.BlockedPenalty;
            }

            return pointScore;
        }

        private float GetStopDistance(BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                if (_attackIntent == BossExcavatorAttack.BucketStrike)
                {
                    float bucketStopDistance = Mathf.Min(_config.StopDistance, _config.DistanceTolerance * 0.35f);

                    return Mathf.Max(0.1f, bucketStopDistance);
                }
            }

            return _config.StopDistance;
        }

        private float GetRetreatTriggerDistance()
        {
            float closeDistance = _config.StopDistance + 0.95f;
            float sweepDistance = _config.SweepMaxDistance - 1.1f;

            return Mathf.Max(closeDistance, sweepDistance);
        }

        private float GetCenterHoldDistance()
        {
            return GetPressureCenterDistance() - GetDistanceHysteresis();
        }

        private float GetCenterDistance(Vector3 currentPoint, Vector3 targetPoint)
        {
            if (_attackIntent != BossExcavatorAttack.None)
            {
                return GetAttackIntentDistance();
            }

            return GetPressureCenterDistance();
        }

        private float GetPressureCenterDistance()
        {
            float pressureDistance = _config.BucketMaxDistance - (GetDistanceTolerance() * 0.2f);

            return Mathf.Max(GetMinMoveDistance(), pressureDistance);
        }

        private float GetClosePressureDistance()
        {
            return _config.BucketMaxDistance + (GetDistanceTolerance() * 0.35f);
        }

        private float GetCloseRetreatDistance()
        {
            float sweepDistance = _config.SweepMaxDistance - (GetDistanceTolerance() * 0.5f);
            float stopDistance = _config.StopDistance + 1f;

            return Mathf.Max(stopDistance, sweepDistance);
        }

        private float GetRecoverStepDistance(float targetDistance)
        {
            float distanceGap = GetRetreatDistance() - targetDistance;
            float minStepDistance = GetOrbitStepDistance() * 0.85f;
            float maxStepDistance = GetOrbitStepDistance() * 1.35f;
            float desiredStepDistance = distanceGap + GetDistanceTolerance();

            return Mathf.Clamp(desiredStepDistance, minStepDistance, maxStepDistance);
        }

        private bool ShouldSuppressRetreat()
        {
            if (_attackIntent == BossExcavatorAttack.None)
            {
                return false;
            }

            return true;
        }

        private bool ShouldHoldOrbitPressure()
        {
            if (_attackIntent == BossExcavatorAttack.Sweep)
            {
                return true;
            }

            return false;
        }

        private float GetAttackIntentDistance()
        {
            if (_attackIntent == BossExcavatorAttack.BucketStrike)
            {
                float bucketIntentDistance = _config.BucketMaxDistance * 0.68f;
                float minBucketDistance = _config.StopDistance + 0.9f;

                return Mathf.Max(minBucketDistance, bucketIntentDistance);
            }

            if (_attackIntent == BossExcavatorAttack.Sweep)
            {
                float sweepIntentDistance = _config.SweepMaxDistance * 0.72f;
                float minSweepDistance = _config.StopDistance + 0.35f;

                return Mathf.Max(minSweepDistance, sweepIntentDistance);
            }

            if (_attackIntent == BossExcavatorAttack.ThrowScrap)
            {
                return (_config.ThrowMinDistance + _config.ThrowMaxDistance) * 0.5f;
            }

            if (_attackIntent == BossExcavatorAttack.Charge)
            {
                return Mathf.Lerp(_config.ChargeMinDistance, _config.ChargeMaxDistance, 0.58f);
            }

            return GetPressureCenterDistance();
        }

        private float GetChargeAlignTargetDistance(Vector3 currentPoint, Vector3 targetPoint)
        {
            float currentDistance = Vector3.Distance(currentPoint, targetPoint);
            float idealChargeDistance = GetAttackIntentDistance();
            float minChargeDistance = _config.ChargeMinDistance + 0.35f;
            float maxChargeDistance = _config.ChargeMaxDistance - 0.25f;

            if (maxChargeDistance < minChargeDistance)
            {
                maxChargeDistance = minChargeDistance;
            }

            if (currentDistance < _config.ChargeMinDistance)
            {
                return minChargeDistance;
            }

            if (currentDistance <= _config.ChargeMaxDistance)
            {
                float alignedDistance = currentDistance + 0.55f;

                return Mathf.Clamp(alignedDistance, minChargeDistance, maxChargeDistance);
            }

            return Mathf.Clamp(idealChargeDistance, minChargeDistance, maxChargeDistance);
        }
    }
}

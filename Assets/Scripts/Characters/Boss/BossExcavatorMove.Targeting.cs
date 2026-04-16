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

        private bool ShouldUseRetreat(float targetDistance)
        {
            if (targetDistance < GetMinMoveDistance())
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                if (targetDistance < GetMediumDistance())
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldUseCenter(float targetDistance)
        {
            if (targetDistance > GetAttackChaseDistance())
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                if (targetDistance > GetMediumDistance() - GetDistanceHysteresis())
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

            if (TryResolvePoint(currentPoint, rawPoint, out resolvedPoint))
            {
                return resolvedPoint;
            }

            if (targetPointType == BossExcavatorTargetPoint.PlayerLeft || targetPointType == BossExcavatorTargetPoint.PlayerRight)
            {
                Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());

                if (TryResolvePoint(currentPoint, centerPoint, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (targetPointType != BossExcavatorTargetPoint.PlayerBack)
            {
                Vector3 backPoint = BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());

                if (TryResolvePoint(currentPoint, backPoint, out resolvedPoint))
                {
                    return resolvedPoint;
                }
            }

            if (TryResolvePoint(currentPoint, arenaCenterPoint, out resolvedPoint))
            {
                return resolvedPoint;
            }

            return currentPoint;
        }

        private Vector3 BuildRawPoint(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint, BossExcavatorTargetPoint targetPointType)
        {
            if (targetPointType == BossExcavatorTargetPoint.PlayerCenter)
            {
                return BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
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
                return BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());
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
                return BuildCenterPoint(currentPoint, targetPoint, GetChargeAlignDistance());
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

            if (targetDistance > GetMediumDistance() + GetDistanceHysteresis())
            {
                ringDirection = targetDirection;
            }

            else if (targetDistance < GetMediumDistance() - GetDistanceHysteresis())
            {
                ringDirection = -targetDirection;
            }

            Vector3 desiredDirection = tangentDirection + (ringDirection * 0.75f);
            desiredDirection = GetPlanarDirection(desiredDirection);

            if (desiredDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                desiredDirection = tangentDirection;
            }

            return currentPoint + desiredDirection * GetOrbitStepDistance();
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

        private bool TryResolvePoint(Vector3 currentPoint, Vector3 point, out Vector3 resolvedPoint)
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

            return true;
        }

        private float GetPointScore(Vector3 currentPoint, Vector3 point)
        {
            Vector3 resolvedPoint;

            if (TryResolvePoint(currentPoint, point, out resolvedPoint) == false)
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
            return _config.StopDistance;
        }
    }
}

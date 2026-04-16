using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorMove : MonoBehaviour
    {
        private const float MinSqrMagnitude = 0.0001f;
        private const int PointSearchCount = 4;
        private const int WallPushCount = 3;
        private const float PointSearchAngleStep = 20f;
        private const float MinMoveSpeedFactor = 0.18f;
        private const float MinPushDelta = 0.05f;

        [SerializeField] private Transform _arenaCenter;
        [SerializeField] private LayerMask _obstacleMask;

        private BossExcavatorConfig _config;
        private RoomRuntimeState _roomRuntimeState;
        private Transform _base;
        private Rigidbody _baseRigidbody;
        private Transform _target;
        private BossExcavatorTargetPoint _targetPoint;
        private Vector3 _desiredPoint;
        private Vector3 _runtimeArenaCenter;
        private Vector3 _fallbackArenaCenter;
        private bool _isChargeAlign;
        private bool _hasRuntimeArenaCenter;
        private bool _hasFallbackArenaCenter;
        private float _flankSwitchTimer;
        private float _targetSwitchTimer;
        private int _flankSign = 1;
        private bool _isMoveAllowed;

        public BossExcavatorTargetPoint TargetPoint => _targetPoint;
        public Vector3 DesiredPoint => _desiredPoint;

        private void Awake()
        {
            ValidateDependencies();
        }

        public void Setup(BossExcavatorConfig config, Transform baseTransform, Rigidbody baseRigidbody, Transform target)
        {
            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            if (baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(baseRigidbody));
            }

            _config = config;
            _base = baseTransform;
            _baseRigidbody = baseRigidbody;
            _target = target;
            _targetPoint = BossExcavatorTargetPoint.ArenaCenter;
            _desiredPoint = GetPlanarPosition(_baseRigidbody.position);
            _fallbackArenaCenter = _desiredPoint;
            _hasFallbackArenaCenter = true;
            _targetSwitchTimer = 0f;
            _isMoveAllowed = false;

            ResolveRoomState();
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new InvalidOperationException(nameof(target));
            }

            _target = target;
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _isChargeAlign = isChargeAlign;
        }

        public void SetArenaCenter(Vector3 arenaCenter)
        {
            _runtimeArenaCenter = GetPlanarPosition(arenaCenter);
            _hasRuntimeArenaCenter = true;
        }

        public void FixedTick()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_base == null)
            {
                throw new InvalidOperationException(nameof(_base));
            }

            if (_baseRigidbody == null)
            {
                throw new InvalidOperationException(nameof(_baseRigidbody));
            }

            if (_target == null)
            {
                return;
            }

            UpdateTimers();

            Vector3 basePosition = GetPlanarPosition(_baseRigidbody.position);
            Vector3 targetPosition = GetPlanarPosition(_target.position);
            Vector3 arenaCenterPosition = GetArenaCenterPosition();

            BossExcavatorTargetPoint nextTargetPoint = SelectTargetPoint(basePosition, targetPosition, arenaCenterPosition);
            nextTargetPoint = ResolveTargetPoint(nextTargetPoint);

            Vector3 nextDesiredPoint = BuildTargetPoint(basePosition, targetPosition, arenaCenterPosition, nextTargetPoint);
            nextDesiredPoint = ResolveDesiredPoint(basePosition, nextDesiredPoint, targetPosition, arenaCenterPosition, nextTargetPoint);
            nextDesiredPoint = StabilizeDesiredPoint(basePosition, nextDesiredPoint, nextTargetPoint);

            if (nextTargetPoint != _targetPoint)
            {
                _targetSwitchTimer = _config.TargetSwitchCooldown;
                _isMoveAllowed = false;
            }

            _targetPoint = nextTargetPoint;
            _desiredPoint = nextDesiredPoint;

            Vector3 lookPoint = GetLookPoint(targetPosition, nextDesiredPoint, nextTargetPoint);
            Quaternion nextRotation = RotateBase(basePosition, lookPoint);

            MoveBase(basePosition, arenaCenterPosition, nextRotation);
        }

        private void UpdateTimers()
        {
            if (_flankSwitchTimer <= 0f)
            {
                _flankSwitchTimer = 0f;
            }
            else
            {
                _flankSwitchTimer = Mathf.Max(0f, _flankSwitchTimer - Time.fixedDeltaTime);
            }

            if (_targetSwitchTimer <= 0f)
            {
                _targetSwitchTimer = 0f;
            }
            else
            {
                _targetSwitchTimer = Mathf.Max(0f, _targetSwitchTimer - Time.fixedDeltaTime);
            }
        }

        private BossExcavatorTargetPoint ResolveTargetPoint(BossExcavatorTargetPoint nextTargetPoint)
        {
            if (ShouldKeepTargetPoint(nextTargetPoint))
            {
                return _targetPoint;
            }

            return nextTargetPoint;
        }

        private bool ShouldKeepTargetPoint(BossExcavatorTargetPoint nextTargetPoint)
        {
            if (nextTargetPoint == _targetPoint)
            {
                return false;
            }

            if (_targetSwitchTimer <= 0f)
            {
                return false;
            }

            if (IsImmediateTargetPoint(nextTargetPoint))
            {
                return false;
            }

            if (IsImmediateTargetPoint(_targetPoint))
            {
                return false;
            }

            return true;
        }

        private bool IsImmediateTargetPoint(BossExcavatorTargetPoint targetPoint)
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

        private BossExcavatorTargetPoint SelectTargetPoint(Vector3 basePosition, Vector3 targetPosition, Vector3 arenaCenterPosition)
        {
            if (ShouldReturnToCenter(basePosition, arenaCenterPosition))
            {
                return BossExcavatorTargetPoint.ArenaCenter;
            }

            int wallHitCount = GetWallHitCount(basePosition);

            if (wallHitCount > 1)
            {
                return BossExcavatorTargetPoint.CornerEscape;
            }

            if (wallHitCount > 0)
            {
                return BossExcavatorTargetPoint.WallEscape;
            }

            if (_isChargeAlign)
            {
                return BossExcavatorTargetPoint.ChargeAlign;
            }

            float distanceToTarget = Vector3.Distance(basePosition, targetPosition);

            if (ShouldUseRetreatPoint(distanceToTarget))
            {
                return BossExcavatorTargetPoint.PlayerBack;
            }

            if (ShouldUseCenterPoint(distanceToTarget))
            {
                return BossExcavatorTargetPoint.PlayerCenter;
            }

            return SelectFlankPoint(basePosition, targetPosition);
        }

        private bool ShouldUseRetreatPoint(float distanceToTarget)
        {
            if (distanceToTarget < _config.MinMoveDistance)
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                if (distanceToTarget < _config.MediumDistance - _config.DistanceHysteresis)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldUseCenterPoint(float distanceToTarget)
        {
            if (distanceToTarget > _config.MaxMoveDistance)
            {
                return true;
            }

            if (_targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                if (distanceToTarget > _config.MediumDistance + _config.DistanceHysteresis)
                {
                    return true;
                }
            }

            return false;
        }

        private BossExcavatorTargetPoint SelectFlankPoint(Vector3 basePosition, Vector3 targetPosition)
        {
            Vector3 leftPoint = BuildOrbitPoint(basePosition, targetPosition, -1f);
            Vector3 rightPoint = BuildOrbitPoint(basePosition, targetPosition, 1f);
            float leftScore = GetPointScore(basePosition, leftPoint);
            float rightScore = GetPointScore(basePosition, rightPoint);
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

        private Vector3 BuildTargetPoint(Vector3 basePosition, Vector3 targetPosition, Vector3 arenaCenterPosition, BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                return BuildCenterPoint(basePosition, targetPosition, _config.MediumDistance);
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerLeft)
            {
                return BuildOrbitPoint(basePosition, targetPosition, -1f);
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerRight)
            {
                return BuildOrbitPoint(basePosition, targetPosition, 1f);
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerBack)
            {
                return BuildCenterPoint(basePosition, targetPosition, _config.RetreatDistance);
            }

            if (targetPoint == BossExcavatorTargetPoint.WallEscape)
            {
                return BuildWallEscapePoint(basePosition, arenaCenterPosition);
            }

            if (targetPoint == BossExcavatorTargetPoint.CornerEscape)
            {
                return BuildCornerEscapePoint(basePosition, arenaCenterPosition);
            }

            if (targetPoint == BossExcavatorTargetPoint.ArenaCenter)
            {
                return arenaCenterPosition;
            }

            if (targetPoint == BossExcavatorTargetPoint.ChargeAlign)
            {
                return BuildCenterPoint(basePosition, targetPosition, _config.ChargeAlignDistance);
            }

            return arenaCenterPosition;
        }

        private Vector3 BuildCenterPoint(Vector3 basePosition, Vector3 targetPosition, float distance)
        {
            Vector3 fromTargetToBase = basePosition - targetPosition;

            if (fromTargetToBase.sqrMagnitude <= MinSqrMagnitude)
            {
                fromTargetToBase = -GetPlanarForward(_target.forward);
            }

            Vector3 direction = fromTargetToBase.normalized;

            return targetPosition + direction * distance;
        }

        private Vector3 BuildOrbitPoint(Vector3 basePosition, Vector3 targetPosition, float sideSign)
        {
            Vector3 fromTargetToBase = basePosition - targetPosition;

            if (fromTargetToBase.sqrMagnitude <= MinSqrMagnitude)
            {
                fromTargetToBase = -GetPlanarForward(_target.forward);
            }

            Vector3 baseDirection = fromTargetToBase.normalized;
            Quaternion rotation = Quaternion.AngleAxis(_config.FlankAngle * sideSign, Vector3.up);
            Vector3 orbitDirection = rotation * baseDirection;

            return targetPosition + orbitDirection * _config.MediumDistance;
        }

        private Vector3 BuildWallEscapePoint(Vector3 basePosition, Vector3 arenaCenterPosition)
        {
            return BuildEscapePoint(basePosition, arenaCenterPosition, _config.WallEscapeDistance);
        }

        private Vector3 BuildCornerEscapePoint(Vector3 basePosition, Vector3 arenaCenterPosition)
        {
            return BuildEscapePoint(basePosition, arenaCenterPosition, _config.CornerEscapeDistance);
        }

        private Vector3 BuildEscapePoint(Vector3 basePosition, Vector3 arenaCenterPosition, float escapeDistance)
        {
            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            hitCount += TryAddWallNormal(basePosition, Vector3.forward, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.back, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.left, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.right, _config.WallProbeDistance, ref normalSum);

            Vector3 escapeDirection = GetPlanarDirection(normalSum);
            Vector3 centerDirection = GetPlanarDirection(arenaCenterPosition - basePosition);

            if (centerDirection.sqrMagnitude > MinSqrMagnitude)
            {
                escapeDirection += centerDirection.normalized * _config.EscapeCenterWeight;
            }

            escapeDirection = GetPlanarDirection(escapeDirection);

            if (escapeDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                if (centerDirection.sqrMagnitude <= MinSqrMagnitude)
                {
                    return arenaCenterPosition;
                }

                escapeDirection = centerDirection;
            }

            if (hitCount <= 0)
            {
                if (centerDirection.sqrMagnitude > MinSqrMagnitude)
                {
                    escapeDirection = centerDirection;
                }
            }

            return basePosition + escapeDirection.normalized * escapeDistance;
        }

        private Vector3 ResolveDesiredPoint(Vector3 basePosition, Vector3 desiredPoint, Vector3 targetPosition, Vector3 arenaCenterPosition, BossExcavatorTargetPoint targetPoint)
        {
            Vector3 safePoint = ResolveSafePoint(desiredPoint, arenaCenterPosition);

            if (HasPathBlocked(basePosition, safePoint) == false)
            {
                return safePoint;
            }

            float distanceToPoint = Vector3.Distance(basePosition, safePoint);
            Vector3 directionToPoint = GetPlanarDirection(safePoint - basePosition);
            Vector3 openPoint;

            if (TryFindOpenPoint(basePosition, directionToPoint, distanceToPoint, arenaCenterPosition, out openPoint))
            {
                return openPoint;
            }

            if (targetPoint != BossExcavatorTargetPoint.WallEscape)
            {
                if (targetPoint != BossExcavatorTargetPoint.CornerEscape)
                {
                    Vector3 escapePoint = BuildWallEscapePoint(basePosition, arenaCenterPosition);
                    escapePoint = ResolveSafePoint(escapePoint, arenaCenterPosition);

                    if (HasPathBlocked(basePosition, escapePoint) == false)
                    {
                        return escapePoint;
                    }
                }
            }

            Vector3 centerPoint = BuildCenterPoint(basePosition, targetPosition, _config.MediumDistance);
            centerPoint = ResolveSafePoint(centerPoint, arenaCenterPosition);

            if (HasPathBlocked(basePosition, centerPoint) == false)
            {
                return centerPoint;
            }

            return ResolveSafePoint(arenaCenterPosition, arenaCenterPosition);
        }

        private Vector3 StabilizeDesiredPoint(Vector3 basePosition, Vector3 nextDesiredPoint, BossExcavatorTargetPoint nextTargetPoint)
        {
            if (nextTargetPoint != _targetPoint)
            {
                return nextDesiredPoint;
            }

            if (IsImmediateTargetPoint(nextTargetPoint))
            {
                return nextDesiredPoint;
            }

            float desiredDelta = Vector3.Distance(nextDesiredPoint, _desiredPoint);

            if (desiredDelta > _config.DesiredPointDeadZone)
            {
                return nextDesiredPoint;
            }

            if (HasPathBlocked(basePosition, _desiredPoint))
            {
                return nextDesiredPoint;
            }

            return _desiredPoint;
        }

        private bool TryFindOpenPoint(Vector3 basePosition, Vector3 baseDirection, float distanceToPoint, Vector3 arenaCenterPosition, out Vector3 openPoint)
        {
            openPoint = basePosition;

            if (baseDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return false;
            }

            float safeDistance = Mathf.Max(distanceToPoint, _config.StopDistance + _config.ProbeRadius);
            int pointSearchIndex = 1;

            while (pointSearchIndex <= PointSearchCount)
            {
                float positiveAngle = PointSearchAngleStep * pointSearchIndex;
                Vector3 positivePoint = GetRotatedCandidatePoint(basePosition, baseDirection, safeDistance, positiveAngle, arenaCenterPosition);

                if (HasPathBlocked(basePosition, positivePoint) == false)
                {
                    openPoint = positivePoint;

                    return true;
                }

                float negativeAngle = -PointSearchAngleStep * pointSearchIndex;
                Vector3 negativePoint = GetRotatedCandidatePoint(basePosition, baseDirection, safeDistance, negativeAngle, arenaCenterPosition);

                if (HasPathBlocked(basePosition, negativePoint) == false)
                {
                    openPoint = negativePoint;

                    return true;
                }

                pointSearchIndex += 1;
            }

            return false;
        }

        private Vector3 GetRotatedCandidatePoint(Vector3 basePosition, Vector3 baseDirection, float distanceToPoint, float angle, Vector3 arenaCenterPosition)
        {
            Vector3 rotatedDirection = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection.normalized;
            Vector3 candidatePoint = basePosition + rotatedDirection * distanceToPoint;

            return ResolveSafePoint(candidatePoint, arenaCenterPosition);
        }

        private Vector3 PushPointFromWalls(Vector3 point, Vector3 arenaCenterPosition)
        {
            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            hitCount += TryAddWallNormal(point, Vector3.forward, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(point, Vector3.back, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(point, Vector3.left, _config.WallProbeDistance, ref normalSum);
            hitCount += TryAddWallNormal(point, Vector3.right, _config.WallProbeDistance, ref normalSum);

            if (hitCount <= 0)
            {
                return point;
            }

            Vector3 pushDirection = GetPlanarDirection(normalSum);
            Vector3 centerDirection = GetPlanarDirection(arenaCenterPosition - point);

            if (centerDirection.sqrMagnitude > MinSqrMagnitude)
            {
                pushDirection += centerDirection.normalized * _config.EscapeCenterWeight;
            }

            pushDirection = GetPlanarDirection(pushDirection);

            if (pushDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return point;
            }

            float pushDistance = _config.StopDistance + _config.ProbeRadius;

            return point + pushDirection.normalized * pushDistance;
        }

        private Vector3 ResolveSafePoint(Vector3 point, Vector3 arenaCenterPosition)
        {
            Vector3 safePoint = ClampRoomPoint(point);
            int pushIndex = 0;

            while (pushIndex < WallPushCount)
            {
                Vector3 nextPoint = PushPointFromWalls(safePoint, arenaCenterPosition);
                nextPoint = ClampRoomPoint(nextPoint);
                float pushDelta = Vector3.Distance(nextPoint, safePoint);
                safePoint = nextPoint;

                if (pushDelta <= MinPushDelta)
                {
                    return safePoint;
                }

                if (IsNearWall(safePoint) == false)
                {
                    return safePoint;
                }

                pushIndex += 1;
            }

            return safePoint;
        }

        private Vector3 ClampRoomPoint(Vector3 point)
        {
            ResolveRoomState();

            if (_roomRuntimeState == null)
            {
                return point;
            }

            Vector3 clampedPoint = _roomRuntimeState.ClampMovePoint(point);

            return GetPlanarPosition(clampedPoint);
        }

        private int TryAddWallNormal(Vector3 position, Vector3 direction, float probeDistance, ref Vector3 normalSum)
        {
            Vector3 rayOrigin = position + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, direction, out hit, probeDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return 0;
            }

            normalSum += hit.normal;

            return 1;
        }

        private float GetPointScore(Vector3 basePosition, Vector3 point)
        {
            float pointScore = Vector3.Distance(basePosition, point);

            if (IsNearWall(point))
            {
                pointScore += _config.WallPenalty;
            }

            if (IsNearCorner(point))
            {
                pointScore += _config.CornerPenalty;
            }

            if (HasPathBlocked(basePosition, point))
            {
                pointScore += _config.BlockedPenalty;
            }

            return pointScore;
        }

        private Vector3 GetLookPoint(Vector3 targetPosition, Vector3 desiredPoint, BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.ChargeAlign)
            {
                return targetPosition;
            }

            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                return targetPosition;
            }

            return desiredPoint;
        }

        private Quaternion RotateBase(Vector3 basePosition, Vector3 lookPoint)
        {
            Vector3 lookDirection = GetPlanarDirection(lookPoint - basePosition);
            Quaternion currentRotation = _baseRigidbody.rotation;

            if (lookDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return currentRotation;
            }

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            Quaternion nextRotation = Quaternion.RotateTowards(currentRotation, targetRotation, _config.BaseTurnSpeed * Time.fixedDeltaTime);

            _baseRigidbody.MoveRotation(nextRotation);

            return nextRotation;
        }

        private void MoveBase(Vector3 basePosition, Vector3 arenaCenterPosition, Quaternion nextRotation)
        {
            Vector3 direction = GetPlanarDirection(_desiredPoint - basePosition);

            if (direction.sqrMagnitude <= MinSqrMagnitude)
            {
                _isMoveAllowed = false;

                return;
            }

            float distanceToPoint = direction.magnitude;

            if (distanceToPoint <= _config.StopDistance)
            {
                _isMoveAllowed = false;

                return;
            }

            Vector3 forward = GetPlanarForward(nextRotation * Vector3.forward);
            float angle = Vector3.Angle(forward, direction.normalized);

            if (CanMoveByAngle(angle) == false)
            {
                return;
            }

            if (IsForwardBlocked(basePosition, forward))
            {
                _isMoveAllowed = false;
                int wallHitCount = GetWallHitCount(basePosition);

                if (wallHitCount > 1)
                {
                    _targetPoint = BossExcavatorTargetPoint.CornerEscape;
                    _desiredPoint = BuildCornerEscapePoint(basePosition, arenaCenterPosition);
                }
                else
                {
                    _targetPoint = BossExcavatorTargetPoint.WallEscape;
                    _desiredPoint = BuildWallEscapePoint(basePosition, arenaCenterPosition);
                }

                return;
            }

            float moveSpeedMultiplier = GetMoveSpeedMultiplier(angle);
            float moveDistance = Mathf.Min(_config.BaseMoveSpeed * moveSpeedMultiplier * Time.fixedDeltaTime, distanceToPoint);
            Vector3 nextPlanarPosition = basePosition + forward * moveDistance;
            Vector3 nextPosition = new Vector3(nextPlanarPosition.x, _baseRigidbody.position.y, nextPlanarPosition.z);

            _baseRigidbody.MovePosition(nextPosition);
        }

        private bool CanMoveByAngle(float angle)
        {
            if (_isMoveAllowed == false)
            {
                if (angle > _config.MoveStartAngle)
                {
                    return false;
                }

                _isMoveAllowed = true;

                return true;
            }

            if (angle > _config.MoveStopAngle)
            {
                _isMoveAllowed = false;

                return false;
            }

            return true;
        }

        private float GetMoveSpeedMultiplier(float angle)
        {
            float angleFactor = 1f - Mathf.Clamp01(angle / _config.MoveStartAngle);

            return Mathf.Lerp(MinMoveSpeedFactor, 1f, angleFactor);
        }

        private bool IsForwardBlocked(Vector3 basePosition, Vector3 forward)
        {
            Vector3 rayOrigin = basePosition + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            return Physics.SphereCast(rayOrigin, _config.ProbeRadius, forward, out hit, _config.ForwardProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool HasPathBlocked(Vector3 basePosition, Vector3 point)
        {
            Vector3 pathDirection = GetPlanarDirection(point - basePosition);

            if (pathDirection.sqrMagnitude <= MinSqrMagnitude)
            {
                return false;
            }

            float pathDistance = pathDirection.magnitude - _config.StopDistance;

            if (pathDistance <= 0f)
            {
                return false;
            }

            Vector3 rayOrigin = basePosition + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            return Physics.SphereCast(rayOrigin, _config.ProbeRadius, pathDirection.normalized, out hit, pathDistance, _obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private bool IsNearWall(Vector3 position)
        {
            return GetWallHitCount(position) > 0;
        }

        private bool IsNearCorner(Vector3 position)
        {
            return GetWallHitCount(position) > 1;
        }

        private int GetWallHitCount(Vector3 position)
        {
            int hitCount = 0;
            Vector3 rayOrigin = position + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.forward, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.back, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.left, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.right, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount += 1;
            }

            return hitCount;
        }

        private bool ShouldReturnToCenter(Vector3 basePosition, Vector3 arenaCenterPosition)
        {
            float distanceToCenter = Vector3.Distance(basePosition, arenaCenterPosition);

            if (distanceToCenter >= _config.ArenaReturnDistance)
            {
                return true;
            }

            return false;
        }

        private Vector3 GetPlanarPosition(Vector3 position)
        {
            position.y = 0f;

            return position;
        }

        private Vector3 GetPlanarDirection(Vector3 direction)
        {
            direction.y = 0f;

            return direction;
        }

        private Vector3 GetPlanarForward(Vector3 forward)
        {
            forward.y = 0f;

            if (forward.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private Vector3 GetArenaCenterPosition()
        {
            if (_hasRuntimeArenaCenter)
            {
                return _runtimeArenaCenter;
            }

            if (_arenaCenter != null)
            {
                return GetPlanarPosition(_arenaCenter.position);
            }

            ResolveRoomState();

            if (_roomRuntimeState != null)
            {
                Bounds roomBounds = _roomRuntimeState.GetRoomBounds();

                return GetPlanarPosition(roomBounds.center);
            }

            if (_hasFallbackArenaCenter == false)
            {
                if (_baseRigidbody != null)
                {
                    _fallbackArenaCenter = GetPlanarPosition(_baseRigidbody.position);
                }
                else
                {
                    _fallbackArenaCenter = GetPlanarPosition(transform.position);
                }

                _hasFallbackArenaCenter = true;
            }

            return _fallbackArenaCenter;
        }

        private void ResolveRoomState()
        {
            if (_roomRuntimeState != null)
            {
                return;
            }

            if (_base != null)
            {
                _roomRuntimeState = _base.GetComponentInParent<RoomRuntimeState>();

                if (_roomRuntimeState != null)
                {
                    return;
                }
            }

            RoomCombatLock roomCombatLock = null;

            if (_base != null)
            {
                roomCombatLock = _base.GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock == null)
            {
                roomCombatLock = GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock != null)
            {
                RoomRuntimeState roomRuntimeState = roomCombatLock.GetComponent<RoomRuntimeState>();

                if (roomRuntimeState != null)
                {
                    _roomRuntimeState = roomRuntimeState;

                    return;
                }
            }

            _roomRuntimeState = GetComponentInParent<RoomRuntimeState>();
        }

        private void ValidateDependencies()
        {
            ResolveRoomState();
        }
    }
}

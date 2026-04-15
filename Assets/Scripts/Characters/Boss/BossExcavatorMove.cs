using System;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorMove : MonoBehaviour
    {
        [SerializeField] private Transform _arenaCenter;
        [SerializeField] private LayerMask _obstacleMask;

        private BossExcavatorConfig _config;
        private Transform _base;
        private Transform _target;
        private BossExcavatorTargetPoint _targetPoint;
        private Vector3 _desiredPoint;
        private bool _isChargeAlign;

        public BossExcavatorTargetPoint TargetPoint => _targetPoint;

        public Vector3 DesiredPoint => _desiredPoint;

        private void Awake()
        {
            ValidateDependencies();
        }

        public void Setup(BossExcavatorConfig config, Transform baseTransform, Transform target)
        {
            if (config == null)
            {
                throw new InvalidOperationException(nameof(config));
            }

            if (baseTransform == null)
            {
                throw new InvalidOperationException(nameof(baseTransform));
            }

            _config = config;
            _base = baseTransform;
            _target = target;
            _targetPoint = BossExcavatorTargetPoint.ArenaCenter;
            _desiredPoint = _base.position;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetChargeAlign(bool isChargeAlign)
        {
            _isChargeAlign = isChargeAlign;
        }

        public void Tick()
        {
            if (_config == null)
            {
                throw new InvalidOperationException(nameof(_config));
            }

            if (_base == null)
            {
                throw new InvalidOperationException(nameof(_base));
            }

            if (_target == null)
            {
                return;
            }

            Vector3 basePosition = GetPlanarPosition(_base.position);
            Vector3 targetPosition = GetPlanarPosition(_target.position);
            Vector3 arenaCenterPosition = GetPlanarPosition(_arenaCenter.position);

            _targetPoint = SelectTargetPoint(basePosition, targetPosition, arenaCenterPosition);
            _desiredPoint = BuildTargetPoint(basePosition, targetPosition, arenaCenterPosition, _targetPoint);

            RotateBase(basePosition, _desiredPoint);
            MoveBase(basePosition, arenaCenterPosition);
        }

        private BossExcavatorTargetPoint SelectTargetPoint(Vector3 basePosition, Vector3 targetPosition, Vector3 arenaCenterPosition)
        {
            if (ShouldReturnToCenter(basePosition, arenaCenterPosition))
            {
                return BossExcavatorTargetPoint.ArenaCenter;
            }

            if (IsNearCorner(basePosition))
            {
                return BossExcavatorTargetPoint.WallEscape;
            }

            if (IsNearWall(basePosition))
            {
                return BossExcavatorTargetPoint.WallEscape;
            }

            if (_isChargeAlign)
            {
                return BossExcavatorTargetPoint.ChargeAlign;
            }

            float distanceToTarget = Vector3.Distance(basePosition, targetPosition);

            if (distanceToTarget < _config.MinMoveDistance)
            {
                return BossExcavatorTargetPoint.PlayerBack;
            }

            if (distanceToTarget > _config.MaxMoveDistance)
            {
                return SelectFlankPoint(basePosition, targetPosition);
            }

            return SelectFlankPoint(basePosition, targetPosition);
        }

        private BossExcavatorTargetPoint SelectFlankPoint(Vector3 basePosition, Vector3 targetPosition)
        {
            Vector3 leftPoint = BuildOrbitPoint(basePosition, targetPosition, -1f);
            Vector3 rightPoint = BuildOrbitPoint(basePosition, targetPosition, 1f);

            float leftScore = GetPointScore(basePosition, leftPoint);
            float rightScore = GetPointScore(basePosition, rightPoint);

            if (Mathf.Abs(leftScore - rightScore) <= _config.FlankSwitchThreshold)
            {
                if (_targetPoint == BossExcavatorTargetPoint.PlayerLeft)
                {
                    return BossExcavatorTargetPoint.PlayerLeft;
                }

                if (_targetPoint == BossExcavatorTargetPoint.PlayerRight)
                {
                    return BossExcavatorTargetPoint.PlayerRight;
                }
            }

            if (leftScore <= rightScore)
            {
                return BossExcavatorTargetPoint.PlayerLeft;
            }

            return BossExcavatorTargetPoint.PlayerRight;
        }

        private Vector3 BuildTargetPoint(Vector3 basePosition, Vector3 targetPosition, Vector3 arenaCenterPosition, BossExcavatorTargetPoint targetPoint)
        {
            if (targetPoint == BossExcavatorTargetPoint.PlayerCenter)
            {
                return targetPosition;
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
                return BuildBackPoint(basePosition, targetPosition, _config.MediumDistance);
            }

            if (targetPoint == BossExcavatorTargetPoint.WallEscape)
            {
                return BuildWallEscapePoint(basePosition, arenaCenterPosition);
            }

            if (targetPoint == BossExcavatorTargetPoint.ArenaCenter)
            {
                return arenaCenterPosition;
            }

            if (targetPoint == BossExcavatorTargetPoint.ChargeAlign)
            {
                return BuildBackPoint(basePosition, targetPosition, _config.ChargeAlignDistance);
            }

            return arenaCenterPosition;
        }

        private Vector3 BuildOrbitPoint(Vector3 basePosition, Vector3 targetPosition, float sideSign)
        {
            Vector3 fromTargetToBase = basePosition - targetPosition;

            if (fromTargetToBase.sqrMagnitude <= 0.0001f)
            {
                fromTargetToBase = -GetPlanarForward(_target.forward);
            }

            Vector3 baseDirection = fromTargetToBase.normalized;
            Quaternion rotation = Quaternion.AngleAxis(_config.FlankAngle * sideSign, Vector3.up);
            Vector3 orbitDirection = rotation * baseDirection;

            return targetPosition + orbitDirection * _config.MediumDistance;
        }

        private Vector3 BuildBackPoint(Vector3 basePosition, Vector3 targetPosition, float distance)
        {
            Vector3 fromTargetToBase = basePosition - targetPosition;

            if (fromTargetToBase.sqrMagnitude <= 0.0001f)
            {
                fromTargetToBase = -GetPlanarForward(_target.forward);
            }

            Vector3 direction = fromTargetToBase.normalized;

            return targetPosition + direction * distance;
        }

        private Vector3 BuildWallEscapePoint(Vector3 basePosition, Vector3 arenaCenterPosition)
        {
            Vector3 normalSum = Vector3.zero;
            int hitCount = 0;

            hitCount += TryAddWallNormal(basePosition, Vector3.forward, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.back, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.left, ref normalSum);
            hitCount += TryAddWallNormal(basePosition, Vector3.right, ref normalSum);

            if (hitCount > 0)
            {
                Vector3 escapeDirection = GetPlanarDirection(normalSum);

                if (escapeDirection.sqrMagnitude > 0.0001f)
                {
                    return basePosition + escapeDirection.normalized * _config.WallEscapeDistance;
                }
            }

            Vector3 centerDirection = GetPlanarDirection(arenaCenterPosition - basePosition);

            if (centerDirection.sqrMagnitude <= 0.0001f)
            {
                return arenaCenterPosition;
            }

            return basePosition + centerDirection.normalized * _config.WallEscapeDistance;
        }

        private int TryAddWallNormal(Vector3 basePosition, Vector3 direction, ref Vector3 normalSum)
        {
            Vector3 rayOrigin = basePosition + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, direction, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore) == false)
            {
                return 0;
            }

            normalSum += hit.normal;

            return 1;
        }

        private float GetPointScore(Vector3 basePosition, Vector3 point)
        {
            float distanceScore = Vector3.Distance(basePosition, point);
            float wallPenalty = 0f;

            if (IsNearWall(point))
            {
                wallPenalty += _config.WallPenalty;
            }

            if (IsNearCorner(point))
            {
                wallPenalty += _config.CornerPenalty;
            }

            return distanceScore + wallPenalty;
        }

        private void RotateBase(Vector3 basePosition, Vector3 desiredPoint)
        {
            Vector3 direction = GetPlanarDirection(desiredPoint - basePosition);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            _base.rotation = Quaternion.RotateTowards(_base.rotation, targetRotation, _config.BaseTurnSpeed * Time.deltaTime);
        }

        private void MoveBase(Vector3 basePosition, Vector3 arenaCenterPosition)
        {
            Vector3 direction = GetPlanarDirection(_desiredPoint - basePosition);

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float distanceToPoint = direction.magnitude;

            if (distanceToPoint <= _config.StopDistance)
            {
                return;
            }

            Vector3 forward = GetPlanarForward(_base.forward);
            float angle = Vector3.Angle(forward, direction.normalized);

            if (angle > _config.MoveStartAngle)
            {
                return;
            }

            if (IsForwardBlocked(basePosition))
            {
                _targetPoint = BossExcavatorTargetPoint.WallEscape;
                _desiredPoint = BuildWallEscapePoint(basePosition, arenaCenterPosition);

                return;
            }

            Vector3 nextPosition = _base.position + _base.forward * (_config.BaseMoveSpeed * Time.deltaTime);
            nextPosition.y = _base.position.y;
            _base.position = nextPosition;
        }

        private bool IsForwardBlocked(Vector3 basePosition)
        {
            Vector3 rayOrigin = basePosition + Vector3.up * _config.ProbeHeight;
            RaycastHit hit;

            return Physics.SphereCast(rayOrigin, _config.ProbeRadius, GetPlanarForward(_base.forward), out hit, _config.ForwardProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore);
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
                hitCount++;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.back, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount++;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.left, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount++;
            }

            if (Physics.SphereCast(rayOrigin, _config.ProbeRadius, Vector3.right, out hit, _config.WallProbeDistance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                hitCount++;
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

            if (forward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private void ValidateDependencies()
        {
            if (_arenaCenter == null)
            {
                throw new InvalidOperationException(nameof(_arenaCenter));
            }
        }
    }
}

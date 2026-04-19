using UnityEngine;
using UnityEngine.AI;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private bool ShouldReturnToCenter(Vector3 currentPoint, Vector3 arenaCenterPoint)
        {
            float distanceToCenter = Vector3.Distance(currentPoint, arenaCenterPoint);

            if (distanceToCenter >= GetArenaReturnDistance())
            {
                return true;
            }

            return false;
        }

        private float GetMediumDistance()
        {
            return _config.MediumDistance;
        }

        private float GetRetreatDistance()
        {
            return _config.RetreatDistance;
        }

        private float GetAttackChaseDistance()
        {
            return _config.AttackChaseDistance;
        }

        private float GetArenaReturnDistance()
        {
            return _config.ArenaReturnDistance;
        }

        private float GetChargeAlignDistance()
        {
            return _config.ChargeAlignDistance;
        }

        private float GetWallEscapeDistance()
        {
            return _config.WallEscapeDistance;
        }

        private float GetCornerEscapeDistance()
        {
            return _config.CornerEscapeDistance;
        }

        private float GetDistanceTolerance()
        {
            return _config.DistanceTolerance;
        }

        private float GetDistanceHysteresis()
        {
            return _config.DistanceHysteresis;
        }

        private float GetMinMoveDistance()
        {
            return Mathf.Max(0.1f, GetMediumDistance() - GetDistanceTolerance());
        }

        private float GetOrbitStepDistance()
        {
            return Mathf.Max(_config.ForwardProbeDistance, _config.StopDistance * 2f);
        }

        private Vector3 GetOrbitDirection(Vector3 targetDirection, float sideSign)
        {
            if (sideSign < 0f)
            {
                return GetPlanarDirection(Vector3.Cross(Vector3.up, targetDirection));
            }

            return GetPlanarDirection(Vector3.Cross(targetDirection, Vector3.up));
        }

        private Vector3 ClampRoomPoint(Vector3 point)
        {
            ResolveRoomState();

            if (_roomRuntimeState == null)
            {
                return GetPlanarPosition(point);
            }

            return GetPlanarPosition(_roomRuntimeState.ClampMovePoint(point));
        }

        private int GetAvoidPriority()
        {
            GameObject priorityGameObject = gameObject;

            if (_base != null)
            {
                priorityGameObject = _base.gameObject;
            }

            int priorityId = priorityGameObject.GetInstanceID();

            if (priorityId == int.MinValue)
            {
                priorityId = int.MaxValue;
            }

            if (priorityId < 0)
            {
                priorityId = -priorityId;
            }

            return 20 + (priorityId % 60);
        }

        private bool HasAnyNavMesh()
        {
            NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();

            if (navMeshTriangulation.vertices == null)
            {
                return false;
            }

            return navMeshTriangulation.vertices.Length > 0;
        }

        private Vector3 GetBasePoint()
        {
            return GetPlanarPosition(_baseRigidbody.position);
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

            return GetBasePoint();
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
                }
            }
        }

        private Vector3 GetPlanarPosition(Vector3 position)
        {
            position.y = GetMoveHeight();

            return position;
        }

        private float GetMoveHeight()
        {
            if (_baseRigidbody != null)
            {
                return _baseRigidbody.position.y;
            }

            if (_base != null)
            {
                return _base.position.y;
            }

            return transform.position.y;
        }

        private Vector3 GetPlanarDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= MinSqrMagnitude)
            {
                return Vector3.zero;
            }

            return direction.normalized;
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
    }
}

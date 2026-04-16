using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavatorMove
    {
        private void OnDrawGizmosSelected()
        {
            if (_config == null)
            {
                return;
            }

            Vector3 currentPoint = GetGizmoBasePoint();
            Vector3 arenaCenterPoint = GetGizmoArenaCenterPoint(currentPoint);

            DrawArenaGizmo(currentPoint, arenaCenterPoint);
            DrawBaseGizmo(currentPoint);

            if (_target == null)
            {
                return;
            }

            Vector3 targetPoint = GetPlanarPosition(_target.position);

            DrawTargetDistanceGizmo(targetPoint);
            DrawCandidatePointGizmo(currentPoint, targetPoint, arenaCenterPoint);
            DrawDesiredPointGizmo(currentPoint, targetPoint);
        }

        private void DrawArenaGizmo(Vector3 currentPoint, Vector3 arenaCenterPoint)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawLine(currentPoint, arenaCenterPoint);
            Gizmos.DrawWireSphere(arenaCenterPoint, GizmoPointSize);
            Gizmos.DrawWireSphere(arenaCenterPoint, GetArenaReturnDistance());
        }

        private void DrawBaseGizmo(Vector3 currentPoint)
        {
            Transform forwardTransform = transform;

            if (_base != null)
            {
                forwardTransform = _base;
            }

            Vector3 forwardPoint = currentPoint + (GetPlanarForward(forwardTransform.forward) * 2f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(currentPoint, forwardPoint);
            Gizmos.DrawWireSphere(forwardPoint, GizmoSmallPointSize);
        }

        private void DrawTargetDistanceGizmo(Vector3 targetPoint)
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, _config.BucketMaxDistance);

            Gizmos.color = new Color(0.2f, 1f, 1f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetAttackChaseDistance());

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetMediumDistance());

            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetRetreatDistance());

            Gizmos.color = new Color(0.3f, 1f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(targetPoint, GetMinMoveDistance());
        }

        private void DrawCandidatePointGizmo(Vector3 currentPoint, Vector3 targetPoint, Vector3 arenaCenterPoint)
        {
            Vector3 centerPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
            Vector3 leftPoint = BuildOrbitPoint(currentPoint, targetPoint, -1f);
            Vector3 rightPoint = BuildOrbitPoint(currentPoint, targetPoint, 1f);
            Vector3 backPoint = BuildCenterPoint(currentPoint, targetPoint, GetRetreatDistance());
            Vector3 chargePoint = BuildCenterPoint(currentPoint, targetPoint, GetChargeAlignDistance());

            DrawPointGizmo(targetPoint, centerPoint, Color.white, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, leftPoint, Color.yellow, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, rightPoint, Color.yellow, GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, backPoint, new Color(1f, 0.45f, 0.2f, 0.95f), GizmoSmallPointSize);
            DrawPointGizmo(targetPoint, chargePoint, Color.red, GizmoSmallPointSize);

            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.5f);
            Gizmos.DrawLine(arenaCenterPoint, currentPoint);
        }

        private void DrawDesiredPointGizmo(Vector3 currentPoint, Vector3 targetPoint)
        {
            Vector3 desiredPoint = _desiredPoint;

            if (Application.isPlaying == false)
            {
                desiredPoint = BuildCenterPoint(currentPoint, targetPoint, GetMediumDistance());
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(currentPoint, desiredPoint);
            Gizmos.DrawWireSphere(desiredPoint, GizmoPointSize);
        }

        private void DrawPointGizmo(Vector3 fromPoint, Vector3 toPoint, Color color, float radius)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(fromPoint, toPoint);
            Gizmos.DrawWireSphere(toPoint, radius);
        }

        private Vector3 GetGizmoBasePoint()
        {
            if (_baseRigidbody != null)
            {
                return GetPlanarPosition(_baseRigidbody.position);
            }

            if (_base != null)
            {
                return GetPlanarPosition(_base.position);
            }

            return GetPlanarPosition(transform.position);
        }

        private Vector3 GetGizmoArenaCenterPoint(Vector3 currentPoint)
        {
            if (Application.isPlaying)
            {
                return GetArenaCenterPosition();
            }

            if (_arenaCenter != null)
            {
                return GetPlanarPosition(_arenaCenter.position);
            }

            return currentPoint;
        }
    }
}

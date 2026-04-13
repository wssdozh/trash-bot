using UnityEngine;

public sealed partial class EnemyMeleeBrain
{
    private void OnDrawGizmos()
    {
        if (_isMoveGizmoVisible == false)
        {
            return;
        }

        DrawMoveGizmo();
        DrawStateGizmo();
        DrawTargetGizmo();
    }

    private void DrawMoveGizmo()
    {
        Vector3 currentPoint = transform.position;
        Vector3 forwardPoint = currentPoint + (GetStartDirection() * ForwardGizmoLength);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentPoint, forwardPoint);
        Gizmos.DrawWireSphere(forwardPoint, PointGizmoSize);

        if (_enemyMove == null)
        {
            return;
        }

        if (_enemyMove.MoveAmount <= 0f)
        {
            return;
        }

        Vector3 movePoint = currentPoint + (_enemyMove.MoveDirection * MoveGizmoLength);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(currentPoint, movePoint);
        Gizmos.DrawWireSphere(movePoint, PointGizmoSize);
    }

    private void DrawStateGizmo()
    {
        if (_isIdleWalking)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _idleTargetPoint);
            Gizmos.DrawWireSphere(_idleTargetPoint, _idleReachDistance);
        }

        else if (_hasLastSeenPoint == false)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _idleLookPoint);
            Gizmos.DrawWireSphere(_idleLookPoint, 0.15f);
        }

        if (_hasLastSeenPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_lastSeenPoint, _searchPointDistance);
        }

        if (_hasSearchPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, _searchTargetPoint);
            Gizmos.DrawWireSphere(_searchTargetPoint, _searchPointDistance);
        }
    }

    private void DrawTargetGizmo()
    {
        if (_targetVision == null)
        {
            return;
        }

        if (_targetVision.IsTargetVisible == false)
        {
            return;
        }

        if (_targetVision.CurrentTarget == null)
        {
            return;
        }

        Vector3 targetPoint = GetFlatPoint(_targetVision.CurrentTargetPoint);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, PointGizmoSize);
    }
}

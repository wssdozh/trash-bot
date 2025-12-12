using UnityEngine;

public sealed class StepAnimator
{
    private Transform _transform;
    private float _moveDirectionDeadZone;
    private float _runForwardMoveY;

    private Vector3 _previousPosition;
    private bool _hasPreviousPosition;

    public StepAnimator(Transform transform, float moveDirectionDeadZone, float runForwardMoveY)
    {
        _transform = transform;
        _moveDirectionDeadZone = moveDirectionDeadZone;
        _runForwardMoveY = runForwardMoveY;

        _hasPreviousPosition = false;
    }

    public Vector2 UpdateMoveFromMovement(bool isMoving, bool isSprinting)
    {
        if (_hasPreviousPosition == false)
        {
            _previousPosition = _transform.position;
            _hasPreviousPosition = true;
            return Vector2.zero;
        }

        Vector3 currentPosition = _transform.position;
        Vector3 deltaPosition = currentPosition - _previousPosition;
        _previousPosition = currentPosition;

        if (isMoving == false)
        {
            return Vector2.zero;
        }

        return GetMoveFromWorldDirection(deltaPosition, isSprinting);
    }

    public Vector2 GetMoveFromWorldDirection(Vector3 worldMoveDirection, bool isSprinting)
    {
        worldMoveDirection.y = 0f;

        float deadZoneSqr = _moveDirectionDeadZone * _moveDirectionDeadZone;

        if (worldMoveDirection.sqrMagnitude <= deadZoneSqr)
        {
            return Vector2.zero;
        }

        worldMoveDirection.Normalize();

        Vector3 forward = _transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = _transform.right;
        right.y = 0f;
        right.Normalize();

        float rawMoveY = Mathf.Clamp(Vector3.Dot(worldMoveDirection, forward), -1f, 1f);
        float rawMoveX = Mathf.Clamp(Vector3.Dot(worldMoveDirection, right), -1f, 1f);

        Vector2 snappedMove = ApplyAxisSnapping(rawMoveX, rawMoveY);

        if (isSprinting == true && snappedMove.y > 0f)
        {
            snappedMove.y = _runForwardMoveY;
        }

        return snappedMove;
    }

    private Vector2 ApplyAxisSnapping(float rawMoveX, float rawMoveY)
    {
        float absMoveX = Mathf.Abs(rawMoveX);
        float absMoveY = Mathf.Abs(rawMoveY);

        if (absMoveY >= absMoveX)
        {
            return new Vector2(0f, rawMoveY);
        }

        return new Vector2(rawMoveX, 0f);
    }
}

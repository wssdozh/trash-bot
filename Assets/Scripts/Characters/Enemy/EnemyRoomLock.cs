using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomLock : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private EnemyMove _enemyMove;
    private RoomRuntimeState _roomRuntimeState;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _enemyMove = GetComponent<EnemyMove>();

        if (_enemyMove == null)
        {
            _enemyMove = GetComponentInChildren<EnemyMove>();
        }
    }

    private void FixedUpdate()
    {
        if (_roomRuntimeState == null)
        {
            return;
        }

        if (_roomRuntimeState.ContainsRoomPoint(transform.position))
        {
            return;
        }

        SnapInside();
    }

    public void Setup(RoomRuntimeState roomRuntimeState)
    {
        if (roomRuntimeState == null)
        {
            return;
        }

        _roomRuntimeState = roomRuntimeState;
    }

    public bool ContainsRoomPoint(Vector3 point)
    {
        if (_roomRuntimeState == null)
        {
            return true;
        }

        return _roomRuntimeState.ContainsRoomPoint(point);
    }

    public bool ContainsMovePoint(Vector3 point)
    {
        if (_roomRuntimeState == null)
        {
            return true;
        }

        return _roomRuntimeState.ContainsMovePoint(point);
    }

    public Vector3 ClampMovePoint(Vector3 point)
    {
        if (_roomRuntimeState == null)
        {
            return point;
        }

        return _roomRuntimeState.ClampMovePoint(point);
    }

    public void SnapInside()
    {
        if (_roomRuntimeState == null)
        {
            return;
        }

        Vector3 clampedPoint = _roomRuntimeState.ClampSnapPoint(transform.position);

        if (_enemyMove != null)
        {
            _enemyMove.ForceStop();
        }

        ApplyPoint(clampedPoint);
    }

    private void ApplyPoint(Vector3 point)
    {
        if (_rigidbody != null)
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            _rigidbody.position = point;
            _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);

            return;
        }

        transform.position = point;
    }
}

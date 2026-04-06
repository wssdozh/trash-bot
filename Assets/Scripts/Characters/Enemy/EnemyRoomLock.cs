using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomLock : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private EnemyMove _enemyMove;
    private EnemyDroneMove _enemyDroneMove;
    private EnemyRoomAlert _enemyRoomAlert;
    private RoomRuntimeState _roomRuntimeState;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _enemyMove = GetComponent<EnemyMove>();
        _enemyDroneMove = GetComponent<EnemyDroneMove>();

        if (_enemyMove == null)
        {
            _enemyMove = GetComponentInChildren<EnemyMove>();
        }

        if (_enemyDroneMove == null)
        {
            _enemyDroneMove = GetComponentInChildren<EnemyDroneMove>();
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
        _enemyRoomAlert = roomRuntimeState.GetComponent<EnemyRoomAlert>();
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

    public float GetMoveTop()
    {
        if (_roomRuntimeState == null)
        {
            return transform.position.y;
        }

        return _roomRuntimeState.GetMoveTop();
    }

    public float GetMoveBottom()
    {
        if (_roomRuntimeState == null)
        {
            return transform.position.y;
        }

        return _roomRuntimeState.GetMoveBottom();
    }

    public Vector3 GetPatrolPoint(float height, System.Random random)
    {
        if (_roomRuntimeState == null)
        {
            return new Vector3(transform.position.x, height, transform.position.z);
        }

        return _roomRuntimeState.GetRandomMovePoint(height, random);
    }

    public void AlertPoint(Vector3 point, MonoBehaviour sender)
    {
        EnemyRoomAlert enemyRoomAlert = GetRoomAlert();

        if (enemyRoomAlert == null)
        {
            return;
        }

        enemyRoomAlert.AlertPoint(point, sender);
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

        if (_enemyDroneMove != null)
        {
            _enemyDroneMove.ForceStop();
        }

        ApplyPoint(clampedPoint);
    }

    private void ApplyPoint(Vector3 point)
    {
        if (_rigidbody != null)
        {
            _rigidbody.position = point;

            if (_rigidbody.isKinematic == false)
            {
                Vector3 currentVelocity = _rigidbody.linearVelocity;
                _rigidbody.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            }

            return;
        }

        transform.position = point;
    }

    private EnemyRoomAlert GetRoomAlert()
    {
        if (_enemyRoomAlert != null)
        {
            return _enemyRoomAlert;
        }

        _enemyRoomAlert = GetComponentInParent<EnemyRoomAlert>();

        return _enemyRoomAlert;
    }
}

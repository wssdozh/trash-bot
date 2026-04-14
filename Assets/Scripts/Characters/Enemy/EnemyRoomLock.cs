using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomLock : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private EnemyMove _enemyMove;
    private EnemyDroneMove _enemyDroneMove;
    private EnemyRoomAlert _enemyRoomAlert;
    private RoomRuntimeState _roomRuntimeState;
    private bool _useGroundPatrol;

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

        _useGroundPatrol = _enemyDroneMove == null;
        enabled = false;
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
        enabled = true;
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

    public int GetPatrolCount()
    {
        if (_roomRuntimeState == null)
        {
            return 0;
        }

        if (_useGroundPatrol)
        {
            return _roomRuntimeState.GetGroundPatrolCount();
        }

        return _roomRuntimeState.GetPatrolCount();
    }

    public int GetNearestPatrolIndex(Vector3 point)
    {
        if (_roomRuntimeState == null)
        {
            return 0;
        }

        if (_useGroundPatrol)
        {
            return _roomRuntimeState.GetNearestGroundPatrolIndex(point);
        }

        return _roomRuntimeState.GetNearestPatrolIndex(point);
    }

    public int GetNextPatrolIndex(int patrolIndex, int patrolDirection)
    {
        int patrolCount = GetPatrolCount();

        if (patrolCount <= 0)
        {
            return 0;
        }

        if (patrolDirection == 0)
        {
            patrolDirection = 1;
        }

        int nextIndex = patrolIndex + patrolDirection;
        int loopIndex = nextIndex % patrolCount;

        if (loopIndex < 0)
        {
            loopIndex += patrolCount;
        }

        return loopIndex;
    }

    public Vector3 GetPatrolPoint(int patrolIndex, float height)
    {
        if (_roomRuntimeState == null)
        {
            return new Vector3(transform.position.x, height, transform.position.z);
        }

        if (_useGroundPatrol)
        {
            return _roomRuntimeState.GetGroundPatrolPoint(patrolIndex, height);
        }

        return _roomRuntimeState.GetPatrolPoint(patrolIndex, height);
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

    public void AlertPoint(Vector3 point, MonoBehaviour sender, int maxCount, System.Random random)
    {
        EnemyRoomAlert enemyRoomAlert = GetRoomAlert();

        if (enemyRoomAlert == null)
        {
            return;
        }

        enemyRoomAlert.AlertPoint(point, sender, maxCount, random);
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

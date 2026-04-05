using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyDroneMove : MonoBehaviour
{
    private const int AvoidDirectionCount = 4;
    private const float ZeroThreshold = 0.0001f;
    private const float ProbeSkin = 0.05f;

    [Header("Dependencies")]
    [SerializeField] private Transform _moveRoot;
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 1.25f;
    [SerializeField] private float _heightSpeed = 1.85f;
    [SerializeField] private float _turnSpeed = 180f;
    [SerializeField] private float _moveGainSpeed = 1.2f;
    [SerializeField] private float _moveDropSpeed = 1.9f;
    [SerializeField] private float _moveReachDistance = 0.22f;
    [SerializeField] private float _slowDistance = 1.1f;
    [SerializeField] private float _verticalSmoothTime = 0.2f;

    [Header("Hover")]
    [SerializeField] private float _hoverAmplitude = 0.08f;
    [SerializeField] private float _hoverSpeed = 1.25f;

    [Header("Flight")]
    [SerializeField] private float _ceilingGap = 1.2f;
    [SerializeField] private float _floorGap = 2.4f;

    [Header("Obstacle")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _probeRadius = 0.3f;
    [SerializeField] private float _probeDistance = 1.2f;
    [SerializeField] private float _avoidAngleStep = 35f;
    [SerializeField] private float _liftMaxHeight = 1.75f;
    [SerializeField] private float _liftStepHeight = 0.35f;
    [SerializeField] private float _liftGainSpeed = 1.35f;
    [SerializeField] private float _liftDropSpeed = 1.85f;

    private Vector3 _anchorPoint;
    private Vector3 _moveDirection;
    private Vector3 _movePoint;
    private EnemyRoomLock _enemyRoomLock;
    private bool _hasMovePoint;
    private float _currentMoveSpeed;
    private float _currentLiftHeight;
    private float _hoverTime;
    private float _verticalVelocity;

    public Vector3 MoveVelocity => _moveDirection * _currentMoveSpeed;
    public Vector3 ForwardDirection => GetForwardDirection();

    private void Awake()
    {
        if (_moveRoot == null)
        {
            _moveRoot = transform;
        }

        if (_rigidbody == null)
        {
            throw new InvalidOperationException(nameof(_rigidbody));
        }

        if (_moveSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveSpeed));
        }

        if (_heightSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_heightSpeed));
        }

        if (_turnSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_turnSpeed));
        }

        if (_moveGainSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveGainSpeed));
        }

        if (_moveDropSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveDropSpeed));
        }

        if (_moveReachDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_moveReachDistance));
        }

        if (_slowDistance < _moveReachDistance)
        {
            throw new InvalidOperationException(nameof(_slowDistance));
        }

        if (_verticalSmoothTime <= 0f)
        {
            throw new InvalidOperationException(nameof(_verticalSmoothTime));
        }

        if (_hoverAmplitude < 0f)
        {
            throw new InvalidOperationException(nameof(_hoverAmplitude));
        }

        if (_hoverSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_hoverSpeed));
        }

        if (_ceilingGap < 0f)
        {
            throw new InvalidOperationException(nameof(_ceilingGap));
        }

        if (_floorGap < 0f)
        {
            throw new InvalidOperationException(nameof(_floorGap));
        }

        if (_probeRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeRadius));
        }

        if (_probeDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_probeDistance));
        }

        if (_avoidAngleStep <= 0f)
        {
            throw new InvalidOperationException(nameof(_avoidAngleStep));
        }

        if (_liftMaxHeight < 0f)
        {
            throw new InvalidOperationException(nameof(_liftMaxHeight));
        }

        if (_liftStepHeight <= 0f)
        {
            throw new InvalidOperationException(nameof(_liftStepHeight));
        }

        if (_liftGainSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_liftGainSpeed));
        }

        if (_liftDropSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_liftDropSpeed));
        }

        ApplyFlightBody();
        _movePoint = _moveRoot.position;
        _hoverTime = GetInstanceID() * 0.01f;
    }

    private void Start()
    {
        ResetAnchor();
    }

    private void FixedUpdate()
    {
        _hoverTime += Time.fixedDeltaTime * _hoverSpeed;

        Vector3 currentPoint = _rigidbody.position;
        Vector3 targetPoint = GetTargetPoint(currentPoint);
        Vector3 nextPoint = GetNextPoint(currentPoint, targetPoint);
        _rigidbody.MovePosition(ClampPoint(nextPoint));
    }

    public void ResetAnchor()
    {
        ApplyFlightBody();
        _anchorPoint = _rigidbody.position;
        UpdateAnchorHeight(_anchorPoint.y);
        _rigidbody.position = new Vector3(_rigidbody.position.x, _anchorPoint.y, _rigidbody.position.z);
        _moveDirection = Vector3.zero;
        _movePoint = _anchorPoint;
        _hasMovePoint = false;
        _currentMoveSpeed = 0f;
        _currentLiftHeight = 0f;
        _verticalVelocity = 0f;
        enabled = true;
    }

    public Vector3 GetAnchorPoint()
    {
        return _anchorPoint;
    }

    public void SetMovePoint(Vector3 movePoint)
    {
        UpdateAnchorHeight(_rigidbody.position.y);
        _movePoint = ClampPoint(movePoint);
        _movePoint.y = _anchorPoint.y;
        _hasMovePoint = true;
    }

    public void StopMove()
    {
        _hasMovePoint = false;
    }

    public void ForceStop()
    {
        _hasMovePoint = false;
        _moveDirection = Vector3.zero;
        _movePoint = _rigidbody.position;
        _currentMoveSpeed = 0f;
        _currentLiftHeight = 0f;
        _verticalVelocity = 0f;
    }

    private Vector3 GetTargetPoint(Vector3 currentPoint)
    {
        UpdateAnchorHeight(currentPoint.y);

        Vector3 targetPoint = currentPoint;
        float requestedLiftHeight = 0f;

        if (_hasMovePoint)
        {
            targetPoint = _movePoint;

            Vector3 flatOffset = _movePoint - currentPoint;
            flatOffset.y = 0f;

            if (flatOffset.sqrMagnitude > ZeroThreshold)
            {
                Vector3 moveDirection = flatOffset.normalized;
                requestedLiftHeight = GetLiftHeight(currentPoint, moveDirection);
            }
        }

        UpdateLiftHeight(requestedLiftHeight);

        targetPoint.y = _anchorPoint.y + _currentLiftHeight + (Mathf.Sin(_hoverTime) * _hoverAmplitude);

        return ClampPoint(targetPoint);
    }

    private void ApplyFlightBody()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.constraints = RigidbodyConstraints.None;
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
        _rigidbody.position = _moveRoot.position;
    }

    private Vector3 GetForwardDirection()
    {
        Vector3 forwardDirection = _moveRoot.forward;
        forwardDirection.y = 0f;

        if (forwardDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.forward;
        }

        forwardDirection.Normalize();

        return forwardDirection;
    }

    private Vector3 GetNextPoint(Vector3 currentPoint, Vector3 targetPoint)
    {
        Vector3 nextPoint = currentPoint;
        Vector3 moveOffset = targetPoint - currentPoint;
        moveOffset.y = 0f;
        float remainingDistance = moveOffset.magnitude;
        Vector3 desiredDirection = Vector3.zero;
        float desiredSpeed = 0f;

        if (_hasMovePoint && remainingDistance > _moveReachDistance)
        {
            desiredDirection = moveOffset / remainingDistance;

            if (targetPoint.y <= currentPoint.y + (_liftStepHeight * 0.5f))
            {
                desiredDirection = ResolveDirection(currentPoint, desiredDirection);
            }

            desiredSpeed = GetDesiredSpeed(remainingDistance);
        }

        Vector3 currentDirection = UpdateMoveDirection(desiredDirection);
        UpdateMoveSpeed(desiredSpeed);
        RotateMoveRoot(currentDirection);

        if (_currentMoveSpeed > ZeroThreshold)
        {
            float moveDistance = _currentMoveSpeed * Time.fixedDeltaTime;

            if (IsBlocked(currentPoint, currentDirection, moveDistance))
            {
                _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, 0f, _moveDropSpeed * Time.fixedDeltaTime);
                moveDistance = _currentMoveSpeed * Time.fixedDeltaTime;
            }

            if (_currentMoveSpeed > ZeroThreshold && IsBlocked(currentPoint, currentDirection, moveDistance) == false)
            {
                nextPoint += currentDirection * moveDistance;
            }
        }

        nextPoint.y = Mathf.SmoothDamp(
            currentPoint.y,
            targetPoint.y,
            ref _verticalVelocity,
            _verticalSmoothTime,
            _heightSpeed,
            Time.fixedDeltaTime);

        return nextPoint;
    }

    private void RotateMoveRoot(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        float rotationStep = _turnSpeed * Time.fixedDeltaTime;
        Quaternion nextRotation = Quaternion.RotateTowards(
            _moveRoot.rotation,
            targetRotation,
            rotationStep);

        if (_moveRoot == transform)
        {
            _rigidbody.MoveRotation(nextRotation);

            return;
        }

        _moveRoot.rotation = nextRotation;
    }

    private float GetDesiredSpeed(float remainingDistance)
    {
        if (remainingDistance <= _moveReachDistance)
        {
            return 0f;
        }

        float speedScale = Mathf.InverseLerp(_moveReachDistance, _slowDistance, remainingDistance);
        float minimumSpeed = _moveSpeed * 0.35f;

        return Mathf.Lerp(minimumSpeed, _moveSpeed, speedScale);
    }

    private float GetLiftHeight(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (_liftMaxHeight <= ZeroThreshold)
        {
            return 0f;
        }

        if (IsBlocked(currentPoint, moveDirection, _probeDistance) == false)
        {
            return 0f;
        }

        float liftHeight = _liftStepHeight;

        while (liftHeight <= _liftMaxHeight + ZeroThreshold)
        {
            if (CanFlyAtHeight(currentPoint, moveDirection, liftHeight))
            {
                return liftHeight;
            }

            liftHeight += _liftStepHeight;
        }

        return 0f;
    }

    private void UpdateLiftHeight(float requestedLiftHeight)
    {
        float liftStep = _liftDropSpeed;

        if (requestedLiftHeight > _currentLiftHeight)
        {
            liftStep = _liftGainSpeed;
        }

        _currentLiftHeight = Mathf.MoveTowards(_currentLiftHeight, requestedLiftHeight, liftStep * Time.fixedDeltaTime);
    }

    private void UpdateAnchorHeight(float fallbackHeight)
    {
        float flightHeight = GetFlightHeight(fallbackHeight);

        if (Mathf.Abs(_anchorPoint.y - flightHeight) <= ZeroThreshold)
        {
            return;
        }

        _anchorPoint.y = flightHeight;
    }

    private float GetFlightHeight(float fallbackHeight)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            return Mathf.Max(fallbackHeight, _floorGap);
        }

        float moveTop = enemyRoomLock.GetMoveTop();
        float moveBottom = enemyRoomLock.GetMoveBottom();
        float roomHeight = moveTop - moveBottom;
        float maxHeight = moveTop - _ceilingGap;
        float minHeight = moveBottom + _floorGap;

        if (roomHeight <= _ceilingGap + _floorGap + ZeroThreshold)
        {
            return Mathf.Max(fallbackHeight, _floorGap);
        }

        if (maxHeight < minHeight)
        {
            return Mathf.Max(fallbackHeight, minHeight);
        }

        return maxHeight;
    }

    private bool CanFlyAtHeight(Vector3 currentPoint, Vector3 moveDirection, float liftHeight)
    {
        if (liftHeight <= ZeroThreshold)
        {
            return true;
        }

        Vector3 liftDirection = Vector3.up;

        if (IsBlocked(currentPoint, liftDirection, liftHeight))
        {
            return false;
        }

        Vector3 liftedPoint = currentPoint + (Vector3.up * liftHeight);

        if (IsBlocked(liftedPoint, moveDirection, _probeDistance))
        {
            return false;
        }

        return true;
    }

    private Vector3 UpdateMoveDirection(Vector3 desiredDirection)
    {
        if (desiredDirection.sqrMagnitude <= ZeroThreshold)
        {
            if (_moveDirection.sqrMagnitude <= ZeroThreshold)
            {
                return Vector3.zero;
            }

            return _moveDirection;
        }

        if (_moveDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = desiredDirection;

            return _moveDirection;
        }

        float maxRadiansDelta = _turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 nextDirection = Vector3.RotateTowards(_moveDirection, desiredDirection, maxRadiansDelta, 0f);
        nextDirection.y = 0f;

        if (nextDirection.sqrMagnitude <= ZeroThreshold)
        {
            _moveDirection = Vector3.zero;

            return _moveDirection;
        }

        nextDirection.Normalize();
        _moveDirection = nextDirection;

        return _moveDirection;
    }

    private void UpdateMoveSpeed(float desiredSpeed)
    {
        float speedStep = _moveDropSpeed;

        if (desiredSpeed > _currentMoveSpeed)
        {
            speedStep = _moveGainSpeed;
        }

        _currentMoveSpeed = Mathf.MoveTowards(_currentMoveSpeed, desiredSpeed, speedStep * Time.fixedDeltaTime);

        if (_currentMoveSpeed <= ZeroThreshold)
        {
            _currentMoveSpeed = 0f;
        }
    }

    private Vector3 ResolveDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        if (IsBlocked(currentPoint, moveDirection, _probeDistance) == false)
        {
            return moveDirection;
        }

        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue;
        int directionIndex = 1;

        while (directionIndex <= AvoidDirectionCount)
        {
            float positiveAngle = _avoidAngleStep * directionIndex;
            EvaluateCandidate(currentPoint, moveDirection, positiveAngle, ref bestDirection, ref bestScore);

            float negativeAngle = -positiveAngle;
            EvaluateCandidate(currentPoint, moveDirection, negativeAngle, ref bestDirection, ref bestScore);

            directionIndex += 1;
        }

        if (bestDirection.sqrMagnitude > ZeroThreshold)
        {
            return bestDirection;
        }

        return GetSlideDirection(currentPoint, moveDirection);
    }

    private void EvaluateCandidate(
        Vector3 currentPoint,
        Vector3 moveDirection,
        float angle,
        ref Vector3 bestDirection,
        ref float bestScore)
    {
        Vector3 candidateDirection = Quaternion.Euler(0f, angle, 0f) * moveDirection;
        candidateDirection.y = 0f;

        if (candidateDirection.sqrMagnitude <= ZeroThreshold)
        {
            return;
        }

        candidateDirection.Normalize();

        float clearDistance = GetClearDistance(currentPoint, candidateDirection);
        float distanceScore = clearDistance / _probeDistance;
        float directionScore = Vector3.Dot(moveDirection, candidateDirection);
        float candidateScore = (distanceScore * 1.5f) + directionScore;

        if (candidateScore <= bestScore)
        {
            return;
        }

        bestScore = candidateScore;
        bestDirection = candidateDirection;
    }

    private float GetClearDistance(Vector3 currentPoint, Vector3 moveDirection)
    {
        RaycastHit obstacleHit;
        bool hasObstacle = Physics.SphereCast(
            currentPoint,
            _probeRadius,
            moveDirection,
            out obstacleHit,
            _probeDistance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hasObstacle == false)
        {
            return _probeDistance;
        }

        return obstacleHit.distance;
    }

    private Vector3 GetSlideDirection(Vector3 currentPoint, Vector3 moveDirection)
    {
        RaycastHit obstacleHit;
        bool hasObstacle = Physics.SphereCast(
            currentPoint,
            _probeRadius,
            moveDirection,
            out obstacleHit,
            _probeDistance,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);

        if (hasObstacle == false)
        {
            return moveDirection;
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, obstacleHit.normal);
        slideDirection.y = 0f;

        if (slideDirection.sqrMagnitude <= ZeroThreshold)
        {
            return Vector3.zero;
        }

        slideDirection.Normalize();

        return slideDirection;
    }

    private bool IsBlocked(Vector3 currentPoint, Vector3 moveDirection, float moveDistance)
    {
        if (moveDistance <= ZeroThreshold)
        {
            return false;
        }

        RaycastHit obstacleHit;

        return Physics.SphereCast(
            currentPoint,
            _probeRadius,
            moveDirection,
            out obstacleHit,
            moveDistance + ProbeSkin,
            _obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    private Vector3 ClampPoint(Vector3 point)
    {
        EnemyRoomLock enemyRoomLock = GetRoomLock();

        if (enemyRoomLock == null)
        {
            return point;
        }

        return enemyRoomLock.ClampMovePoint(point);
    }

    private EnemyRoomLock GetRoomLock()
    {
        if (_enemyRoomLock != null)
        {
            return _enemyRoomLock;
        }

        _enemyRoomLock = GetComponent<EnemyRoomLock>();

        return _enemyRoomLock;
    }
}

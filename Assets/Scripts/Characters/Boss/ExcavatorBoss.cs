using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExcavatorBoss : MonoBehaviour
{
    private const int HitBufferSize = 16;
    private const int StateIdle = 0;
    private const int StateSlamWindup = 1;
    private const int StateSlamRecover = 2;
    private const int StateThrowWindup = 3;
    private const int StateThrowRecover = 4;
    private const float DirectionThreshold = 0.0001f;

    private readonly Collider[] _hitBuffer = new Collider[HitBufferSize];

    [Header("Dependencies")]
    [SerializeField] private Health _health;
    [SerializeField] private ExcavatorBossVisual _visual;
    [SerializeField] private BoxCollider _bodyCollider;

    [Header("Stats")]
    [SerializeField, Min(1f)] private float _maxHealth = 320f;
    [SerializeField, Min(0f)] private float _viewDistance = 18f;
    [SerializeField] private string _targetTag = "Player";
    [SerializeField] private bool _startActive;

    [Header("Move")]
    [SerializeField, Min(0f)] private float _moveSpeed = 1.6f;
    [SerializeField, Min(0f)] private float _turnSpeed = 110f;
    [SerializeField, Min(0f)] private float _cabinTurnSpeed = 210f;
    [SerializeField, Min(0f)] private float _moveRadius = 2.4f;
    [SerializeField, Min(0f)] private float _standDistance = 8f;
    [SerializeField, Min(0f)] private float _anchorRadius = 8f;
    [SerializeField, Min(0f)] private float _moveDelay = 2.6f;
    [SerializeField, Min(0f)] private float _moveReachDistance = 0.12f;

    [Header("Slam")]
    [SerializeField, Min(0f)] private float _slamDamage = 22f;
    [SerializeField, Min(0f)] private float _slamStartDistance = 4.5f;
    [SerializeField, Min(0f)] private float _slamRange = 3.3f;
    [SerializeField, Min(0f)] private float _slamWidth = 2.6f;
    [SerializeField, Min(0f)] private float _slamWindup = 0.85f;
    [SerializeField, Min(0f)] private float _slamRecover = 1f;
    [SerializeField, Min(0f)] private float _slamCooldown = 3.8f;

    [Header("Throw")]
    [SerializeField, Min(0f)] private float _throwDamage = 10f;
    [SerializeField, Min(1)] private int _throwCount = 5;
    [SerializeField, Min(0f)] private float _throwStartDistance = 12f;
    [SerializeField, Min(0f)] private float _throwSpeed = 8f;
    [SerializeField, Min(0f)] private float _throwLifeTime = 2.5f;
    [SerializeField, Min(0f)] private float _throwHitRadius = 0.28f;
    [SerializeField, Min(0f)] private float _throwScale = 0.42f;
    [SerializeField, Min(0f)] private float _throwSpinSpeed = 300f;
    [SerializeField, Min(0f)] private float _throwSpread = 34f;
    [SerializeField, Min(0f)] private float _throwWindup = 0.72f;
    [SerializeField, Min(0f)] private float _throwRecover = 0.55f;
    [SerializeField, Min(0f)] private float _throwCooldown = 3.2f;
    [SerializeField] private Color _throwColor = new Color(0.46f, 0.34f, 0.24f);

    [Header("Phase")]
    [SerializeField, Range(0.1f, 1f)] private float _phaseHealth01 = 0.5f;
    [SerializeField, Min(1f)] private float _phaseMoveScale = 1.2f;
    [SerializeField, Range(0.1f, 1f)] private float _phaseCooldownScale = 0.8f;
    [SerializeField, Min(0)] private int _phaseExtraThrowCount = 2;

    [Header("Death")]
    [SerializeField, Min(0f)] private float _sinkDelay = 2.4f;
    [SerializeField, Min(0.01f)] private float _sinkDuration = 4.5f;
    [SerializeField, Min(0f)] private float _sinkDistance = 2.5f;

    private Transform _targetTransform;
    private Health _targetHealth;
    private Vector3 _anchorPoint;
    private Vector3 _movePoint;
    private bool _hasMovePoint;
    private bool _isPhaseTwo;
    private bool _isDead;
    private bool _isCombatActive;
    private int _moveSide = 1;
    private int _state;
    private float _moveTimer;
    private float _slamTimer;
    private float _throwTimer;
    private float _stateTimer;
    private float _currentCabinYaw;
    private float _currentArmAngle;
    private float _currentForearmAngle;
    private float _currentBucketAngle;
    private int _attackStep;

    public event Action Died;

    public bool IsDead => _isDead;

    private void Awake()
    {
        ValidateDependencies();
        ValidateStats();
        ValidateMove();
        ValidateSlam();
        ValidateThrow();
        ValidatePhase();
        ValidateDeath();
        ConfigureHealth();
        ConfigureCollider();
        _anchorPoint = transform.position;
    }

    private void OnEnable()
    {
        _health.Ended += OnHealthEnded;
        ResetState();
    }

    private void OnDisable()
    {
        _health.Ended -= OnHealthEnded;
    }

    private void OnValidate()
    {
        if (_bodyCollider == null)
        {
            return;
        }

        ConfigureCollider();
    }

    private void Update()
    {
        if (_isDead)
        {
            return;
        }

        if (_isCombatActive == false)
        {
            UpdateIdlePose();

            return;
        }

        ResolveTarget();
        TickTimers();
        UpdatePhase();

        if (_targetTransform == null)
        {
            UpdateIdlePose();

            return;
        }

        Vector3 targetPoint = _targetTransform.position;

        if (GetFlatDistanceSqr(transform.position, targetPoint) > _viewDistance * _viewDistance)
        {
            UpdateIdlePose();

            return;
        }

        RotateCabin(targetPoint);

        if (_state == StateSlamWindup)
        {
            UpdateSlamWindup();

            return;
        }

        if (_state == StateSlamRecover)
        {
            UpdateSlamRecover();

            return;
        }

        if (_state == StateThrowWindup)
        {
            UpdateThrowWindup(targetPoint);

            return;
        }

        if (_state == StateThrowRecover)
        {
            UpdateThrowRecover();

            return;
        }

        if (TryStartAttack(targetPoint))
        {
            return;
        }

        UpdateMove(targetPoint);
        MoveArmTo(14f, -18f, 26f, 120f);
    }

    private void ValidateDependencies()
    {
        if (_health == null)
        {
            throw new InvalidOperationException(nameof(_health));
        }

        if (_visual == null)
        {
            throw new InvalidOperationException(nameof(_visual));
        }

        if (_bodyCollider == null)
        {
            throw new InvalidOperationException(nameof(_bodyCollider));
        }
    }

    private void ValidateStats()
    {
        if (_maxHealth <= 0f)
        {
            throw new InvalidOperationException(nameof(_maxHealth));
        }

        if (_viewDistance <= 0f)
        {
            throw new InvalidOperationException(nameof(_viewDistance));
        }

        if (string.IsNullOrWhiteSpace(_targetTag))
        {
            throw new InvalidOperationException(nameof(_targetTag));
        }
    }

    private void ValidateMove()
    {
        if (_moveSpeed < 0f)
        {
            throw new InvalidOperationException(nameof(_moveSpeed));
        }

        if (_turnSpeed < 0f)
        {
            throw new InvalidOperationException(nameof(_turnSpeed));
        }

        if (_cabinTurnSpeed < 0f)
        {
            throw new InvalidOperationException(nameof(_cabinTurnSpeed));
        }

        if (_moveRadius < 0f)
        {
            throw new InvalidOperationException(nameof(_moveRadius));
        }

        if (_standDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_standDistance));
        }

        if (_anchorRadius < 0f)
        {
            throw new InvalidOperationException(nameof(_anchorRadius));
        }

        if (_moveDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_moveDelay));
        }

        if (_moveReachDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_moveReachDistance));
        }
    }

    private void ValidateSlam()
    {
        if (_slamDamage < 0f)
        {
            throw new InvalidOperationException(nameof(_slamDamage));
        }

        if (_slamStartDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_slamStartDistance));
        }

        if (_slamRange < 0f)
        {
            throw new InvalidOperationException(nameof(_slamRange));
        }

        if (_slamWidth < 0f)
        {
            throw new InvalidOperationException(nameof(_slamWidth));
        }

        if (_slamWindup < 0f)
        {
            throw new InvalidOperationException(nameof(_slamWindup));
        }

        if (_slamRecover < 0f)
        {
            throw new InvalidOperationException(nameof(_slamRecover));
        }

        if (_slamCooldown < 0f)
        {
            throw new InvalidOperationException(nameof(_slamCooldown));
        }
    }

    private void ValidateThrow()
    {
        if (_throwDamage < 0f)
        {
            throw new InvalidOperationException(nameof(_throwDamage));
        }

        if (_throwCount <= 0)
        {
            throw new InvalidOperationException(nameof(_throwCount));
        }

        if (_throwStartDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_throwStartDistance));
        }

        if (_throwSpeed <= 0f)
        {
            throw new InvalidOperationException(nameof(_throwSpeed));
        }

        if (_throwLifeTime <= 0f)
        {
            throw new InvalidOperationException(nameof(_throwLifeTime));
        }

        if (_throwHitRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_throwHitRadius));
        }

        if (_throwScale <= 0f)
        {
            throw new InvalidOperationException(nameof(_throwScale));
        }

        if (_throwSpinSpeed < 0f)
        {
            throw new InvalidOperationException(nameof(_throwSpinSpeed));
        }

        if (_throwSpread < 0f)
        {
            throw new InvalidOperationException(nameof(_throwSpread));
        }

        if (_throwWindup < 0f)
        {
            throw new InvalidOperationException(nameof(_throwWindup));
        }

        if (_throwRecover < 0f)
        {
            throw new InvalidOperationException(nameof(_throwRecover));
        }

        if (_throwCooldown < 0f)
        {
            throw new InvalidOperationException(nameof(_throwCooldown));
        }
    }

    private void ValidatePhase()
    {
        if (_phaseHealth01 <= 0f || _phaseHealth01 > 1f)
        {
            throw new InvalidOperationException(nameof(_phaseHealth01));
        }

        if (_phaseMoveScale < 1f)
        {
            throw new InvalidOperationException(nameof(_phaseMoveScale));
        }

        if (_phaseCooldownScale <= 0f || _phaseCooldownScale > 1f)
        {
            throw new InvalidOperationException(nameof(_phaseCooldownScale));
        }

        if (_phaseExtraThrowCount < 0)
        {
            throw new InvalidOperationException(nameof(_phaseExtraThrowCount));
        }
    }

    private void ValidateDeath()
    {
        if (_sinkDelay < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDelay));
        }

        if (_sinkDuration <= 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDuration));
        }

        if (_sinkDistance < 0f)
        {
            throw new InvalidOperationException(nameof(_sinkDistance));
        }
    }

    private void ConfigureHealth()
    {
        _health.SetAutoRegen(false);
        _health.SetMaxValue(_maxHealth);
        _health.Fill();
    }

    private void ConfigureCollider()
    {
        _bodyCollider.center = new Vector3(0f, 0.92f, 0f);
        _bodyCollider.size = new Vector3(3.7f, 2.1f, 5f);
    }

    private void ResetState()
    {
        _state = StateIdle;
        _moveTimer = 0f;
        _slamTimer = 0f;
        _throwTimer = 0f;
        _stateTimer = 0f;
        _hasMovePoint = false;
        _isPhaseTwo = false;
        _isDead = false;
        _currentCabinYaw = 0f;
        _currentArmAngle = 14f;
        _currentForearmAngle = -18f;
        _currentBucketAngle = 26f;
        _attackStep = 0;
        _targetTransform = null;
        _targetHealth = null;
        _isCombatActive = _startActive;
        _visual.SetCabinYaw(_currentCabinYaw);
        _visual.SetArmAngles(_currentArmAngle, _currentForearmAngle, _currentBucketAngle);
    }

    public void SetCombatActive(bool isCombatActive)
    {
        if (_isDead)
        {
            return;
        }

        _isCombatActive = isCombatActive;

        if (_isCombatActive)
        {
            _targetTransform = null;
            _targetHealth = null;
            _hasMovePoint = false;
            _moveTimer = 0f;
            _state = StateIdle;
            _stateTimer = 0f;

            return;
        }

        StopCombat();
    }

    private void ResolveTarget()
    {
        if (_targetTransform != null)
        {
            if (_targetTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            _targetTransform = null;
            _targetHealth = null;
        }

        GameObject targetObject = GameObject.FindGameObjectWithTag(_targetTag);

        if (targetObject == null)
        {
            return;
        }

        _targetTransform = targetObject.transform;
        _targetHealth = targetObject.GetComponent<Health>();

        if (_targetHealth != null)
        {
            return;
        }

        _targetHealth = targetObject.GetComponentInParent<Health>();

        if (_targetHealth != null)
        {
            _targetTransform = _targetHealth.transform;

            return;
        }

        _targetHealth = targetObject.GetComponentInChildren<Health>();

        if (_targetHealth != null)
        {
            _targetTransform = _targetHealth.transform;
        }
    }

    private void TickTimers()
    {
        if (_moveTimer > 0f)
        {
            _moveTimer -= Time.deltaTime;
        }

        if (_slamTimer > 0f)
        {
            _slamTimer -= Time.deltaTime;
        }

        if (_throwTimer > 0f)
        {
            _throwTimer -= Time.deltaTime;
        }

        if (_stateTimer > 0f)
        {
            _stateTimer -= Time.deltaTime;
        }
    }

    private void UpdatePhase()
    {
        if (_isPhaseTwo)
        {
            return;
        }

        if (_health.Normalized > _phaseHealth01)
        {
            return;
        }

        _isPhaseTwo = true;
    }

    private void UpdateIdlePose()
    {
        MoveArmTo(14f, -18f, 26f, 120f);
        _currentCabinYaw = Mathf.MoveTowardsAngle(_currentCabinYaw, 0f, _cabinTurnSpeed * Time.deltaTime);
        _visual.SetCabinYaw(_currentCabinYaw);
    }

    private void StopCombat()
    {
        _targetTransform = null;
        _targetHealth = null;
        _state = StateIdle;
        _moveTimer = 0f;
        _slamTimer = 0f;
        _throwTimer = 0f;
        _stateTimer = 0f;
        _hasMovePoint = false;
        _currentCabinYaw = 0f;
        _currentArmAngle = 14f;
        _currentForearmAngle = -18f;
        _currentBucketAngle = 26f;
        _attackStep = 0;
        _visual.SetCabinYaw(_currentCabinYaw);
        _visual.SetArmAngles(_currentArmAngle, _currentForearmAngle, _currentBucketAngle);
    }

    private bool TryStartAttack(Vector3 targetPoint)
    {
        bool canStartSlam = CanStartSlam(targetPoint);
        bool canStartThrow = CanStartThrow(targetPoint);

        if (canStartSlam == false && canStartThrow == false)
        {
            return false;
        }

        if (canStartSlam && canStartThrow)
        {
            if (_attackStep % 2 == 0)
            {
                BeginSlam();
            }

            else
            {
                BeginThrow();
            }

            return true;
        }

        if (canStartSlam)
        {
            BeginSlam();

            return true;
        }

        BeginThrow();

        return true;
    }

    private bool CanStartSlam(Vector3 targetPoint)
    {
        if (_slamTimer > 0f)
        {
            return false;
        }

        float targetDistanceSqr = GetFlatDistanceSqr(transform.position, targetPoint);

        if (targetDistanceSqr > _slamStartDistance * _slamStartDistance)
        {
            return false;
        }

        return true;
    }

    private bool CanStartThrow(Vector3 targetPoint)
    {
        if (_throwTimer > 0f)
        {
            return false;
        }

        float targetDistanceSqr = GetFlatDistanceSqr(transform.position, targetPoint);

        if (targetDistanceSqr > _throwStartDistance * _throwStartDistance)
        {
            return false;
        }

        return true;
    }

    private void BeginSlam()
    {
        _state = StateSlamWindup;
        _stateTimer = _slamWindup;
        _slamTimer = GetScaledCooldown(_slamCooldown);
        _hasMovePoint = false;
        _attackStep += 1;
    }

    private void UpdateSlamWindup()
    {
        MoveArmTo(58f, -28f, 46f, 180f);

        if (_stateTimer > 0f)
        {
            return;
        }

        PerformSlam();
        _state = StateSlamRecover;
        _stateTimer = _slamRecover;
    }

    private void UpdateSlamRecover()
    {
        MoveArmTo(-12f, 22f, -35f, 220f);

        if (_stateTimer > 0f)
        {
            return;
        }

        _state = StateIdle;
    }

    private void PerformSlam()
    {
        Vector3 slamDirection = GetSlamDirection();
        Vector3 attackCenter = _visual.AttackPoint.position + (slamDirection * (_slamRange * 0.35f));
        Vector3 halfExtents = new Vector3(_slamWidth * 0.5f, 1.2f, _slamRange * 0.5f);
        Quaternion attackRotation = Quaternion.LookRotation(slamDirection, Vector3.up);
        int hitCount = Physics.OverlapBoxNonAlloc(
            attackCenter,
            halfExtents,
            _hitBuffer,
            attackRotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _hitBuffer[hitIndex];

            if (TryDamagePlayer(hitCollider, _slamDamage))
            {
                break;
            }

            hitIndex += 1;
        }
    }

    private void BeginThrow()
    {
        _state = StateThrowWindup;
        _stateTimer = _throwWindup;
        _throwTimer = GetScaledCooldown(_throwCooldown);
        _hasMovePoint = false;
        _attackStep += 1;
    }

    private void UpdateThrowWindup(Vector3 targetPoint)
    {
        RotateCabin(targetPoint);
        MoveArmTo(34f, -8f, 22f, 170f);

        if (_stateTimer > 0f)
        {
            return;
        }

        ThrowScrap(targetPoint);
        _state = StateThrowRecover;
        _stateTimer = _throwRecover;
    }

    private void UpdateThrowRecover()
    {
        MoveArmTo(8f, -12f, 12f, 180f);

        if (_stateTimer > 0f)
        {
            return;
        }

        _state = StateIdle;
    }

    private void ThrowScrap(Vector3 targetPoint)
    {
        Vector3 firePoint = _visual.AttackPoint.position;
        Vector3 baseDirection = GetFlatDirection(targetPoint - firePoint);

        if (baseDirection.sqrMagnitude <= DirectionThreshold)
        {
            baseDirection = GetFlatDirection(transform.forward);
        }

        int throwCount = _throwCount;

        if (_isPhaseTwo)
        {
            throwCount += _phaseExtraThrowCount;
        }

        float startAngle = -_throwSpread * 0.5f;
        float angleStep = 0f;

        if (throwCount > 1)
        {
            angleStep = _throwSpread / (throwCount - 1);
        }

        for (int projectileIndex = 0; projectileIndex < throwCount; projectileIndex++)
        {
            float angle = startAngle + (angleStep * projectileIndex);
            Quaternion spreadRotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 projectileDirection = spreadRotation * baseDirection;
            CreateProjectile(firePoint, projectileDirection);
        }
    }

    private void CreateProjectile(Vector3 spawnPoint, Vector3 direction)
    {
        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        projectileObject.name = "Excavator Scrap";
        projectileObject.transform.position = spawnPoint;

        Collider projectileCollider = projectileObject.GetComponent<Collider>();

        if (projectileCollider != null)
        {
            projectileCollider.enabled = false;
            Destroy(projectileCollider);
        }

        ExcavatorBossProjectile projectile = projectileObject.AddComponent<ExcavatorBossProjectile>();
        projectile.Setup(
            transform,
            direction,
            _throwSpeed,
            _throwDamage,
            _throwLifeTime,
            _throwHitRadius,
            _throwSpinSpeed,
            _targetTag
        );
        projectile.SetVisual(Vector3.one * _throwScale, _throwColor);
    }

    private void UpdateMove(Vector3 targetPoint)
    {
        if (_moveTimer > 0f && _hasMovePoint == false)
        {
            return;
        }

        if (_hasMovePoint == false)
        {
            PickMovePoint(targetPoint);
        }

        else
        {
            if (_moveTimer <= 0f)
            {
                PickMovePoint(targetPoint);
            }
        }

        if (_hasMovePoint == false)
        {
            return;
        }

        float moveSpeed = _moveSpeed;

        if (_isPhaseTwo)
        {
            moveSpeed *= _phaseMoveScale;
        }

        Vector3 currentPoint = transform.position;
        Vector3 moveDirection = GetFlatDirection(_movePoint - currentPoint);

        if (moveDirection.sqrMagnitude > DirectionThreshold)
        {
            RotateBody(moveDirection);
        }

        Vector3 nextPoint = Vector3.MoveTowards(currentPoint, _movePoint, moveSpeed * Time.deltaTime);
        transform.position = nextPoint;

        if (GetFlatDistanceSqr(nextPoint, _movePoint) <= _moveReachDistance * _moveReachDistance)
        {
            _hasMovePoint = false;
        }
    }

    private void PickMovePoint(Vector3 targetPoint)
    {
        Vector3 directionToTarget = GetFlatDirection(targetPoint - transform.position);

        if (directionToTarget.sqrMagnitude <= DirectionThreshold)
        {
            directionToTarget = GetFlatDirection(transform.forward);
        }

        Quaternion sideRotation = Quaternion.Euler(0f, 90f * _moveSide, 0f);
        Vector3 sideDirection = sideRotation * directionToTarget;
        Vector3 standPoint = targetPoint - (directionToTarget * _standDistance);
        Vector3 movePoint = standPoint + (sideDirection * _moveRadius);
        float targetDistanceSqr = GetFlatDistanceSqr(transform.position, targetPoint);

        if (targetDistanceSqr < _slamStartDistance * _slamStartDistance)
        {
            movePoint = targetPoint - (directionToTarget * (_standDistance + _moveRadius));
        }

        movePoint = ClampMovePoint(movePoint);
        _moveSide *= -1;
        _movePoint = movePoint;
        _moveTimer = _moveDelay;
        _hasMovePoint = true;
    }

    private Vector3 ClampMovePoint(Vector3 movePoint)
    {
        Vector3 flatDelta = movePoint - _anchorPoint;
        flatDelta.y = 0f;

        if (flatDelta.sqrMagnitude > _anchorRadius * _anchorRadius)
        {
            flatDelta.Normalize();
            movePoint = _anchorPoint + (flatDelta * _anchorRadius);
        }

        movePoint.y = transform.position.y;

        return movePoint;
    }

    private void RotateBody(Vector3 lookDirection)
    {
        lookDirection = GetFlatDirection(lookDirection);

        if (lookDirection.sqrMagnitude <= DirectionThreshold)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
    }

    private void RotateCabin(Vector3 targetPoint)
    {
        Vector3 lookDirection = GetFlatDirection(targetPoint - transform.position);

        if (lookDirection.sqrMagnitude <= DirectionThreshold)
        {
            return;
        }

        Vector3 localDirection = transform.InverseTransformDirection(lookDirection);
        float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        targetYaw = Mathf.Clamp(targetYaw, -70f, 70f);
        _currentCabinYaw = Mathf.MoveTowardsAngle(_currentCabinYaw, targetYaw, _cabinTurnSpeed * Time.deltaTime);
        _visual.SetCabinYaw(_currentCabinYaw);
    }

    private void MoveArmTo(float armAngle, float forearmAngle, float bucketAngle, float speed)
    {
        _currentArmAngle = Mathf.MoveTowardsAngle(_currentArmAngle, armAngle, speed * Time.deltaTime);
        _currentForearmAngle = Mathf.MoveTowardsAngle(_currentForearmAngle, forearmAngle, speed * Time.deltaTime);
        _currentBucketAngle = Mathf.MoveTowardsAngle(_currentBucketAngle, bucketAngle, speed * Time.deltaTime);
        _visual.SetArmAngles(_currentArmAngle, _currentForearmAngle, _currentBucketAngle);
    }

    private bool TryDamagePlayer(Collider hitCollider, float damage)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        Health targetHealth = hitCollider.GetComponentInParent<Health>();

        if (targetHealth == null)
        {
            return false;
        }

        if (IsTargetHealth(targetHealth) == false)
        {
            return false;
        }

        targetHealth.Decrease(damage);

        return true;
    }

    private bool IsTargetHealth(Health targetHealth)
    {
        if (targetHealth == null)
        {
            return false;
        }

        if (_targetHealth == targetHealth)
        {
            return true;
        }

        Transform healthTransform = targetHealth.transform;

        if (healthTransform.CompareTag(_targetTag))
        {
            return true;
        }

        if (healthTransform.root.CompareTag(_targetTag))
        {
            return true;
        }

        return false;
    }

    private Vector3 GetSlamDirection()
    {
        Vector3 slamDirection = GetFlatDirection(_visual.AttackPoint.position - transform.position);

        if (slamDirection.sqrMagnitude <= DirectionThreshold)
        {
            slamDirection = GetFlatDirection(_visual.AttackPoint.forward);
        }

        if (slamDirection.sqrMagnitude <= DirectionThreshold)
        {
            slamDirection = GetFlatDirection(transform.forward);
        }

        return slamDirection;
    }

    private float GetScaledCooldown(float baseCooldown)
    {
        if (_isPhaseTwo == false)
        {
            return baseCooldown;
        }

        return baseCooldown * _phaseCooldownScale;
    }

    private Vector3 GetFlatDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= DirectionThreshold)
        {
            return Vector3.zero;
        }

        direction.Normalize();

        return direction;
    }

    private float GetFlatDistanceSqr(Vector3 firstPoint, Vector3 secondPoint)
    {
        firstPoint.y = 0f;
        secondPoint.y = 0f;

        return (firstPoint - secondPoint).sqrMagnitude;
    }

    private void OnHealthEnded()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;

        if (_bodyCollider.enabled)
        {
            _bodyCollider.enabled = false;
        }

        Action died = Died;

        if (died != null)
        {
            died.Invoke();
        }

        StartCoroutine(SinkCoroutine());
    }

    private IEnumerator SinkCoroutine()
    {
        float delayTimer = 0f;

        while (delayTimer < _sinkDelay)
        {
            delayTimer += Time.deltaTime;

            yield return null;
        }

        Vector3 startPoint = transform.position;
        Vector3 endPoint = startPoint + (Vector3.down * _sinkDistance);
        float sinkTimer = 0f;

        while (sinkTimer < _sinkDuration)
        {
            sinkTimer += Time.deltaTime;
            float sinkProgress = Mathf.Clamp01(sinkTimer / _sinkDuration);
            transform.position = Vector3.Lerp(startPoint, endPoint, sinkProgress);

            yield return null;
        }

        Destroy(gameObject);
    }
}

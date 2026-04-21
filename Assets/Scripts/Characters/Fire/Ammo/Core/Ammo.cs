using System;
using UnityEngine;

public abstract class Ammo : MonoBehaviour
{
    private const CollisionDetectionMode ActiveDetection = CollisionDetectionMode.ContinuousDynamic;
    private const RigidbodyInterpolation ActiveInterpolation = RigidbodyInterpolation.Interpolate;
    private const int SweepHitBufferSize = 16;
    private const float MinSweepDistance = 0.0001f;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collisionCollider;

    [Header("Settings")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _minSpeed = 8f;
    [SerializeField] private float _maxSpeed = 12f;
    [SerializeField] private float _lifetimeSeconds = 5f;

    private readonly RaycastHit[] _sweepHitBuffer = new RaycastHit[SweepHitBufferSize];

    private float _speed;
    private float _lifetimeTimer;
    private bool _isLifeEnded;
    private float _damage;
    private float _speedMultiplier = 1f;
    private Transform _ignoredRoot;

    public event Action Impacted;
    public event Action LifeEnded;
    public event Action<Vector3, Vector3> Moved;
    public event Action<Collider> TargetImpacted;

    public Transform IgnoredRoot => _ignoredRoot;
    public LayerMask TargetLayers => _targetLayers;

    protected float Damage => _damage;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            throw new InvalidOperationException(nameof(_rigidbody));
        }

        if (_collisionCollider == null)
        {
            throw new InvalidOperationException(nameof(_collisionCollider));
        }
    }

    protected virtual void OnEnable()
    {
        _lifetimeTimer = _lifetimeSeconds;
        _isLifeEnded = false;
        _speed = UnityEngine.Random.Range(_minSpeed, _maxSpeed);
        _damage = 0f;
        _speedMultiplier = 1f;

        ApplyPhysics();
        ResetMotion();

        OnAmmoEnabled();
    }

    private void FixedUpdate()
    {
        if (_isLifeEnded)
        {
            return;
        }

        MoveForward();
    }

    protected virtual void Update()
    {
        if (_isLifeEnded)
        {
            return;
        }

        _lifetimeTimer -= Time.deltaTime;

        if (_lifetimeTimer <= 0f)
        {
            EndLife();
        }
    }

    protected virtual void OnDisable()
    {
        ResetMotion();
    }

    public void SetLayers(LayerMask targetLayers)
    {
        _targetLayers = targetLayers;
    }

    public void SetDamage(float damage)
    {
        if (damage <= 0f)
        {
            throw new InvalidOperationException(nameof(damage));
        }

        _damage = damage;
    }

    public void SetSpeedMultiplier(float speedMultiplier)
    {
        if (speedMultiplier <= 0f)
        {
            throw new InvalidOperationException(nameof(speedMultiplier));
        }

        _speedMultiplier = speedMultiplier;
    }

    public void SetIgnoredRoot(Transform ignoredRoot)
    {
        _ignoredRoot = ignoredRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isLifeEnded)
        {
            return;
        }

        TryProcessImpact(other);
    }

    protected virtual void OnAmmoEnabled()
    {
    }

    protected virtual void MoveForward()
    {
        Vector3 startPoint = _rigidbody.position;
        Vector3 moveVelocity = transform.forward * _speed * _speedMultiplier;
        Vector3 moveDelta = moveVelocity * Time.fixedDeltaTime;
        float moveDistance = moveDelta.magnitude;
        Vector3 nextPosition = startPoint + moveDelta;

        if (moveDistance > MinSweepDistance)
        {
            Vector3 moveDirection = moveDelta / moveDistance;

            if (TryMoveToImpactPoint(startPoint, moveDirection, moveDistance, out Collider hitCollider, out Vector3 impactPosition))
            {
                NotifyMoved(startPoint, impactPosition);
                TryProcessImpact(hitCollider);

                return;
            }
        }

        _rigidbody.linearVelocity = moveVelocity;
        NotifyMoved(startPoint, nextPosition);
    }

    protected abstract void OnHitTarget(Collider other);

    protected virtual void OnLifeEnding()
    {
    }

    protected virtual bool ShouldIgnoreCollision(Collider other)
    {
        return false;
    }

    protected bool IsPlayerOwned()
    {
        if (_ignoredRoot == null)
        {
            return false;
        }

        Player player = _ignoredRoot.GetComponentInParent<Player>();

        return player != null;
    }

    protected void EndLife()
    {
        if (_isLifeEnded)
        {
            return;
        }

        _isLifeEnded = true;

        ResetMotion();
        OnLifeEnding();

        Action lifeEnded = LifeEnded;

        if (lifeEnded != null)
        {
            lifeEnded.Invoke();
        }
    }

    private void NotifyMoved(Vector3 startPoint, Vector3 nextPosition)
    {
        Action<Vector3, Vector3> moved = Moved;

        if (moved != null)
        {
            moved.Invoke(startPoint, nextPosition);
        }
    }

    private bool TryMoveToImpactPoint(Vector3 startPoint, Vector3 moveDirection, float moveDistance, out Collider hitCollider, out Vector3 impactPosition)
    {
        if (TryGetNearestSweepHit(moveDirection, moveDistance, out RaycastHit hit) == false)
        {
            hitCollider = null;
            impactPosition = startPoint + moveDirection * moveDistance;

            return false;
        }

        hitCollider = hit.collider;
        impactPosition = startPoint + moveDirection * Mathf.Max(hit.distance, 0f);
        _rigidbody.position = impactPosition;

        return true;
    }

    private bool TryGetNearestSweepHit(Vector3 moveDirection, float moveDistance, out RaycastHit nearestHit)
    {
        int hitCount = CastSweep(moveDirection, moveDistance);
        bool hasHit = false;
        float nearestDistance = float.MaxValue;
        nearestHit = default;

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            RaycastHit hit = _sweepHitBuffer[hitIndex];
            Collider hitCollider = hit.collider;

            if (CanProcessCollision(hitCollider) == false)
            {
                continue;
            }

            if (hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            nearestHit = hit;
            hasHit = true;
        }

        return hasHit;
    }

    private int CastSweep(Vector3 moveDirection, float moveDistance)
    {
        SphereCollider sphereCollider = _collisionCollider as SphereCollider;

        if (sphereCollider != null)
        {
            Vector3 sphereCenter = sphereCollider.bounds.center;
            float sphereRadius = GetMaxComponent(sphereCollider.bounds.extents);

            return Physics.SphereCastNonAlloc(
                sphereCenter,
                sphereRadius,
                moveDirection,
                _sweepHitBuffer,
                moveDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }

        BoxCollider boxCollider = _collisionCollider as BoxCollider;

        if (boxCollider != null)
        {
            Vector3 boxCenter = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 boxHalfExtents = GetBoxHalfExtents(boxCollider);

            return Physics.BoxCastNonAlloc(
                boxCenter,
                boxHalfExtents,
                moveDirection,
                _sweepHitBuffer,
                boxCollider.transform.rotation,
                moveDistance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);
        }

        RaycastHit[] hits = _rigidbody.SweepTestAll(moveDirection, moveDistance, QueryTriggerInteraction.Ignore);
        int copyCount = Mathf.Min(hits.Length, _sweepHitBuffer.Length);

        for (int hitIndex = 0; hitIndex < copyCount; hitIndex++)
        {
            _sweepHitBuffer[hitIndex] = hits[hitIndex];
        }

        return copyCount;
    }

    private Vector3 GetBoxHalfExtents(BoxCollider boxCollider)
    {
        Vector3 lossyScale = boxCollider.transform.lossyScale;

        return new Vector3(
            Mathf.Abs(boxCollider.size.x * lossyScale.x) * 0.5f,
            Mathf.Abs(boxCollider.size.y * lossyScale.y) * 0.5f,
            Mathf.Abs(boxCollider.size.z * lossyScale.z) * 0.5f
        );
    }

    private float GetMaxComponent(Vector3 vector)
    {
        float maxValue = Mathf.Abs(vector.x);
        maxValue = Mathf.Max(maxValue, Mathf.Abs(vector.y));
        maxValue = Mathf.Max(maxValue, Mathf.Abs(vector.z));

        return maxValue;
    }

    private bool TryProcessImpact(Collider other)
    {
        if (CanProcessCollision(other) == false)
        {
            return false;
        }

        bool isTargetLayer = IsInTargetLayers(other.gameObject.layer);

        if (isTargetLayer)
        {
            if (_damage <= 0f)
            {
                throw new InvalidOperationException(nameof(_damage));
            }

            Action<Collider> targetImpacted = TargetImpacted;

            if (targetImpacted != null)
            {
                targetImpacted.Invoke(other);
            }

            OnHitTarget(other);
        }

        Action impacted = Impacted;

        if (impacted != null)
        {
            impacted.Invoke();
        }

        EndLife();

        return true;
    }

    private bool CanProcessCollision(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other == _collisionCollider)
        {
            return false;
        }

        if (IsIgnoredRoot(other))
        {
            return false;
        }

        if (other.isTrigger)
        {
            return false;
        }

        if (ShouldIgnoreCollision(other))
        {
            return false;
        }

        return true;
    }

    private bool IsInTargetLayers(int layer)
    {
        return (_targetLayers.value & (1 << layer)) != 0;
    }

    private bool IsIgnoredRoot(Collider other)
    {
        if (_ignoredRoot == null)
        {
            return false;
        }

        if (other.transform.IsChildOf(_ignoredRoot))
        {
            return true;
        }

        Rigidbody attachedRigidbody = other.attachedRigidbody;

        if (attachedRigidbody == null)
        {
            return false;
        }

        return attachedRigidbody.transform.IsChildOf(_ignoredRoot);
    }

    private void ApplyPhysics()
    {
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = false;
        _rigidbody.interpolation = ActiveInterpolation;
        _rigidbody.collisionDetectionMode = ActiveDetection;
        _rigidbody.WakeUp();
    }

    private void ResetMotion()
    {
        if (_rigidbody.isKinematic)
            return;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}

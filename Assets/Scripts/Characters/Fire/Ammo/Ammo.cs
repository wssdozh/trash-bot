using System;
using UnityEngine;

public abstract class Ammo : MonoBehaviour
{
    private const CollisionDetectionMode ActiveDetection = CollisionDetectionMode.ContinuousDynamic;
    private const RigidbodyInterpolation ActiveInterpolation = RigidbodyInterpolation.Interpolate;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody _rigidbody;

    [Header("Settings")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _minSpeed = 8f;
    [SerializeField] private float _maxSpeed = 12f;
    [SerializeField] private float _lifetimeSeconds = 5f;

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
            throw new InvalidOperationException(nameof(_rigidbody));
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
            return;

        MoveForward();
    }

    protected virtual void Update()
    {
        if (_isLifeEnded)
            return;

        _lifetimeTimer -= Time.deltaTime;

        if (_lifetimeTimer <= 0f)
            EndLife();
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
            throw new InvalidOperationException(nameof(damage));

        _damage = damage;
    }

    public void SetSpeedMultiplier(float speedMultiplier)
    {
        if (speedMultiplier <= 0f)
            throw new InvalidOperationException(nameof(speedMultiplier));

        _speedMultiplier = speedMultiplier;
    }

    public void SetIgnoredRoot(Transform ignoredRoot)
    {
        _ignoredRoot = ignoredRoot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isLifeEnded)
            return;

        if (IsIgnoredRoot(other))
            return;

        bool isTargetLayer = IsInTargetLayers(other.gameObject.layer);

        if (other.isTrigger)
            return;

        if (isTargetLayer)
        {
            if (_damage <= 0f)
                throw new InvalidOperationException(nameof(_damage));

            Action<Collider> targetImpacted = TargetImpacted;

            if (targetImpacted != null)
                targetImpacted.Invoke(other);

            OnHitTarget(other);
        }

        Action impacted = Impacted;

        if (impacted != null)
            impacted.Invoke();

        EndLife();
    }

    protected virtual void OnAmmoEnabled()
    {
    }

    protected virtual void MoveForward()
    {
        Vector3 startPoint = _rigidbody.position;
        Vector3 moveVelocity = transform.forward * _speed * _speedMultiplier;
        Vector3 nextPosition = startPoint + moveVelocity * Time.fixedDeltaTime;

        _rigidbody.linearVelocity = moveVelocity;

        Action<Vector3, Vector3> moved = Moved;

        if (moved != null)
            moved.Invoke(startPoint, nextPosition);
    }

    protected abstract void OnHitTarget(Collider other);

    protected virtual void OnLifeEnding()
    {
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
            return;

        _isLifeEnded = true;

        ResetMotion();
        OnLifeEnding();

        Action lifeEnded = LifeEnded;

        if (lifeEnded != null)
            lifeEnded.Invoke();
    }

    private bool IsInTargetLayers(int layer)
    {
        return (_targetLayers.value & (1 << layer)) != 0;
    }

    private bool IsIgnoredRoot(Collider other)
    {
        if (_ignoredRoot == null)
            return false;

        if (other.transform.IsChildOf(_ignoredRoot))
            return true;

        Rigidbody attachedRigidbody = other.attachedRigidbody;

        if (attachedRigidbody == null)
            return false;

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
        {
            return;
        }

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }
}

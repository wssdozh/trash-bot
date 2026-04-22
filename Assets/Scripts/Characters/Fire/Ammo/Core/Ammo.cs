using System;
using UnityEngine;

public abstract partial class Ammo : MonoBehaviour
{
    private const CollisionDetectionMode ActiveDetection = CollisionDetectionMode.ContinuousDynamic;
    private const RigidbodyInterpolation ActiveInterpolation = RigidbodyInterpolation.Interpolate;
    private const int OverlapHitBufferSize = 16;
    private const int SweepHitBufferSize = 16;
    private const float MinSweepDistance = 0.0001f;
    private const QueryTriggerInteraction SweepTriggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Collider _collisionCollider;

    [Header("Settings")]
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _minSpeed = 8f;
    [SerializeField] private float _maxSpeed = 12f;
    [SerializeField] private float _lifetimeSeconds = 5f;

    private readonly Collider[] _overlapHitBuffer = new Collider[OverlapHitBufferSize];
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
}

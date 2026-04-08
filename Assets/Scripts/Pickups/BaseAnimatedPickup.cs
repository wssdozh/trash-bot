using System;
using UnityEngine;

public abstract class BaseAnimatedPickup : BasePickup
{
    private const CollisionDetectionMode ActiveDetection = CollisionDetectionMode.ContinuousDynamic;
    private const CollisionDetectionMode HeldDetection = CollisionDetectionMode.ContinuousSpeculative;
    private const RigidbodyInterpolation ActiveInterpolation = RigidbodyInterpolation.Interpolate;

    [SerializeField] protected PickupAnimator _pickupAnimator;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] protected Collider _collider;
    [SerializeField] private PickupReturner _returner;
    [SerializeField] private PickupIdle _pickupIdle;

    protected virtual void Awake()
    {
        if (_rigidbody == null)
            throw new InvalidOperationException(nameof(_rigidbody));

        if (_collider == null)
            throw new InvalidOperationException(nameof(_collider));

        if (_returner == null)
            throw new InvalidOperationException(nameof(_returner));
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        _collider.enabled = true;

        ApplyActivePhysics();
        ResetMotion();

        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
            pickupIdle.SetIdleActive(true);
    }

    protected virtual void OnDisable()
    {
        ResetMotion();

        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
            pickupIdle.SetIdleActive(false);
    }

    protected override void OnPickup(GameObject player)
    {
        PickupIdle pickupIdle = GetPickupIdle();

        if (pickupIdle != null)
            pickupIdle.SetIdleActive(false);

        if (_pickupAnimator == null)
            _pickupAnimator = GetComponent<PickupAnimator>();

        if (_pickupAnimator != null)
        {
            _collider.enabled = false;

            ResetMotion();

            _rigidbody.isKinematic = true;
            _rigidbody.collisionDetectionMode = HeldDetection;

            _pickupAnimator.PlayAttraction(player.transform, () => OnConsumed(player));

            return;
        }

        OnConsumed(player);
    }

    protected virtual void OnConsumed(GameObject player)
    {
        _returner.ReturnToPool();
    }

    private void ApplyActivePhysics()
    {
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

    private PickupIdle GetPickupIdle()
    {
        if (_pickupIdle == null)
            _pickupIdle = GetComponentInChildren<PickupIdle>(true);

        return _pickupIdle;
    }
}

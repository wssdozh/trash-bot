using System;
using UnityEngine;

public abstract partial class Ammo
{
    private bool TryGetOverlapHit(out Collider hitCollider)
    {
        int hitCount = CastOverlap();

        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider overlappedCollider = _overlapHitBuffer[hitIndex];

            if (CanProcessCollision(overlappedCollider) == false)
            {
                continue;
            }

            hitCollider = overlappedCollider;

            return true;
        }

        hitCollider = null;

        return false;
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

    private int CastOverlap()
    {
        SphereCollider sphereCollider = _collisionCollider as SphereCollider;

        if (sphereCollider != null)
        {
            Vector3 sphereCenter = sphereCollider.bounds.center;
            float sphereRadius = GetMaxComponent(sphereCollider.bounds.extents);

            return Physics.OverlapSphereNonAlloc(
                sphereCenter,
                sphereRadius,
                _overlapHitBuffer,
                Physics.AllLayers,
                SweepTriggerInteraction);
        }

        BoxCollider boxCollider = _collisionCollider as BoxCollider;

        if (boxCollider != null)
        {
            Vector3 boxCenter = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 boxHalfExtents = GetBoxHalfExtents(boxCollider);

            return Physics.OverlapBoxNonAlloc(
                boxCenter,
                boxHalfExtents,
                _overlapHitBuffer,
                boxCollider.transform.rotation,
                Physics.AllLayers,
                SweepTriggerInteraction);
        }

        return Physics.OverlapSphereNonAlloc(
            _collisionCollider.bounds.center,
            GetMaxComponent(_collisionCollider.bounds.extents),
            _overlapHitBuffer,
            Physics.AllLayers,
            SweepTriggerInteraction);
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
                SweepTriggerInteraction);
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
                SweepTriggerInteraction);
        }

        RaycastHit[] hits = _rigidbody.SweepTestAll(moveDirection, moveDistance, SweepTriggerInteraction);
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
            if (CanProcessTriggerCollision(other) == false)
            {
                return false;
            }
        }

        if (ShouldIgnoreCollision(other))
        {
            return false;
        }

        return true;
    }

    private bool CanProcessTriggerCollision(Collider other)
    {
        EnemyAimCollider enemyAimCollider = other.GetComponent<EnemyAimCollider>();

        if (enemyAimCollider != null)
        {
            return true;
        }

        return false;
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
}

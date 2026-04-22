using System;
using UnityEngine;

public abstract partial class Ammo
{
    protected virtual void MoveForward()
    {
        Vector3 startPoint = _rigidbody.position;
        Vector3 moveVelocity = transform.forward * _speed * _speedMultiplier;
        Vector3 moveDelta = moveVelocity * Time.fixedDeltaTime;
        float moveDistance = moveDelta.magnitude;
        Vector3 nextPosition = startPoint + moveDelta;
        Collider hitCollider;

        if (TryGetOverlapHit(out hitCollider))
        {
            NotifyMoved(startPoint, startPoint);
            TryProcessImpact(hitCollider);

            return;
        }

        if (moveDistance > MinSweepDistance)
        {
            Vector3 moveDirection = moveDelta / moveDistance;

            if (TryMoveToImpactPoint(startPoint, moveDirection, moveDistance, out hitCollider, out Vector3 impactPosition))
            {
                NotifyMoved(startPoint, impactPosition);
                TryProcessImpact(hitCollider);

                return;
            }
        }

        _rigidbody.linearVelocity = moveVelocity;
        NotifyMoved(startPoint, nextPosition);
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

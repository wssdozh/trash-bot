using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public sealed class RoomEnterTrigger : MonoBehaviour
{
    private const float MinSize = 0.0001f;
    private const float MinHeightScale = 6f;
    private const string IgnoreRaycastLayer = "Ignore Raycast";

    [SerializeField, Min(0f)] private float _expandInBlocks = 0.5f;

    private readonly List<Collider> _playerColliders = new List<Collider>(4);

    private BoxCollider _boxCollider;
    private Rigidbody _rigidbody;

    public event Action Entered;

    public void Setup(Bounds roomBounds, float blockSize)
    {
        EnsureComponents();
        ApplyBounds(roomBounds, blockSize);
    }

    private void OnDisable()
    {
        _playerColliders.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other) == false)
        {
            return;
        }

        if (_playerColliders.Contains(other))
        {
            return;
        }

        _playerColliders.Add(other);

        if (_playerColliders.Count == 1 && Entered != null)
        {
            Entered.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other) == false)
        {
            return;
        }

        _playerColliders.Remove(other);
    }

    private void EnsureComponents()
    {
        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider>();
        }

        if (_boxCollider == null)
        {
            throw new InvalidOperationException(nameof(_boxCollider));
        }

        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        if (_rigidbody == null)
        {
            throw new InvalidOperationException(nameof(_rigidbody));
        }

        int ignoreRaycastLayer = LayerMask.NameToLayer(IgnoreRaycastLayer);

        if (ignoreRaycastLayer >= 0)
        {
            gameObject.layer = ignoreRaycastLayer;
        }

        _boxCollider.isTrigger = true;
        _boxCollider.center = Vector3.zero;

        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void ApplyBounds(Bounds roomBounds, float blockSize)
    {
        float safeBlockSize = Mathf.Max(blockSize, MinSize);
        float expandSize = Mathf.Max(0f, _expandInBlocks) * safeBlockSize * 2f;
        float triggerHeight = Mathf.Max(roomBounds.size.y, safeBlockSize * MinHeightScale);
        float triggerWidth = Mathf.Max(safeBlockSize, roomBounds.size.x + expandSize);
        float triggerDepth = Mathf.Max(safeBlockSize, roomBounds.size.z + expandSize);
        Vector3 triggerSize = new Vector3(triggerWidth, triggerHeight, triggerDepth);
        Vector3 triggerCenter = roomBounds.center;
        triggerCenter.y = roomBounds.min.y + (triggerHeight * 0.5f);

        transform.position = triggerCenter;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        _boxCollider.size = triggerSize;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.GetComponentInParent<Player>() != null)
        {
            return true;
        }

        if (other.attachedRigidbody == null)
        {
            return false;
        }

        return other.attachedRigidbody.GetComponentInParent<Player>() != null;
    }
}

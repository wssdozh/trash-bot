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

    [SerializeField, Min(0f)] private float _expandInBlocks = 0.25f;
    [SerializeField, Min(0.1f)] private float _depthInBlocks = 1f;
    [SerializeField, Min(0f)] private float _insideOffsetInBlocks = 0.2f;

    private readonly List<Collider> _playerColliders = new List<Collider>(4);

    private BoxCollider _boxCollider;
    private Rigidbody _rigidbody;

    public event Action Entered;

    public void Setup(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        if (roomDoorMarker == null)
        {
            throw new InvalidOperationException(nameof(roomDoorMarker));
        }

        EnsureComponents();
        ApplyDoorBounds(roomDoorMarker, blockSize);
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

    private void ApplyDoorBounds(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        float safeBlockSize = Mathf.Max(blockSize, MinSize);
        float expandSize = Mathf.Max(0f, _expandInBlocks) * safeBlockSize * 2f;
        float inwardOffset = Mathf.Max(0f, _insideOffsetInBlocks) * safeBlockSize;
        float triggerWidth = Mathf.Max(safeBlockSize, (roomDoorMarker.WidthInBlocks * safeBlockSize) + expandSize);
        float triggerHeight = Mathf.Max(roomDoorMarker.HeightInBlocks * safeBlockSize, safeBlockSize * MinHeightScale);
        float triggerDepth = Mathf.Max(safeBlockSize, _depthInBlocks * safeBlockSize);
        Vector3 triggerSize = new Vector3(triggerWidth, triggerHeight, triggerDepth);
        Vector3 inwardDirection = roomDoorMarker.transform.rotation * GetInwardDirection(roomDoorMarker.Side);
        Vector3 triggerPosition = roomDoorMarker.transform.position + (inwardDirection * inwardOffset);
        Quaternion triggerRotation = roomDoorMarker.transform.rotation * GetTriggerRotation(roomDoorMarker.Side);

        transform.position = triggerPosition;
        transform.rotation = triggerRotation;
        transform.localScale = Vector3.one;
        _boxCollider.size = triggerSize;
    }

    private Quaternion GetTriggerRotation(DoorSide side)
    {
        if (side == DoorSide.East)
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        if (side == DoorSide.West)
        {
            return Quaternion.Euler(0f, 90f, 0f);
        }

        return Quaternion.identity;
    }

    private Vector3 GetInwardDirection(DoorSide side)
    {
        if (side == DoorSide.North)
        {
            return Vector3.back;
        }

        if (side == DoorSide.South)
        {
            return Vector3.forward;
        }

        if (side == DoorSide.East)
        {
            return Vector3.left;
        }

        return Vector3.right;
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.isTrigger)
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

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomDoorGate : MonoBehaviour
{
    private const float MinSize = 0.0001f;

    [SerializeField] private float _moveTime = 0.38f;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private BoxCollider _boxCollider;
    private Vector3 _closedSize;
    private Vector3 _closedPosition;
    private Vector3 _hiddenPosition;
    private float _progress;
    private float _targetProgress;
    private float _moveSpeed;
    private RoomShellBuilder _roomShellBuilder;

    public void Setup(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        if (roomDoorMarker == null)
            throw new InvalidOperationException(nameof(roomDoorMarker));

        CacheRoomShellBuilder();
        EnsureView();
        ConfigureView();
        ConfigureGate(roomDoorMarker, blockSize);
        SetClosed(false, true);
    }

    public void SetClosed(bool isClosed, bool isInstant)
    {
        if (isClosed)
            _boxCollider.enabled = true;

        if (isClosed)
            _targetProgress = 1f;
        else
            _targetProgress = 0f;

        if (isInstant)
        {
            _progress = _targetProgress;
            ApplyProgress();
            _boxCollider.enabled = isClosed;
            enabled = false;

            return;
        }

        if (isClosed == false && _progress <= MinSize)
            _boxCollider.enabled = false;

        if (Mathf.Abs(_progress - _targetProgress) <= MinSize)
        {
            enabled = false;

            return;
        }

        enabled = true;
    }

    private void Update()
    {
        if (Mathf.Abs(_progress - _targetProgress) <= MinSize)
        {
            enabled = false;

            return;
        }

        _progress = Mathf.MoveTowards(_progress, _targetProgress, _moveSpeed * Time.deltaTime);
        ApplyProgress();

        if (_targetProgress <= MinSize)
        {
            if (_progress <= MinSize)
                _boxCollider.enabled = false;
        }

        if (Mathf.Abs(_progress - _targetProgress) <= MinSize)
        {
            enabled = false;
        }
    }

    private void EnsureView()
    {
        RemoveLegacyView();

        if (_boxCollider == null)
        {
            _boxCollider = GetComponent<BoxCollider>();

            if (_boxCollider == null)
                _boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();

            if (_meshFilter == null)
                _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();

            if (_meshRenderer == null)
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        EnsureCubeVisual();
    }

    private void ConfigureView()
    {
        if (_meshRenderer == null)
            return;

        Material fenceMaterial = GetFenceMaterial();

        if (fenceMaterial != null)
        {
            _meshRenderer.sharedMaterial = fenceMaterial;

            return;
        }

        Material material = _meshRenderer.material;

        if (material == null)
            return;

        if (material.HasProperty("_Color"))
            material.color = new Color(0.17f, 0.33f, 0.36f, 1f);
    }

    private void ConfigureGate(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        float safeBlockSize = Mathf.Max(blockSize, MinSize);
        float gateWidth = roomDoorMarker.WidthInBlocks * safeBlockSize;
        float gateThickness = safeBlockSize;
        float gateHeight = GetGateHeight(roomDoorMarker, safeBlockSize);
        float gateDownOffset = GetGateDownOffset(roomDoorMarker, safeBlockSize);
        Vector3 inwardDirection = GetInwardDirection(roomDoorMarker.Side);
        Vector3 worldInwardDirection = roomDoorMarker.transform.rotation * inwardDirection;
        Vector3 gatePosition = roomDoorMarker.transform.position - (worldInwardDirection * safeBlockSize);
        gatePosition.y -= gateDownOffset;
        Quaternion gateRotation = roomDoorMarker.transform.rotation * GetGateRotation(roomDoorMarker.Side);

        _closedSize = new Vector3(gateWidth, gateHeight, gateThickness);
        _moveSpeed = 1f / Mathf.Max(_moveTime, MinSize);
        transform.position = gatePosition;
        transform.rotation = gateRotation;
        transform.localScale = _closedSize;
        _boxCollider.center = Vector3.zero;
        _boxCollider.size = Vector3.one;
        _closedPosition = gatePosition + (transform.up * (gateHeight * 0.5f));
        _hiddenPosition = _closedPosition - (transform.up * gateHeight);
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        if (_meshRenderer == null)
            return;

        float currentProgress = Mathf.Clamp01(_progress);
        transform.localScale = _closedSize;
        transform.position = Vector3.Lerp(_hiddenPosition, _closedPosition, currentProgress);

        _meshRenderer.enabled = currentProgress > MinSize;
    }

    private void CacheRoomShellBuilder()
    {
        if (_roomShellBuilder != null)
        {
            return;
        }

        _roomShellBuilder = GetComponentInParent<RoomShellBuilder>();
    }

    private Material GetFenceMaterial()
    {
        if (_roomShellBuilder == null)
        {
            return null;
        }

        return _roomShellBuilder.FenceMaterial;
    }

    private float GetGateHeight(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        if (_roomShellBuilder == null)
        {
            return roomDoorMarker.HeightInBlocks * blockSize;
        }

        int heightInBlocks = Mathf.Max(roomDoorMarker.HeightInBlocks, _roomShellBuilder.PostHeightInBlocks);

        return heightInBlocks * blockSize;
    }

    private float GetGateDownOffset(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        if (_roomShellBuilder == null)
        {
            return 0f;
        }

        int extraHeightInBlocks = _roomShellBuilder.PostHeightInBlocks - roomDoorMarker.HeightInBlocks;

        if (extraHeightInBlocks < 0)
        {
            extraHeightInBlocks = 0;
        }

        return extraHeightInBlocks * blockSize;
    }

    private void EnsureCubeVisual()
    {
        bool hasMesh = _meshFilter.sharedMesh != null;
        bool hasMaterial = _meshRenderer.sharedMaterial != null;

        if (hasMesh && hasMaterial)
            return;

        GameObject cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        MeshFilter cubeMeshFilter = cubeObject.GetComponent<MeshFilter>();
        MeshRenderer cubeMeshRenderer = cubeObject.GetComponent<MeshRenderer>();

        if (hasMesh == false)
            _meshFilter.sharedMesh = cubeMeshFilter.sharedMesh;

        if (hasMaterial == false)
            _meshRenderer.sharedMaterial = cubeMeshRenderer.sharedMaterial;

        if (Application.isPlaying == false)
            DestroyImmediate(cubeObject);
        else
            Destroy(cubeObject);
    }

    private void RemoveLegacyView()
    {
        Transform legacyView = transform.Find("View");

        if (legacyView == null)
            return;

        if (Application.isPlaying == false)
            DestroyImmediate(legacyView.gameObject);
        else
            Destroy(legacyView.gameObject);
    }

    private Quaternion GetGateRotation(DoorSide side)
    {
        if (side == DoorSide.East)
            return Quaternion.Euler(0f, 90f, 0f);

        if (side == DoorSide.West)
            return Quaternion.Euler(0f, 90f, 0f);

        return Quaternion.identity;
    }

    private Vector3 GetInwardDirection(DoorSide side)
    {
        if (side == DoorSide.North)
            return Vector3.back;

        if (side == DoorSide.South)
            return Vector3.forward;

        if (side == DoorSide.East)
            return Vector3.left;

        return Vector3.right;
    }
}

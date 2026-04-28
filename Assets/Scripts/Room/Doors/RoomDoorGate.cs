using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomDoorGate : MonoBehaviour
{
    private const float MinSize = 0.0001f;

    [SerializeField] private float _moveTime = 0.38f;
    [SerializeField, Min(0.01f)] private float _thicknessMultiplier = 1.5f;
    [SerializeField] private GameObject _visualPrefab;
    [SerializeField] private string _visualResourcePath = "Meshes/Props/door";
    [SerializeField] private string _fallbackVisualResourcePath = "Prefabs/Room/Doors/DoorGateVisual";
    [SerializeField] private float _visualYawOffset = 0f;

    private readonly List<Renderer> _visualRenderers = new List<Renderer>();
    private BoxCollider _boxCollider;
    private Transform _visualRoot;
    private GameObject _resolvedVisualPrefab;
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
        {
            throw new InvalidOperationException(nameof(roomDoorMarker));
        }

        CacheRoomShellBuilder();
        EnsureView();
        ConfigureGate(roomDoorMarker, blockSize);
        SetClosed(false, true);
    }

    public void SetClosed(bool isClosed, bool isInstant)
    {
        if (isClosed == true)
        {
            _boxCollider.enabled = true;
        }

        if (isClosed == true)
        {
            _targetProgress = 1f;
        }
        else
        {
            _targetProgress = 0f;
        }

        if (isInstant == true)
        {
            _progress = _targetProgress;
            ApplyProgress();
            _boxCollider.enabled = isClosed;
            enabled = false;
            return;
        }

        if (isClosed == false && _progress <= MinSize)
        {
            _boxCollider.enabled = false;
        }

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
            {
                _boxCollider.enabled = false;
            }
        }

        if (Mathf.Abs(_progress - _targetProgress) <= MinSize)
        {
            enabled = false;
        }
    }

    private void EnsureView()
    {
        EnsureCollider();
        RemoveLegacyRootVisual();
        ClearVisualChildren();
        ResolveVisualPrefab();
        CreateVisual();
        CacheVisualRenderers();
        NormalizeVisualToUnitCollider();
    }

    private void EnsureCollider()
    {
        if (_boxCollider != null)
        {
            return;
        }

        _boxCollider = GetComponent<BoxCollider>();

        if (_boxCollider == null)
        {
            _boxCollider = gameObject.AddComponent<BoxCollider>();
        }
    }

    private void ResolveVisualPrefab()
    {
        _resolvedVisualPrefab = _visualPrefab;

        if (_resolvedVisualPrefab != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_visualResourcePath) == true)
        {
            throw new InvalidOperationException(nameof(_visualResourcePath));
        }

        _resolvedVisualPrefab = Resources.Load<GameObject>(_visualResourcePath);

        if (_resolvedVisualPrefab != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_fallbackVisualResourcePath) == true)
        {
            throw new InvalidOperationException(nameof(_fallbackVisualResourcePath));
        }

        _resolvedVisualPrefab = Resources.Load<GameObject>(_fallbackVisualResourcePath);

        if (_resolvedVisualPrefab == null)
        {
            throw new InvalidOperationException(nameof(_fallbackVisualResourcePath));
        }
    }

    private void CreateVisual()
    {
        GameObject visualInstance = Instantiate(_resolvedVisualPrefab, transform);
        _visualRoot = visualInstance.transform;
        _visualRoot.name = "Visual";
        _visualRoot.localPosition = Vector3.zero;
        _visualRoot.localRotation = Quaternion.Euler(0f, _visualYawOffset, 0f);
        _visualRoot.localScale = Vector3.one;
    }

    private void CacheVisualRenderers()
    {
        _visualRenderers.Clear();

        if (_visualRoot == null)
        {
            throw new InvalidOperationException(nameof(_visualRoot));
        }

        Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            throw new InvalidOperationException(nameof(renderers));
        }

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer visualRenderer = renderers[rendererIndex];

            if (visualRenderer == null)
            {
                continue;
            }

            _visualRenderers.Add(visualRenderer);
        }

        if (_visualRenderers.Count == 0)
        {
            throw new InvalidOperationException(nameof(_visualRenderers));
        }
    }

    private void NormalizeVisualToUnitCollider()
    {
        Bounds localBounds;
        bool hasBounds = TryGetLocalRendererBounds(_visualRoot, out localBounds);

        if (hasBounds == false)
        {
            throw new InvalidOperationException(nameof(Renderer));
        }

        Vector3 axisScale = new Vector3(
            GetAxisScale(1f, localBounds.size.x),
            GetAxisScale(1f, localBounds.size.y),
            GetAxisScale(1f, localBounds.size.z)
        );

        _visualRoot.localScale = Vector3.Scale(_visualRoot.localScale, axisScale);
        _visualRoot.localPosition = -(_visualRoot.localRotation * Vector3.Scale(localBounds.center, axisScale));
    }

    private void ConfigureGate(RoomDoorMarker roomDoorMarker, float blockSize)
    {
        float safeBlockSize = Mathf.Max(blockSize, MinSize);
        float gateWidth = roomDoorMarker.WidthInBlocks * safeBlockSize;
        float gateThickness = safeBlockSize * _thicknessMultiplier;
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
        float currentProgress = Mathf.Clamp01(_progress);
        transform.localScale = _closedSize;
        transform.position = Vector3.Lerp(_hiddenPosition, _closedPosition, currentProgress);

        SetVisualVisible(currentProgress > MinSize);
    }

    private void SetVisualVisible(bool isVisible)
    {
        for (int rendererIndex = 0; rendererIndex < _visualRenderers.Count; rendererIndex++)
        {
            Renderer visualRenderer = _visualRenderers[rendererIndex];

            if (visualRenderer == null)
            {
                continue;
            }

            visualRenderer.enabled = isVisible;
        }
    }

    private void CacheRoomShellBuilder()
    {
        if (_roomShellBuilder != null)
        {
            return;
        }

        _roomShellBuilder = GetComponentInParent<RoomShellBuilder>();
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

    private Quaternion GetGateRotation(DoorSide side)
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

    private bool TryGetLocalRendererBounds(Transform rootTransform, out Bounds localBounds)
    {
        Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer visualRenderer = renderers[rendererIndex];

            if (visualRenderer == null)
            {
                continue;
            }

            Bounds rendererBounds = visualRenderer.bounds;
            EncapsulateWorldPoint(rootTransform, rendererBounds.min, ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, rendererBounds.max, ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.max.z), ref localBounds, ref hasBounds);
            EncapsulateWorldPoint(rootTransform, new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.min.z), ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private void EncapsulateWorldPoint(Transform rootTransform, Vector3 worldPoint, ref Bounds localBounds, ref bool hasBounds)
    {
        Vector3 localPoint = rootTransform.InverseTransformPoint(worldPoint);

        if (hasBounds == false)
        {
            localBounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        localBounds.Encapsulate(localPoint);
    }

    private float GetAxisScale(float targetSize, float sourceSize)
    {
        if (sourceSize < MinSize)
        {
            return 1f;
        }

        return targetSize / sourceSize;
    }

    private void ClearVisualChildren()
    {
        int childCount = transform.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = transform.GetChild(childIndex);
            DestroyUnityObject(childTransform.gameObject);
        }

        _visualRoot = null;
        _visualRenderers.Clear();
    }

    private void RemoveLegacyRootVisual()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            DestroyUnityObject(meshRenderer);
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter != null)
        {
            DestroyUnityObject(meshFilter);
        }
    }

    private void DestroyUnityObject(UnityEngine.Object targetObject)
    {
        if (Application.isPlaying == true)
        {
            Destroy(targetObject);
            return;
        }

        DestroyImmediate(targetObject);
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LevelCorridorBuilder : MonoBehaviour
{
    private const float WallFaceYawOffset = -90f;
    private const float MinSize = 0.01f;
    private const float MinVisualSize = 0.0001f;
    private static readonly int s_mainTexStId = Shader.PropertyToID("_MainTex_ST");
    private static readonly int s_baseMapStId = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int s_colorMapStId = Shader.PropertyToID("_ColorMap_ST");
    private static readonly int s_normalMapStId = Shader.PropertyToID("_NormalMap_ST");

    private struct WallVisualMetrics
    {
        public Bounds LocalBounds;
        public bool LengthAlongX;
        public float SourceLength;

        public WallVisualMetrics(Bounds localBounds)
        {
            LocalBounds = localBounds;
            LengthAlongX = localBounds.size.x > localBounds.size.z;
            SourceLength = LengthAlongX == true ? localBounds.size.x : localBounds.size.z;
        }
    }

    private enum WallPostPlacement
    {
        None,
        CornersAndDoors,
        EveryFenceSegment
    }

    [Header("Grid")]
    [SerializeField, Min(0.01f)] private float _blockSize = 1f;
    [SerializeField, Min(0f)] private float _doorInsetInBlocks = 1f;

    [Header("Floor (one stretched prefab)")]
    [SerializeField] private GameObject _floorPrefab;

    [Tooltip("Автоматически измерять базовый размер префаба пола (X/Z) через Renderer.bounds и кэшировать.")]
    [SerializeField] private bool _autoDetectFloorPrefabSize = true;

    [Tooltip("Ручной базовый размер префаба пола в юнитах (X = длина, Y = ширина по Z).\nИспользуется, если авто-детект выключен.")]
    [SerializeField] private Vector2 _floorPrefabSizeInUnits = new Vector2(10f, 10f);

    [SerializeField] private Vector3 _floorLocalOffset;
    [SerializeField, Min(0.01f)] private float _floorTextureTilingMultiplier = 0.2f;
    [SerializeField, Min(0f)] private float _floorEndTrimInBlocks = 0.5f;

    [Header("Walls (optional)")]
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _postPrefab;
    [SerializeField] private WallPostPlacement _wallPostPlacement = WallPostPlacement.CornersAndDoors;
    [SerializeField] private GameObject _wallPostVisualPrefab;
    [SerializeField] private string _wallPostVisualResourcePath;
    [SerializeField, Min(0.01f)] private float _wallPostVisualHeightMultiplier = 0.825f;
    [SerializeField, Min(0.01f)] private float _wallPostVisualThicknessMultiplier = 1f;
    [SerializeField] private float _wallPostVisualYawOffset = 0f;

    [SerializeField, Min(0.01f)] private float _wallPrefabLengthInUnits = 1f;
    [SerializeField, Min(0.01f)] private float _wallPrefabHeightInUnits = 1f;
    [SerializeField, Min(0.01f)] private float _postPrefabHeightInUnits = 1f;

    [SerializeField, Min(0f)] private float _wallHeightInUnits = 2f;
    [SerializeField, Min(0f)] private float _wallSideOffsetInUnits = 0f;
    [SerializeField, Min(1)] private int _postSpacingInBlocks = 6;
    [SerializeField, Min(0f)] private float _wallSegmentEndGapInBlocks = 0.5f;

    [SerializeField] private Vector3 _wallLocalOffset;
    [SerializeField] private bool _enableColliders = true;

    [Header("Wall Visuals")]
    [SerializeField] private List<GameObject> _wallVisualPrefabs = new List<GameObject>();
    [SerializeField] private List<string> _wallVisualResourcePaths = new List<string>();
    [SerializeField] private bool _hideWallBlockRenderersWhenVisualsAssigned = true;
    [SerializeField, Min(0.01f)] private float _wallVisualHeightMultiplier = 0.75f;
    [SerializeField, Min(0.01f)] private float _wallVisualThicknessMultiplier = 0.45f;
    [SerializeField, Min(1f)] private float _wallVisualMaximumStretch = 1.35f;
    [SerializeField] private float _wallVisualYawOffset = 180f;
    [SerializeField] private float _wallVisualFloorOffsetInUnits = 0f;

    private bool _hasCachedFloorSize;
    private Vector2 _cachedFloorSizeInUnits;
    private readonly List<GameObject> _resolvedWallVisualPrefabs = new List<GameObject>();
    private GameObject _resolvedWallPostVisualPrefab;

    public float BlockSize => _blockSize;

    public void BuildBetweenDoors(Transform parent, RoomDoorMarker fromDoor, RoomDoorMarker toDoor, int corridorWidthInBlocks)
    {
        if (parent == null)
        {
            throw new InvalidOperationException(nameof(parent));
        }

        if (fromDoor == null)
        {
            throw new InvalidOperationException(nameof(fromDoor));
        }

        if (toDoor == null)
        {
            throw new InvalidOperationException(nameof(toDoor));
        }

        if (_floorPrefab == null)
        {
            throw new InvalidOperationException(nameof(_floorPrefab));
        }

        if (corridorWidthInBlocks < 1)
        {
            corridorWidthInBlocks = 1;
        }

        Vector3 from = fromDoor.transform.position;
        Vector3 to = toDoor.transform.position;

        from.y = 0f;
        to.y = 0f;

        float deltaX = Mathf.Abs(from.x - to.x);
        float deltaZ = Mathf.Abs(from.z - to.z);

        bool alongX = deltaX >= deltaZ;

        if (alongX == true)
        {
            if (deltaZ > 0.001f)
            {
                throw new InvalidOperationException("Corridor must be strictly straight (same X or same Z).");
            }
        }
        else
        {
            if (deltaX > 0.001f)
            {
                throw new InvalidOperationException("Corridor must be strictly straight (same X or same Z).");
            }
        }

        float horizontalScale = GetHorizontalScale(parent);
        float verticalScale = GetVerticalScale(parent);
        float widthUnits = corridorWidthInBlocks * _blockSize * horizontalScale;

        float endInsetUnits = _doorInsetInBlocks * _blockSize * horizontalScale;

        float lengthUnits;
        Vector3 center;
        Quaternion rotation;

        if (alongX == true)
        {
            lengthUnits = deltaX;
            center = new Vector3((from.x + to.x) * 0.5f, 0f, from.z);
            rotation = Quaternion.identity;
        }
        else
        {
            lengthUnits = deltaZ;
            center = new Vector3(from.x, 0f, (from.z + to.z) * 0.5f);
            rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        float minLengthUnits = endInsetUnits * 2f;

        if (lengthUnits <= minLengthUnits)
        {
            throw new InvalidOperationException(nameof(lengthUnits));
        }

        lengthUnits = lengthUnits - minLengthUnits;

        float doorSurfaceY = (fromDoor.transform.position.y + toDoor.transform.position.y) * 0.5f;

        GameObject corridorRoot = new GameObject("Corridor");
        corridorRoot.transform.position = center;
        corridorRoot.transform.rotation = rotation;
        corridorRoot.transform.SetParent(parent, true);

        float floorLengthUnits = GetFloorLengthUnits(lengthUnits, horizontalScale);
        GameObject floor = BuildFloor(corridorRoot.transform, floorLengthUnits, widthUnits, horizontalScale, verticalScale);

        if (_wallPrefab != null)
        {
            if (_postPrefab == null && ShouldCreateWallPosts() == true)
            {
                throw new InvalidOperationException(nameof(_postPrefab));
            }

            ResolveWallVisualPrefabs();

            if (ShouldCreateWallPosts() == true)
            {
                ResolveWallPostVisualPrefab();
            }

            float floorSurfaceY = GetFloorSurfaceLocalY(corridorRoot.transform, floor);
            BuildWalls(corridorRoot.transform, lengthUnits, widthUnits, floorSurfaceY, horizontalScale, verticalScale);
        }

        AlignCorridorHeight(corridorRoot.transform, floor, doorSurfaceY);
    }

    private GameObject BuildFloor(Transform corridorTransform, float lengthUnits, float widthUnits, float horizontalScale, float verticalScale)
    {
        Vector2 baseSizeInUnits = GetFloorBaseSizeInUnits();

        float baseLength = baseSizeInUnits.x;
        float baseWidth = baseSizeInUnits.y;

        if (baseLength < 0.01f)
        {
            baseLength = 0.01f;
        }

        if (baseWidth < 0.01f)
        {
            baseWidth = 0.01f;
        }

        GameObject floor = Instantiate(_floorPrefab, corridorTransform);

        floor.transform.localPosition = ScaleLocalOffset(_floorLocalOffset, horizontalScale, verticalScale);
        floor.transform.localRotation = Quaternion.identity;

        Vector3 baseScale = floor.transform.localScale;

        float scaleX = lengthUnits / baseLength;
        float scaleZ = widthUnits / baseWidth;

        floor.transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y, baseScale.z * scaleZ);
        ApplyFloorTextureScale(floor, scaleX, scaleZ);

        EnableColliders(floor);

        return floor;
    }

    private float GetFloorLengthUnits(float lengthUnits, float horizontalScale)
    {
        float trimUnits = _floorEndTrimInBlocks * _blockSize * horizontalScale;
        float floorLengthUnits = lengthUnits - (trimUnits * 2f);

        if (floorLengthUnits < MinSize)
        {
            return MinSize;
        }

        return floorLengthUnits;
    }

    private void ApplyFloorTextureScale(GameObject floor, float scaleX, float scaleZ)
    {
        Renderer[] renderers = floor.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            throw new InvalidOperationException(nameof(renderers));
        }

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer floorRenderer = renderers[rendererIndex];

            if (floorRenderer == null)
            {
                continue;
            }

            Material material = floorRenderer.sharedMaterial;

            if (material == null)
            {
                continue;
            }

            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            floorRenderer.GetPropertyBlock(propertyBlock);

        float textureScaleX = scaleX * _floorTextureTilingMultiplier;
        float textureScaleZ = scaleZ * _floorTextureTilingMultiplier;

        ApplyTextureScale(propertyBlock, material, "_MainTex", s_mainTexStId, textureScaleX, textureScaleZ);
        ApplyTextureScale(propertyBlock, material, "_BaseMap", s_baseMapStId, textureScaleX, textureScaleZ);
        ApplyTextureScale(propertyBlock, material, "_ColorMap", s_colorMapStId, textureScaleX, textureScaleZ);
        ApplyTextureScale(propertyBlock, material, "_NormalMap", s_normalMapStId, textureScaleX, textureScaleZ);

            floorRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ApplyTextureScale(MaterialPropertyBlock propertyBlock, Material material, string texturePropertyName, int textureStId, float scaleX, float scaleZ)
    {
        if (material.HasProperty(texturePropertyName) == false)
        {
            return;
        }

        Vector2 textureScale = material.GetTextureScale(texturePropertyName);
        Vector2 textureOffset = material.GetTextureOffset(texturePropertyName);

        propertyBlock.SetVector(textureStId, new Vector4(
            textureScale.x * scaleX,
            textureScale.y * scaleZ,
            textureOffset.x,
            textureOffset.y
        ));
    }

    private Vector2 GetFloorBaseSizeInUnits()
    {
        if (_autoDetectFloorPrefabSize == false)
        {
            return _floorPrefabSizeInUnits;
        }

        if (_hasCachedFloorSize == true)
        {
            return _cachedFloorSizeInUnits;
        }

        GameObject probe = Instantiate(_floorPrefab);
        probe.hideFlags = HideFlags.HideAndDontSave;

        Renderer[] renderers = probe.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            DestroyProbe(probe);
            throw new InvalidOperationException(nameof(renderers));
        }


        Bounds bounds = renderers[0].bounds;

        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }


        Vector2 sizeInUnits = new Vector2(bounds.size.x, bounds.size.z);

        DestroyProbe(probe);

        _cachedFloorSizeInUnits = sizeInUnits;
        _hasCachedFloorSize = true;

        return _cachedFloorSizeInUnits;
    }

    private void DestroyProbe(GameObject probe)
    {
        if (probe == null)
        {
            return;
        }

        if (Application.isPlaying == true)
        {
            Destroy(probe);
            return;
        }

        DestroyImmediate(probe);
    }

    private float GetFloorSurfaceLocalY(Transform corridorTransform, GameObject floor)
    {
        if (floor == null)
        {
            throw new InvalidOperationException(nameof(floor));
        }

        Collider[] floorColliders = floor.GetComponentsInChildren<Collider>(true);

        if (floorColliders != null && floorColliders.Length > 0)
        {
            float highestFloorY = float.MinValue;

            for (int colliderIndex = 0; colliderIndex < floorColliders.Length; colliderIndex++)
            {
                Collider floorCollider = floorColliders[colliderIndex];

                if (floorCollider == null)
                {
                    continue;
                }

                float localY = corridorTransform.InverseTransformPoint(floorCollider.bounds.max).y;

                if (localY > highestFloorY)
                {
                    highestFloorY = localY;
                }
            }

            if (highestFloorY == float.MinValue)
            {
                throw new InvalidOperationException(nameof(highestFloorY));
            }

            return highestFloorY;
        }

        Renderer[] floorRenderers = floor.GetComponentsInChildren<Renderer>(true);

        if (floorRenderers == null || floorRenderers.Length == 0)
        {
            throw new InvalidOperationException(nameof(floorRenderers));
        }

        float highestRendererY = float.MinValue;

        for (int rendererIndex = 0; rendererIndex < floorRenderers.Length; rendererIndex++)
        {
            Renderer floorRenderer = floorRenderers[rendererIndex];

            if (floorRenderer == null)
            {
                continue;
            }

            float localY = corridorTransform.InverseTransformPoint(floorRenderer.bounds.max).y;

            if (localY > highestRendererY)
            {
                highestRendererY = localY;
            }
        }

        if (highestRendererY == float.MinValue)
        {
            throw new InvalidOperationException(nameof(highestRendererY));
        }

        return highestRendererY;
    }

    private void BuildWalls(Transform corridorTransform, float lengthUnits, float widthUnits, float floorSurfaceY, float horizontalScale, float verticalScale)
    {
        float halfWidth = widthUnits * 0.5f;
        float sideOffset = halfWidth + (_wallSideOffsetInUnits * horizontalScale);
        int segmentCount = GetWallSegmentCount(lengthUnits, horizontalScale);

        if (_wallPostPlacement != WallPostPlacement.EveryFenceSegment)
        {
            segmentCount = 1;
        }

        BuildWallSide(corridorTransform, lengthUnits, sideOffset, Vector3.back, segmentCount, floorSurfaceY, horizontalScale, verticalScale);

        BuildWallSide(corridorTransform, lengthUnits, -sideOffset, Vector3.forward, segmentCount, floorSurfaceY, horizontalScale, verticalScale);
    }

    private void BuildWallSide(Transform corridorTransform, float lengthUnits, float localZ, Vector3 inwardDirection, int segmentCount, float floorSurfaceY, float horizontalScale, float verticalScale)
    {
        float halfLength = lengthUnits * 0.5f;

        if (_wallPostPlacement == WallPostPlacement.EveryFenceSegment)
        {
            for (int postIndex = 0; postIndex <= segmentCount; postIndex++)
            {
                if (ShouldBuildWallPostAtIndex(postIndex, segmentCount) == false)
                {
                    continue;
                }

                float postRatio = (float)postIndex / segmentCount;
                float localX = Mathf.Lerp(-halfLength, halfLength, postRatio);

                BuildPost(corridorTransform, localX, localZ, inwardDirection, floorSurfaceY, horizontalScale, verticalScale);
            }
        }

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            float startRatio = (float)segmentIndex / segmentCount;
            float endRatio = (float)(segmentIndex + 1) / segmentCount;
            float startX = Mathf.Lerp(-halfLength, halfLength, startRatio);
            float endX = Mathf.Lerp(-halfLength, halfLength, endRatio);
            bool keepStartGap = ShouldBuildWallPostAtIndex(segmentIndex, segmentCount);
            bool keepEndGap = ShouldBuildWallPostAtIndex(segmentIndex + 1, segmentCount);

            BuildWallSegment(corridorTransform, startX, endX, localZ, inwardDirection, floorSurfaceY, horizontalScale, verticalScale, keepStartGap, keepEndGap);
        }
    }

    private void BuildPost(Transform corridorTransform, float localX, float localZ, Vector3 inwardDirection, float floorSurfaceY, float horizontalScale, float verticalScale)
    {
        GameObject post = Instantiate(_postPrefab, corridorTransform);
        Vector3 wallLocalOffset = ScaleLocalOffset(_wallLocalOffset, horizontalScale, verticalScale);
        post.transform.localPosition = new Vector3(localX, 0f, localZ) + wallLocalOffset;
        post.transform.localRotation = GetWallRotation(inwardDirection);

        Vector3 baseScale = post.transform.localScale;

        float baseHeight = _postPrefabHeightInUnits;

        if (baseHeight < MinSize)
        {
            baseHeight = MinSize;
        }

        float scaleX = _blockSize * horizontalScale;
        float scaleY = (_wallHeightInUnits * verticalScale) / baseHeight;
        float scaleZ = _blockSize * horizontalScale;

        post.transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z * scaleZ);

        if (_resolvedWallPostVisualPrefab != null)
        {
            HideBlockRenderers(post);
            CreateWallPostVisual(corridorTransform, localX, localZ, inwardDirection, floorSurfaceY, horizontalScale, verticalScale);
        }

        EnableColliders(post);
    }

    private void CreateWallPostVisual(Transform corridorTransform, float localX, float localZ, Vector3 inwardDirection, float floorSurfaceY, float horizontalScale, float verticalScale)
    {
        GameObject visualInstance = Instantiate(_resolvedWallPostVisualPrefab, corridorTransform);

        Bounds localBounds;
        bool hasBounds = TryGetLocalRendererBounds(visualInstance.transform, out localBounds);

        if (hasBounds == false)
        {
            throw new InvalidOperationException(nameof(Renderer));
        }

        Vector3 targetSize = GetWallPostVisualTargetSize(horizontalScale, verticalScale);
        float sourceThickness = Mathf.Max(localBounds.size.x, localBounds.size.z);
        float maximumStretch = GetWallVisualMaximumStretch();
        float uniformHorizontalScale = GetVisualAxisScale(targetSize.x, sourceThickness);
        float visualVerticalScale = GetVisualAxisScale(targetSize.y, localBounds.size.y);
        float minimumHorizontalScale = visualVerticalScale / maximumStretch;
        float maximumHorizontalScale = visualVerticalScale * maximumStretch;

        if (uniformHorizontalScale < minimumHorizontalScale)
        {
            uniformHorizontalScale = minimumHorizontalScale;
        }

        if (uniformHorizontalScale > maximumHorizontalScale)
        {
            uniformHorizontalScale = maximumHorizontalScale;
        }

        float visualHeight = localBounds.size.y * visualVerticalScale;
        Vector3 axisScale = new Vector3(
            uniformHorizontalScale,
            visualVerticalScale,
            uniformHorizontalScale
        );
        Quaternion visualRotation = GetWallRotation(inwardDirection) * Quaternion.Euler(0f, _wallPostVisualYawOffset, 0f);
        Vector3 localPosition = new Vector3(
            localX,
            floorSurfaceY + (visualHeight * 0.5f),
            localZ
        );

        visualInstance.transform.localScale = Vector3.Scale(visualInstance.transform.localScale, axisScale);
        visualInstance.transform.localRotation = visualRotation;
        visualInstance.transform.localPosition = localPosition - (visualRotation * Vector3.Scale(localBounds.center, axisScale));
    }

    private Vector3 GetWallPostVisualTargetSize(float horizontalScale, float verticalScale)
    {
        float heightMultiplier = _wallPostVisualHeightMultiplier;

        if (heightMultiplier < MinVisualSize)
        {
            heightMultiplier = MinVisualSize;
        }

        float thicknessMultiplier = _wallPostVisualThicknessMultiplier;

        if (thicknessMultiplier < MinVisualSize)
        {
            thicknessMultiplier = MinVisualSize;
        }

        float thickness = _blockSize * horizontalScale * thicknessMultiplier;
        float height = _wallHeightInUnits * verticalScale * heightMultiplier;

        return new Vector3(thickness, height, thickness);
    }

    private void BuildWallSegment(Transform corridorTransform, float startX, float endX, float localZ, Vector3 inwardDirection, float floorSurfaceY, float horizontalScale, float verticalScale, bool keepStartGap, bool keepEndGap)
    {
        float segmentLength = Mathf.Abs(endX - startX);
        float startGap = 0f;
        float endGap = 0f;

        if (keepStartGap == true)
        {
            startGap = _wallSegmentEndGapInBlocks * _blockSize * horizontalScale;
        }

        if (keepEndGap == true)
        {
            endGap = _wallSegmentEndGapInBlocks * _blockSize * horizontalScale;
        }

        float finalLength = segmentLength - startGap - endGap;

        if (finalLength <= MinSize)
        {
            return;
        }

        GameObject wall = Instantiate(_wallPrefab, corridorTransform);
        Vector3 wallLocalOffset = ScaleLocalOffset(_wallLocalOffset, horizontalScale, verticalScale);
        float localStartX = startX + startGap;
        float localEndX = endX - endGap;
        float localX = (localStartX + localEndX) * 0.5f;

        wall.transform.localPosition = new Vector3(localX, 0f, localZ) + wallLocalOffset;
        wall.transform.localRotation = GetWallRotation(inwardDirection);

        Vector3 baseScale = wall.transform.localScale;
        float baseLength = _wallPrefabLengthInUnits;
        float baseHeight = _wallPrefabHeightInUnits;

        if (baseLength < MinSize)
        {
            baseLength = MinSize;
        }

        if (baseHeight < MinSize)
        {
            baseHeight = MinSize;
        }

        float scaleX = _blockSize * horizontalScale;
        float scaleY = (_wallHeightInUnits * verticalScale) / baseHeight;
        float scaleZ = finalLength / baseLength;

        wall.transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z * scaleZ);

        GameObject visualPrefab = GetWallVisualPrefab(startX, endX, localZ);

        if (visualPrefab != null)
        {
            Vector3 visualTargetSize = GetWallVisualTargetSize(scaleX, finalLength, horizontalScale, verticalScale);
            Vector3 visualPosition = wall.transform.localPosition;
            visualPosition.y = floorSurfaceY + (_wallVisualFloorOffsetInUnits * verticalScale) + (visualTargetSize.y * 0.5f);

            HideBlockRenderers(wall);
            CreateWallVisuals(visualPrefab, corridorTransform, visualPosition, wall.transform.localRotation, visualTargetSize);
        }

        EnableColliders(wall);
    }

    private Vector3 GetWallVisualTargetSize(float baseThickness, float length, float horizontalScale, float verticalScale)
    {
        float heightMultiplier = _wallVisualHeightMultiplier;

        if (heightMultiplier < MinVisualSize)
        {
            heightMultiplier = MinVisualSize;
        }

        float thicknessMultiplier = _wallVisualThicknessMultiplier;

        if (thicknessMultiplier < MinVisualSize)
        {
            thicknessMultiplier = MinVisualSize;
        }

        float visualThickness = baseThickness * thicknessMultiplier;
        float visualHeight = (_wallHeightInUnits * verticalScale) * heightMultiplier;

        return new Vector3(visualThickness, visualHeight, length);
    }

    private GameObject GetWallVisualPrefab(float startX, float endX, float localZ)
    {
        if (_resolvedWallVisualPrefabs.Count == 0)
        {
            return null;
        }

        int startHash = Mathf.RoundToInt(startX * 10f);
        int endHash = Mathf.RoundToInt(endX * 10f);
        int zHash = Mathf.RoundToInt(localZ * 10f);
        int index = Mathf.Abs((startHash * 17) + (endHash * 31) + zHash);
        index %= _resolvedWallVisualPrefabs.Count;

        return _resolvedWallVisualPrefabs[index];
    }

    private void ResolveWallVisualPrefabs()
    {
        _resolvedWallVisualPrefabs.Clear();

        for (int prefabIndex = 0; prefabIndex < _wallVisualPrefabs.Count; prefabIndex++)
        {
            GameObject prefab = _wallVisualPrefabs[prefabIndex];

            if (prefab == null)
            {
                continue;
            }

            _resolvedWallVisualPrefabs.Add(prefab);
        }

        for (int pathIndex = 0; pathIndex < _wallVisualResourcePaths.Count; pathIndex++)
        {
            string resourcePath = _wallVisualResourcePaths[pathIndex];

            if (string.IsNullOrWhiteSpace(resourcePath) == true)
            {
                throw new InvalidOperationException(nameof(_wallVisualResourcePaths));
            }

            GameObject prefab = Resources.Load<GameObject>(resourcePath);

            if (prefab == null)
            {
                throw new InvalidOperationException(nameof(_wallVisualResourcePaths));
            }

            _resolvedWallVisualPrefabs.Add(prefab);
        }
    }

    private void ResolveWallPostVisualPrefab()
    {
        _resolvedWallPostVisualPrefab = _wallPostVisualPrefab;

        if (_resolvedWallPostVisualPrefab != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_wallPostVisualResourcePath) == true)
        {
            return;
        }

        _resolvedWallPostVisualPrefab = Resources.Load<GameObject>(_wallPostVisualResourcePath);

        if (_resolvedWallPostVisualPrefab == null)
        {
            throw new InvalidOperationException(nameof(_wallPostVisualResourcePath));
        }
    }

    private bool ShouldCreateWallPosts()
    {
        return _wallPostPlacement == WallPostPlacement.EveryFenceSegment;
    }

    private bool ShouldBuildWallPostAtIndex(int postIndex, int segmentCount)
    {
        if (ShouldCreateWallPosts() == false)
        {
            return false;
        }

        if (postIndex <= 0)
        {
            return false;
        }

        if (postIndex >= segmentCount)
        {
            return false;
        }

        return true;
    }

    private void CreateWallVisuals(GameObject visualPrefab, Transform rootTransform, Vector3 localPosition, Quaternion localRotation, Vector3 targetSize)
    {
        GameObject firstVisualInstance = Instantiate(visualPrefab, rootTransform);

        Bounds localBounds;
        bool hasBounds = TryGetLocalRendererBounds(firstVisualInstance.transform, out localBounds);

        if (hasBounds == false)
        {
            throw new InvalidOperationException(nameof(Renderer));
        }

        WallVisualMetrics metrics = new WallVisualMetrics(localBounds);
        float uniformScale = GetWallVisualUniformScale(metrics, targetSize);
        int pieceCount = GetWallVisualPieceCount(metrics, targetSize.z, uniformScale);
        float pieceLength = targetSize.z / pieceCount;
        Vector3 pieceTargetSize = new Vector3(targetSize.x, targetSize.y, pieceLength);
        float startOffset = targetSize.z * -0.5f;

        for (int pieceIndex = 0; pieceIndex < pieceCount; pieceIndex++)
        {
            GameObject visualInstance = firstVisualInstance;

            if (pieceIndex > 0)
            {
                visualInstance = Instantiate(visualPrefab, rootTransform);
            }

            float pieceOffset = startOffset + (pieceLength * (pieceIndex + 0.5f));
            Vector3 piecePosition = localPosition + (localRotation * new Vector3(0f, 0f, pieceOffset));
            ConfigureWallVisual(visualInstance, piecePosition, localRotation, pieceTargetSize, metrics, uniformScale);
        }
    }

    private int GetWallVisualPieceCount(WallVisualMetrics metrics, float targetLength, float uniformScale)
    {
        float sourceLength = metrics.SourceLength;

        if (sourceLength < MinVisualSize)
        {
            return 1;
        }

        float maximumStretch = GetWallVisualMaximumStretch();
        float maximumPieceLength = sourceLength * maximumStretch;

        if (maximumPieceLength < MinVisualSize)
        {
            return 1;
        }

        float uniformPieceLength = sourceLength * uniformScale;

        if (uniformPieceLength < MinVisualSize)
        {
            uniformPieceLength = maximumPieceLength;
        }

        int pieceCount = Mathf.Max(1, Mathf.RoundToInt(targetLength / uniformPieceLength));
        float pieceScale = GetVisualAxisScale(targetLength / pieceCount, sourceLength);

        if (pieceScale > maximumStretch)
        {
            pieceCount = Mathf.CeilToInt(targetLength / maximumPieceLength);
        }

        return Mathf.Max(1, pieceCount);
    }

    private void ConfigureWallVisual(GameObject visualInstance, Vector3 localPosition, Quaternion localRotation, Vector3 targetSize, WallVisualMetrics metrics, float uniformScale)
    {
        Vector3 baseScale = visualInstance.transform.localScale;
        Vector3 axisScale = GetWallVisualAxisScale(metrics.LocalBounds.size, targetSize, metrics.LengthAlongX, uniformScale);
        Vector3 visualScale = new Vector3(
            baseScale.x * axisScale.x,
            baseScale.y * axisScale.y,
            baseScale.z * axisScale.z
        );
        Quaternion axisRotation = GetWallVisualAxisRotation(metrics.LengthAlongX);
        Quaternion visualRotation = localRotation * axisRotation * Quaternion.Euler(0f, _wallVisualYawOffset, 0f);

        visualInstance.transform.localScale = visualScale;
        visualInstance.transform.localRotation = visualRotation;
        visualInstance.transform.localPosition = localPosition - (visualRotation * Vector3.Scale(metrics.LocalBounds.center, axisScale));
    }

    private Vector3 GetWallVisualAxisScale(Vector3 sourceSize, Vector3 targetSize, bool lengthAlongX, float uniformScale)
    {
        if (lengthAlongX == true)
        {
            return new Vector3(
                GetVisualAxisScale(targetSize.z, sourceSize.x),
                uniformScale,
                uniformScale
            );
        }

        return new Vector3(
            uniformScale,
            uniformScale,
            GetVisualAxisScale(targetSize.z, sourceSize.z)
        );
    }

    private Quaternion GetWallVisualAxisRotation(bool lengthAlongX)
    {
        if (lengthAlongX == true)
        {
            return Quaternion.Euler(0f, -90f, 0f);
        }

        return Quaternion.identity;
    }

    private bool TryGetLocalRendererBounds(Transform rootTransform, out Bounds localBounds)
    {
        Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
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

    private float GetVisualAxisScale(float targetSize, float sourceSize)
    {
        if (sourceSize < MinVisualSize)
        {
            return 1f;
        }

        return targetSize / sourceSize;
    }

    private float GetWallVisualUniformScale(WallVisualMetrics metrics, Vector3 targetSize)
    {
        float scale = GetVisualAxisScale(targetSize.y, metrics.LocalBounds.size.y);
        float maximumStretch = GetWallVisualMaximumStretch();

        if (scale > maximumStretch)
        {
            return maximumStretch;
        }

        return scale;
    }

    private float GetWallVisualMaximumStretch()
    {
        float maximumStretch = _wallVisualMaximumStretch;

        if (maximumStretch < 1f)
        {
            maximumStretch = 1f;
        }

        return maximumStretch;
    }

    private void HideBlockRenderers(GameObject blockInstance)
    {
        if (_hideWallBlockRenderersWhenVisualsAssigned == false)
        {
            return;
        }

        Renderer[] renderers = blockInstance.GetComponentsInChildren<Renderer>(true);

        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];

            if (renderer == null)
            {
                continue;
            }

            renderer.enabled = false;
        }
    }

    private int GetWallSegmentCount(float lengthUnits, float horizontalScale)
    {
        float postSpacing = _postSpacingInBlocks * _blockSize * horizontalScale;

        if (postSpacing < MinSize)
        {
            postSpacing = MinSize;
        }

        return Mathf.Max(1, Mathf.CeilToInt(lengthUnits / postSpacing));
    }

    private Quaternion GetWallRotation(Vector3 inwardDirection)
    {
        return Quaternion.LookRotation(inwardDirection, Vector3.up) * Quaternion.Euler(0f, WallFaceYawOffset, 0f);
    }

    private float GetHorizontalScale(Transform parent)
    {
        Vector3 lossyScale = parent.lossyScale;
        float horizontalScale = (Mathf.Abs(lossyScale.x) + Mathf.Abs(lossyScale.z)) * 0.5f;

        if (horizontalScale <= 0.0001f)
        {
            return 1f;
        }

        return horizontalScale;
    }

    private float GetVerticalScale(Transform parent)
    {
        float verticalScale = Mathf.Abs(parent.lossyScale.y);

        if (verticalScale <= 0.0001f)
        {
            return 1f;
        }

        return verticalScale;
    }

    private Vector3 ScaleLocalOffset(Vector3 localOffset, float horizontalScale, float verticalScale)
    {
        float scaledOffsetX = localOffset.x * horizontalScale;
        float scaledOffsetY = localOffset.y * verticalScale;
        float scaledOffsetZ = localOffset.z * horizontalScale;

        return new Vector3(scaledOffsetX, scaledOffsetY, scaledOffsetZ);
    }

    private void EnableColliders(GameObject targetObject)
    {
        if (_enableColliders == false)
        {
            return;
        }

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);

        if (colliders != null && colliders.Length > 0)
        {
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider collider = colliders[colliderIndex];

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = true;
            }

            return;
        }

        BoxCollider newCollider = targetObject.AddComponent<BoxCollider>();
        newCollider.enabled = true;
    }

    private void AlignCorridorHeight(Transform corridorTransform, GameObject floor, float doorSurfaceY)
    {
        if (floor == null)
        {
            throw new InvalidOperationException(nameof(floor));
        }

        Collider[] floorColliders = floor.GetComponentsInChildren<Collider>(true);

        if (floorColliders != null && floorColliders.Length > 0)
        {
            float highestFloorY = float.MinValue;

            for (int colliderIndex = 0; colliderIndex < floorColliders.Length; colliderIndex++)
            {
                Collider floorCollider = floorColliders[colliderIndex];

                if (floorCollider == null)
                {
                    continue;
                }

                float currentTopY = floorCollider.bounds.max.y;

                if (currentTopY > highestFloorY)
                {
                    highestFloorY = currentTopY;
                }
            }

            if (highestFloorY == float.MinValue)
            {
                throw new InvalidOperationException(nameof(highestFloorY));
            }

            float shiftY = doorSurfaceY - highestFloorY;
            Vector3 corridorPosition = corridorTransform.position;
            corridorPosition.y = corridorPosition.y + shiftY;
            corridorTransform.position = corridorPosition;

            return;
        }

        Renderer[] floorRenderers = floor.GetComponentsInChildren<Renderer>(true);

        if (floorRenderers == null || floorRenderers.Length == 0)
        {
            throw new InvalidOperationException(nameof(floorRenderers));
        }

        float highestRendererY = float.MinValue;

        for (int rendererIndex = 0; rendererIndex < floorRenderers.Length; rendererIndex++)
        {
            Renderer floorRenderer = floorRenderers[rendererIndex];

            if (floorRenderer == null)
            {
                continue;
            }

            float currentTopY = floorRenderer.bounds.max.y;

            if (currentTopY > highestRendererY)
            {
                highestRendererY = currentTopY;
            }
        }

        if (highestRendererY == float.MinValue)
        {
            throw new InvalidOperationException(nameof(highestRendererY));
        }

        float renderShiftY = doorSurfaceY - highestRendererY;
        Vector3 renderCorridorPosition = corridorTransform.position;
        renderCorridorPosition.y = renderCorridorPosition.y + renderShiftY;
        corridorTransform.position = renderCorridorPosition;
    }
}

using System;
using UnityEngine;

public sealed class LevelCorridorBuilder : MonoBehaviour
{
    private const float WallFaceYawOffset = -90f;
    private const float MinSize = 0.01f;

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

    [Header("Walls (optional)")]
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _postPrefab;

    [SerializeField, Min(0.01f)] private float _wallPrefabLengthInUnits = 1f;
    [SerializeField, Min(0.01f)] private float _wallPrefabHeightInUnits = 1f;
    [SerializeField, Min(0.01f)] private float _postPrefabHeightInUnits = 1f;

    [SerializeField, Min(0f)] private float _wallHeightInUnits = 2f;
    [SerializeField, Min(0f)] private float _wallSideOffsetInUnits = 0f;
    [SerializeField, Min(1)] private int _postSpacingInBlocks = 6;
    [SerializeField, Min(0f)] private float _wallSegmentEndGapInBlocks = 0.5f;

    [SerializeField] private Vector3 _wallLocalOffset;
    [SerializeField] private bool _enableColliders = true;

    private bool _hasCachedFloorSize;
    private Vector2 _cachedFloorSizeInUnits;

    public float BlockSize => _blockSize;

    public void BuildBetweenDoors(Transform parent, RoomDoorMarker fromDoor, RoomDoorMarker toDoor, int corridorWidthInBlocks)
    {
        if (parent == null)
            throw new InvalidOperationException(nameof(parent));

        if (fromDoor == null)
            throw new InvalidOperationException(nameof(fromDoor));

        if (toDoor == null)
            throw new InvalidOperationException(nameof(toDoor));

        if (_floorPrefab == null)
            throw new InvalidOperationException(nameof(_floorPrefab));


        if (corridorWidthInBlocks < 1)
            corridorWidthInBlocks = 1;


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
                throw new InvalidOperationException("Corridor must be strictly straight (same X or same Z).");
        }
        else
        {
            if (deltaX > 0.001f)
                throw new InvalidOperationException("Corridor must be strictly straight (same X or same Z).");
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
            throw new InvalidOperationException(nameof(lengthUnits));

        lengthUnits = lengthUnits - minLengthUnits;


        float doorSurfaceY = (fromDoor.transform.position.y + toDoor.transform.position.y) * 0.5f;

        GameObject corridorRoot = new GameObject("Corridor");
        corridorRoot.transform.position = center;
        corridorRoot.transform.rotation = rotation;
        corridorRoot.transform.SetParent(parent, true);

        GameObject floor = BuildFloor(corridorRoot.transform, lengthUnits, widthUnits, horizontalScale, verticalScale);

        if (_wallPrefab != null)
        {
            if (_postPrefab == null)
            {
                throw new InvalidOperationException(nameof(_postPrefab));
            }

            BuildWalls(corridorRoot.transform, lengthUnits, widthUnits, horizontalScale, verticalScale);
        }

        AlignCorridorHeight(corridorRoot.transform, floor, doorSurfaceY);
    }

    private GameObject BuildFloor(Transform corridorTransform, float lengthUnits, float widthUnits, float horizontalScale, float verticalScale)
    {
        Vector2 baseSizeInUnits = GetFloorBaseSizeInUnits();

        float baseLength = baseSizeInUnits.x;
        float baseWidth = baseSizeInUnits.y;

        if (baseLength < 0.01f)
            baseLength = 0.01f;

        if (baseWidth < 0.01f)
            baseWidth = 0.01f;


        GameObject floor = Instantiate(_floorPrefab, corridorTransform);

        floor.transform.localPosition = ScaleLocalOffset(_floorLocalOffset, horizontalScale, verticalScale);
        floor.transform.localRotation = Quaternion.identity;

        Vector3 baseScale = floor.transform.localScale;

        float scaleX = lengthUnits / baseLength;
        float scaleZ = widthUnits / baseWidth;

        floor.transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y, baseScale.z * scaleZ);

        EnableColliders(floor);

        return floor;
    }

    private Vector2 GetFloorBaseSizeInUnits()
    {
        if (_autoDetectFloorPrefabSize == false)
            return _floorPrefabSizeInUnits;


        if (_hasCachedFloorSize == true)
            return _cachedFloorSizeInUnits;


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
            return;


        if (Application.isPlaying == true)
        {
            Destroy(probe);
            return;
        }

        DestroyImmediate(probe);
    }

    private void BuildWalls(Transform corridorTransform, float lengthUnits, float widthUnits, float horizontalScale, float verticalScale)
    {
        float halfWidth = widthUnits * 0.5f;
        float sideOffset = halfWidth + (_wallSideOffsetInUnits * horizontalScale);
        int segmentCount = GetWallSegmentCount(lengthUnits, horizontalScale);

        BuildWallSide(corridorTransform, lengthUnits, sideOffset, Vector3.back, segmentCount, horizontalScale, verticalScale);

        BuildWallSide(corridorTransform, lengthUnits, -sideOffset, Vector3.forward, segmentCount, horizontalScale, verticalScale);
    }

    private void BuildWallSide(Transform corridorTransform, float lengthUnits, float localZ, Vector3 inwardDirection, int segmentCount, float horizontalScale, float verticalScale)
    {
        float halfLength = lengthUnits * 0.5f;

        for (int postIndex = 0; postIndex <= segmentCount; postIndex++)
        {
            float postRatio = (float)postIndex / segmentCount;
            float localX = Mathf.Lerp(-halfLength, halfLength, postRatio);

            BuildPost(corridorTransform, localX, localZ, inwardDirection, horizontalScale, verticalScale);
        }

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            float startRatio = (float)segmentIndex / segmentCount;
            float endRatio = (float)(segmentIndex + 1) / segmentCount;
            float startX = Mathf.Lerp(-halfLength, halfLength, startRatio);
            float endX = Mathf.Lerp(-halfLength, halfLength, endRatio);

            BuildWallSegment(corridorTransform, startX, endX, localZ, inwardDirection, horizontalScale, verticalScale);
        }
    }

    private void BuildPost(Transform corridorTransform, float localX, float localZ, Vector3 inwardDirection, float horizontalScale, float verticalScale)
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

        EnableColliders(post);
    }

    private void BuildWallSegment(Transform corridorTransform, float startX, float endX, float localZ, Vector3 inwardDirection, float horizontalScale, float verticalScale)
    {
        float segmentLength = Mathf.Abs(endX - startX);
        float endGap = _wallSegmentEndGapInBlocks * _blockSize * horizontalScale;
        float finalLength = segmentLength - (endGap * 2f);

        if (finalLength <= MinSize)
        {
            return;
        }

        GameObject wall = Instantiate(_wallPrefab, corridorTransform);
        Vector3 wallLocalOffset = ScaleLocalOffset(_wallLocalOffset, horizontalScale, verticalScale);
        float localX = (startX + endX) * 0.5f;

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

        EnableColliders(wall);
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
            throw new InvalidOperationException(nameof(floor));

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
                throw new InvalidOperationException(nameof(highestFloorY));

            float shiftY = doorSurfaceY - highestFloorY;
            Vector3 corridorPosition = corridorTransform.position;
            corridorPosition.y = corridorPosition.y + shiftY;
            corridorTransform.position = corridorPosition;

            return;
        }

        Renderer[] floorRenderers = floor.GetComponentsInChildren<Renderer>(true);

        if (floorRenderers == null || floorRenderers.Length == 0)
            throw new InvalidOperationException(nameof(floorRenderers));

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
            throw new InvalidOperationException(nameof(highestRendererY));

        float renderShiftY = doorSurfaceY - highestRendererY;
        Vector3 renderCorridorPosition = corridorTransform.position;
        renderCorridorPosition.y = renderCorridorPosition.y + renderShiftY;
        corridorTransform.position = renderCorridorPosition;
    }
}

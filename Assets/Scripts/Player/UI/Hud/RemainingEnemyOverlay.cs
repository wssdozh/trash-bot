using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RemainingEnemyOverlay : MonoBehaviour
{
    private const int VisibleThreatThreshold = 3;
    private const int SearchThreatCount = VisibleThreatThreshold + 1;
    private const float SearchInterval = 0.2f;
    private const float HeightOffset = 1.35f;
    private const float ScreenMargin = 92f;
    private const float ScreenTransitionViewportPadding = 0.06f;
    private const int RingTextureSize = 64;
    private const int DotTextureSize = 32;
    private const int ArrowTextureSize = 48;
    private static readonly Color FillColor = new Color(0.28f, 0.06f, 0.06f, 0.44f);
    private static readonly Color RingColor = new Color(0.82f, 0.24f, 0.20f, 0.95f);
    private static readonly Color DotColor = new Color(0.90f, 0.40f, 0.32f, 1f);
    private static readonly Color ArrowColor = new Color(0.82f, 0.24f, 0.20f, 0.95f);

    private readonly List<Transform> _threats = new List<Transform>(SearchThreatCount);
    private readonly List<RemainingEnemyIndicatorView> _indicators = new List<RemainingEnemyIndicatorView>(VisibleThreatThreshold);

    private Transform _playerTransform;
    private RectTransform _uiRoot;
    private RectTransform _overlayRoot;
    private Canvas _canvas;
    private RoomCombatLock _roomCombatLock;
    private Camera _worldCamera;
    private Camera _uiCamera;
    private Texture2D _ringTexture;
    private Texture2D _dotTexture;
    private Texture2D _arrowTexture;
    private Sprite _ringSprite;
    private Sprite _dotSprite;
    private Sprite _arrowSprite;
    private float _searchTimer;

    public void Initialize(Transform playerTransform, RectTransform uiRoot)
    {
        if (playerTransform == null)
        {
            throw new InvalidOperationException(nameof(playerTransform));
        }

        if (uiRoot == null)
        {
            throw new InvalidOperationException(nameof(uiRoot));
        }

        _playerTransform = playerTransform;
        _uiRoot = uiRoot;
        _canvas = uiRoot.GetComponentInParent<Canvas>();

        if (_canvas == null)
        {
            throw new InvalidOperationException(nameof(_canvas));
        }

        BuildSprites();
        BuildOverlayRoot();
        ResolveUiCamera();
        HideIndicators();
    }

    private void OnDisable()
    {
        HideIndicators();
    }

    private void OnDestroy()
    {
        DestroySpriteResources();
    }

    private void LateUpdate()
    {
        if (_overlayRoot == null)
        {
            return;
        }

        ResolveWorldCamera();

        if (_worldCamera == null)
        {
            HideIndicators();

            return;
        }

        TickRoomCombatLock();
        UpdateIndicators();
    }

    private void TickRoomCombatLock()
    {
        bool shouldRefresh = HasValidRoomCombatLock() == false;

        if (shouldRefresh == false)
        {
            _searchTimer -= Time.unscaledDeltaTime;

            if (_searchTimer <= 0f)
            {
                shouldRefresh = true;
            }
        }

        if (shouldRefresh == false)
        {
            return;
        }

        _searchTimer = SearchInterval;
        _roomCombatLock = FindTrackedRoomCombatLock();
    }

    private bool HasValidRoomCombatLock()
    {
        if (_roomCombatLock == null)
        {
            return false;
        }

        if (_roomCombatLock.gameObject.activeInHierarchy == false)
        {
            return false;
        }

        if (_roomCombatLock.IsLocked == false)
        {
            return false;
        }

        if (_roomCombatLock.RoomRuntimeState == null)
        {
            return false;
        }

        return true;
    }

    private RoomCombatLock FindTrackedRoomCombatLock()
    {
        RoomCombatLock[] roomCombatLocks = FindObjectsByType<RoomCombatLock>(FindObjectsSortMode.None);
        RoomCombatLock fallbackRoomCombatLock = null;
        int roomIndex = 0;

        while (roomIndex < roomCombatLocks.Length)
        {
            RoomCombatLock roomCombatLock = roomCombatLocks[roomIndex];
            roomIndex += 1;

            if (roomCombatLock == null)
            {
                continue;
            }

            if (roomCombatLock.IsLocked == false)
            {
                continue;
            }

            if (fallbackRoomCombatLock == null)
            {
                fallbackRoomCombatLock = roomCombatLock;
            }

            RoomRuntimeState roomRuntimeState = roomCombatLock.RoomRuntimeState;

            if (roomRuntimeState == null)
            {
                continue;
            }

            if (roomRuntimeState.ContainsRoomPoint(_playerTransform.position))
            {
                return roomCombatLock;
            }
        }

        return fallbackRoomCombatLock;
    }

    private void UpdateIndicators()
    {
        if (HasValidRoomCombatLock() == false)
        {
            HideIndicators();

            return;
        }

        int aliveThreatCount = _roomCombatLock.FillAliveThreatTransforms(_threats, SearchThreatCount);

        if (aliveThreatCount <= 0)
        {
            HideIndicators();

            return;
        }

        if (aliveThreatCount > VisibleThreatThreshold)
        {
            HideIndicators();

            return;
        }

        EnsureIndicatorPool(aliveThreatCount);

        int indicatorIndex = 0;

        while (indicatorIndex < aliveThreatCount)
        {
            Transform threatTransform = _threats[indicatorIndex];

            if (threatTransform == null)
            {
                _indicators[indicatorIndex].SetVisible(false);
                indicatorIndex += 1;

                continue;
            }

            UpdateIndicator(_indicators[indicatorIndex], threatTransform);
            indicatorIndex += 1;
        }

        while (indicatorIndex < _indicators.Count)
        {
            _indicators[indicatorIndex].SetVisible(false);
            indicatorIndex += 1;
        }
    }

    private void UpdateIndicator(RemainingEnemyIndicatorView indicatorView, Transform threatTransform)
    {
        Vector3 targetWorldPoint = threatTransform.position + (Vector3.up * HeightOffset);
        Vector3 viewportPoint = _worldCamera.WorldToViewportPoint(targetWorldPoint);
        bool isBehindCamera = viewportPoint.z <= 0f;
        bool isOnScreen = isBehindCamera == false
            && viewportPoint.x >= ScreenTransitionViewportPadding
            && viewportPoint.x <= 1f - ScreenTransitionViewportPadding
            && viewportPoint.y >= ScreenTransitionViewportPadding
            && viewportPoint.y <= 1f - ScreenTransitionViewportPadding;

        if (isOnScreen)
        {
            Vector2 anchoredPoint = GetAnchoredPoint(targetWorldPoint);
            indicatorView.ShowOnScreen(anchoredPoint);

            return;
        }

        Vector2 direction = GetScreenDirection(viewportPoint);
        Vector2 anchoredEdgePoint = GetAnchoredEdgePoint(direction);
        float angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        indicatorView.ShowOffScreen(anchoredEdgePoint, angleDegrees);
    }

    private Vector2 GetAnchoredPoint(Vector3 worldPoint)
    {
        Vector3 screenPoint = _worldCamera.WorldToScreenPoint(worldPoint);
        Vector2 anchoredPoint;
        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRoot, screenPoint, _uiCamera, out anchoredPoint);

        if (converted == false)
        {
            return Vector2.zero;
        }

        return anchoredPoint;
    }

    private Vector2 GetScreenDirection(Vector3 viewportPoint)
    {
        Vector2 direction = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);

        if (viewportPoint.z <= 0f)
        {
            direction = -direction;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2.up;
        }

        direction.Normalize();

        return direction;
    }

    private Vector2 GetAnchoredEdgePoint(Vector2 direction)
    {
        Rect rect = _overlayRoot.rect;
        float halfWidth = Mathf.Max(1f, (rect.width * 0.5f) - ScreenMargin);
        float halfHeight = Mathf.Max(1f, (rect.height * 0.5f) - ScreenMargin);
        float scaleX = float.MaxValue;
        float scaleY = float.MaxValue;

        if (Mathf.Abs(direction.x) > 0.0001f)
        {
            scaleX = halfWidth / Mathf.Abs(direction.x);
        }

        if (Mathf.Abs(direction.y) > 0.0001f)
        {
            scaleY = halfHeight / Mathf.Abs(direction.y);
        }

        float scale = Mathf.Min(scaleX, scaleY);

        if (float.IsInfinity(scale))
        {
            scale = Mathf.Min(halfWidth, halfHeight);
        }

        if (float.IsNaN(scale))
        {
            scale = 0f;
        }

        return direction * scale;
    }

    private void EnsureIndicatorPool(int requiredCount)
    {
        while (_indicators.Count < requiredCount)
        {
            GameObject indicatorObject = new GameObject("Remaining Enemy Indicator", typeof(RectTransform), typeof(CanvasRenderer));
            RemainingEnemyIndicatorView indicatorView = indicatorObject.AddComponent<RemainingEnemyIndicatorView>();
            indicatorView.Initialize(_overlayRoot, _ringSprite, _dotSprite, _arrowSprite, FillColor, RingColor, DotColor, ArrowColor);
            _indicators.Add(indicatorView);
        }
    }

    private void HideIndicators()
    {
        int indicatorIndex = 0;

        while (indicatorIndex < _indicators.Count)
        {
            _indicators[indicatorIndex].SetVisible(false);
            indicatorIndex += 1;
        }
    }

    private void BuildOverlayRoot()
    {
        if (_overlayRoot != null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("Remaining Enemy Overlay", typeof(RectTransform), typeof(CanvasRenderer));
        overlayObject.transform.SetParent(_uiRoot, false);
        _overlayRoot = overlayObject.GetComponent<RectTransform>();

        if (_overlayRoot == null)
        {
            throw new InvalidOperationException(nameof(_overlayRoot));
        }

        _overlayRoot.anchorMin = Vector2.zero;
        _overlayRoot.anchorMax = Vector2.one;
        _overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        _overlayRoot.offsetMin = Vector2.zero;
        _overlayRoot.offsetMax = Vector2.zero;
        _overlayRoot.anchoredPosition = Vector2.zero;
        _overlayRoot.localScale = Vector3.one;
        _overlayRoot.localRotation = Quaternion.identity;
        _overlayRoot.SetAsLastSibling();
    }

    private void ResolveWorldCamera()
    {
        if (_worldCamera != null && _worldCamera.isActiveAndEnabled)
        {
            return;
        }

        _worldCamera = Camera.main;
    }

    private void ResolveUiCamera()
    {
        if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = null;

            return;
        }

        _uiCamera = _canvas.worldCamera;
    }

    private void BuildSprites()
    {
        if (_ringSprite != null)
        {
            return;
        }

        _ringTexture = CreateRingTexture(RingTextureSize, 0.7f);
        _dotTexture = CreateCircleTexture(DotTextureSize);
        _arrowTexture = CreateArrowTexture(ArrowTextureSize);
        _ringSprite = CreateSprite(_ringTexture);
        _dotSprite = CreateSprite(_dotTexture);
        _arrowSprite = CreateSprite(_arrowTexture);
    }

    private Texture2D CreateRingTexture(int size, float innerRadiusScale)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "RemainingEnemyRing";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float outerRadius = center;
        float innerRadius = outerRadius * innerRadiusScale;
        int pixelIndex = 0;
        int y = 0;

        while (y < size)
        {
            int x = 0;

            while (x < size)
            {
                float deltaX = x - center;
                float deltaY = y - center;
                float distance = Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY));

                if (distance <= outerRadius && distance >= innerRadius)
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 255);
                }
                else
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 0);
                }

                pixelIndex += 1;
                x += 1;
            }

            y += 1;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "RemainingEnemyDot";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center;
        int pixelIndex = 0;
        int y = 0;

        while (y < size)
        {
            int x = 0;

            while (x < size)
            {
                float deltaX = x - center;
                float deltaY = y - center;
                float distanceSqr = (deltaX * deltaX) + (deltaY * deltaY);

                if (distanceSqr <= radius * radius)
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 255);
                }
                else
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 0);
                }

                pixelIndex += 1;
                x += 1;
            }

            y += 1;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    private Texture2D CreateArrowTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.name = "RemainingEnemyArrow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        float centerX = (size - 1) * 0.5f;
        float baseY = size * 0.2f;
        float tipY = size * 0.82f;
        float halfBaseWidth = size * 0.16f;
        int pixelIndex = 0;
        int y = 0;

        while (y < size)
        {
            int x = 0;

            while (x < size)
            {
                float progress = Mathf.InverseLerp(baseY, tipY, y);
                float halfWidth = Mathf.Lerp(halfBaseWidth, 0f, progress);
                bool isInsideHeight = y >= baseY && y <= tipY;
                bool isInsideWidth = Mathf.Abs(x - centerX) <= halfWidth;

                if (isInsideHeight && isInsideWidth)
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 255);
                }
                else
                {
                    pixels[pixelIndex] = new Color32(255, 255, 255, 0);
                }

                pixelIndex += 1;
                x += 1;
            }

            y += 1;
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return texture;
    }

    private Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private void DestroySpriteResources()
    {
        DestroySprite(_ringSprite);
        DestroySprite(_dotSprite);
        DestroySprite(_arrowSprite);
        DestroyTexture(_ringTexture);
        DestroyTexture(_dotTexture);
        DestroyTexture(_arrowTexture);
    }

    private void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        Destroy(sprite);
    }

    private void DestroyTexture(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        Destroy(texture);
    }
}

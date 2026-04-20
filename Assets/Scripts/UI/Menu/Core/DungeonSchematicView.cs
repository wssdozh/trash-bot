using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DungeonSchematicView : MonoBehaviour
{
    [SerializeField] private RectTransform _viewRoot;
    [SerializeField] private Image _spriteSource;
    [SerializeField, Min(5)] private int _gridWidth = 19;
    [SerializeField, Min(5)] private int _gridHeight = 11;
    [SerializeField, Min(4f)] private float _cellSize = 28.0f;
    [SerializeField, Min(0f)] private float _cellSpacing = 6.0f;
    [SerializeField, Min(0f)] private float _framePadding = 24.0f;
    [SerializeField, Min(1)] private int _minimumRooms = 4;
    [SerializeField, Min(1)] private int _maximumRooms = 7;
    [SerializeField, Min(3)] private int _minimumRoomWidth = 3;
    [SerializeField, Min(3)] private int _maximumRoomWidth = 5;
    [SerializeField, Min(3)] private int _minimumRoomHeight = 3;
    [SerializeField, Min(3)] private int _maximumRoomHeight = 5;
    [SerializeField, Min(1)] private int _roomPadding = 1;
    [SerializeField, Min(1)] private int _minimumCorridorGap = 2;
    [SerializeField, Min(1)] private int _maximumCorridorGap = 4;
    [SerializeField, Min(0.01f)] private float _revealIntervalSeconds = 0.035f;
    [SerializeField, Min(1)] private int _cellsPerReveal = 3;
    [SerializeField, Min(0f)] private float _holdDurationSeconds = 0.55f;
    [SerializeField, Min(0f)] private float _fadeDurationSeconds = 0.20f;
    [SerializeField, Min(0f)] private float _cursorPadding = 4.0f;
    [SerializeField, Min(0.01f)] private float _cursorPulseSpeed = 5.0f;
    [SerializeField, Min(0f)] private float _cursorPulseAmplitude = 0.10f;
    [SerializeField] private Color _frameColor = new Color(0.03f, 0.08f, 0.06f, 0.82f);
    [SerializeField] private Color _gridColor = new Color(0.18f, 0.25f, 0.20f, 0.14f);
    [SerializeField] private Color _roomColor = new Color(0.86f, 1.00f, 0.90f, 0.98f);
    [SerializeField] private Color _corridorColor = new Color(0.20f, 0.96f, 0.74f, 0.96f);
    [SerializeField] private Color _startColor = new Color(0.78f, 1.00f, 0.34f, 1.00f);
    [SerializeField] private Color _exitColor = new Color(1.00f, 0.44f, 0.18f, 1.00f);
    [SerializeField] private Color _cursorColor = new Color(1.00f, 1.00f, 1.00f, 0.58f);

    private readonly List<RectInt> _roomRects = new List<RectInt>(8);
    private readonly List<Vector2Int> _roomCenters = new List<Vector2Int>(8);
    private readonly List<int> _roomCells = new List<int>(160);
    private readonly List<int> _corridorCells = new List<int>(160);
    private readonly List<int> _revealOrder = new List<int>(256);
    private readonly HashSet<int> _roomCellSet = new HashSet<int>();
    private readonly HashSet<int> _corridorCellSet = new HashSet<int>();
    private readonly HashSet<int> _revealCellSet = new HashSet<int>();
    private readonly System.Random _random = new System.Random();

    private RectTransform _contentRoot;
    private RectTransform _frameRoot;
    private RectTransform _gridRoot;
    private RectTransform _cursorRoot;
    private Image _cursorImage;
    private Image[] _cellImages = Array.Empty<Image>();
    private RectTransform[] _cellRects = Array.Empty<RectTransform>();
    private Coroutine _animationCoroutine;
    private int _startCellIndex = -1;
    private int _exitCellIndex = -1;
    private int _currentCellIndex = -1;

    public int GridWidth
    {
        get
        {
            return _gridWidth;
        }
    }

    public int GridHeight
    {
        get
        {
            return _gridHeight;
        }
    }

    public void ApplyLoadingPalette()
    {
        _frameColor = new Color(0.03f, 0.08f, 0.06f, 0.82f);
        _gridColor = new Color(0.18f, 0.25f, 0.20f, 0.14f);
        _roomColor = new Color(0.86f, 1.00f, 0.90f, 0.98f);
        _corridorColor = new Color(0.20f, 0.96f, 0.74f, 0.96f);
        _startColor = new Color(0.78f, 1.00f, 0.34f, 1.00f);
        _exitColor = new Color(1.00f, 0.44f, 0.18f, 1.00f);
        _cursorColor = new Color(1.00f, 1.00f, 1.00f, 0.58f);

        if (_frameRoot == null)
        {
            return;
        }

        Image frameImage = _frameRoot.GetComponent<Image>();

        if (frameImage == null)
        {
            return;
        }

        frameImage.color = _frameColor;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureView();
        ClearLayout();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void PlayLoadingAnimation()
    {
        EnsureView();

        if (_animationCoroutine != null)
            return;

        _animationCoroutine = StartCoroutine(PlayLoadingAnimationRoutine());
    }

    public void StopAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }

        HideCursor();
    }

    public void ClearLayout()
    {
        StopAnimation();
        _startCellIndex = -1;
        _exitCellIndex = -1;
        _currentCellIndex = -1;
        ApplyGhostGrid();
        HideCursor();
    }

    public void ShowLayout(
        IReadOnlyCollection<Vector2Int> roomCells,
        IReadOnlyCollection<Vector2Int> corridorCells,
        Vector2Int startCell,
        Vector2Int exitCell
    )
    {
        EnsureView();
        StopAnimation();
        ApplyGhostGrid();

        _startCellIndex = GetCellIndex(startCell.x, startCell.y);
        _exitCellIndex = GetCellIndex(exitCell.x, exitCell.y);
        _currentCellIndex = -1;

        ApplyCells(corridorCells, _corridorColor);
        ApplyCells(roomCells, _roomColor);
        ApplyMarkerColor(_startCellIndex, _startColor);
        ApplyMarkerColor(_exitCellIndex, _exitColor);
        HideCursor();
    }

    public void SetCurrentCell(Vector2Int currentCell)
    {
        EnsureView();
        _currentCellIndex = GetCellIndex(currentCell.x, currentCell.y);

        if (IsValidCellIndex(_currentCellIndex) == false)
        {
            HideCursor();
            return;
        }

        MoveCursorToCell(_currentCellIndex);
        SetCursorVisual(1.0f, 1.0f);
    }

    private IEnumerator PlayLoadingAnimationRoutine()
    {
        while (true)
        {
            BuildLoadingLayout();
            ApplyGhostGrid();
            HideCursor();

            int revealIndex = 0;
            float revealTimer = 0.0f;

            while (revealIndex < _revealOrder.Count)
            {
                revealTimer += Time.unscaledDeltaTime;

                if (revealTimer < _revealIntervalSeconds)
                {
                    yield return null;
                    continue;
                }

                revealTimer = 0.0f;

                int revealCount = 0;

                while (revealCount < _cellsPerReveal && revealIndex < _revealOrder.Count)
                {
                    int cellIndex = _revealOrder[revealIndex];
                    ApplyCellColor(cellIndex, GetColorForCell(cellIndex));
                    MoveCursorToCell(cellIndex);
                    revealIndex += 1;
                    revealCount += 1;
                }

                yield return null;
            }

            float holdTimer = 0.0f;

            while (holdTimer < _holdDurationSeconds)
            {
                holdTimer += Time.unscaledDeltaTime;
                UpdateCursorPulse(holdTimer);
                yield return null;
            }

            yield return FadeToGhostRoutine();
        }
    }

    private IEnumerator FadeToGhostRoutine()
    {
        if (_fadeDurationSeconds <= 0.0f)
        {
            ApplyGhostGrid();
            HideCursor();
            yield break;
        }

        float timer = 0.0f;

        while (timer < _fadeDurationSeconds)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / _fadeDurationSeconds);

            for (int index = 0; index < _revealOrder.Count; index++)
            {
                int cellIndex = _revealOrder[index];
                Color startColor = GetColorForCell(cellIndex);
                Color color = Color.Lerp(startColor, _gridColor, progress);
                ApplyCellColor(cellIndex, color);
            }

            UpdateCursorFade(progress);
            yield return null;
        }

        ApplyGhostGrid();
        HideCursor();
    }

    private void ResolveReferences()
    {
        if (_viewRoot == null)
            _viewRoot = transform as RectTransform;

        if (_spriteSource == null)
            _spriteSource = GetComponent<Image>();

        if (_viewRoot == null)
            throw new MissingReferenceException(nameof(_viewRoot));

        if (_spriteSource == null)
            throw new MissingReferenceException(nameof(_spriteSource));

        if (_spriteSource.sprite == null)
            throw new MissingReferenceException(nameof(_spriteSource.sprite));
    }

    private void EnsureView()
    {
        if (_contentRoot != null)
            return;

        _contentRoot = CreateRect("SchematicView", _viewRoot);
        _contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _contentRoot.pivot = new Vector2(0.5f, 0.5f);
        _contentRoot.anchoredPosition = Vector2.zero;

        _frameRoot = CreateRect("Frame", _contentRoot);
        _frameRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _frameRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _frameRoot.pivot = new Vector2(0.5f, 0.5f);
        _frameRoot.anchoredPosition = Vector2.zero;
        _frameRoot.sizeDelta = new Vector2(GetGridWidthInUnits() + (_framePadding * 2.0f), GetGridHeightInUnits() + (_framePadding * 2.0f));

        Image frameImage = _frameRoot.gameObject.AddComponent<Image>();
        frameImage.sprite = _spriteSource.sprite;
        frameImage.type = Image.Type.Sliced;
        frameImage.raycastTarget = false;
        frameImage.color = _frameColor;

        _gridRoot = CreateRect("Grid", _frameRoot);
        _gridRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _gridRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _gridRoot.pivot = new Vector2(0.5f, 0.5f);
        _gridRoot.anchoredPosition = Vector2.zero;
        _gridRoot.sizeDelta = new Vector2(GetGridWidthInUnits(), GetGridHeightInUnits());

        _cursorRoot = CreateRect("Cursor", _gridRoot);
        _cursorRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _cursorRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _cursorRoot.pivot = new Vector2(0.5f, 0.5f);
        _cursorRoot.sizeDelta = new Vector2(_cellSize + (_cursorPadding * 2.0f), _cellSize + (_cursorPadding * 2.0f));

        _cursorImage = _cursorRoot.gameObject.AddComponent<Image>();
        _cursorImage.sprite = _spriteSource.sprite;
        _cursorImage.type = Image.Type.Sliced;
        _cursorImage.raycastTarget = false;
        _cursorImage.color = _cursorColor;

        int cellCount = _gridWidth * _gridHeight;
        _cellImages = new Image[cellCount];
        _cellRects = new RectTransform[cellCount];

        for (int y = 0; y < _gridHeight; y++)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                int cellIndex = GetCellIndex(x, y);
                RectTransform cellRect = CreateRect("Cell_" + cellIndex, _gridRoot);
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(_cellSize, _cellSize);
                cellRect.anchoredPosition = GetCellPosition(x, y);

                Image cellImage = cellRect.gameObject.AddComponent<Image>();
                cellImage.sprite = _spriteSource.sprite;
                cellImage.type = Image.Type.Sliced;
                cellImage.raycastTarget = false;

                _cellRects[cellIndex] = cellRect;
                _cellImages[cellIndex] = cellImage;
            }
        }
    }

    private RectTransform CreateRect(string objectName, RectTransform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform childRect = child.GetComponent<RectTransform>();
        childRect.SetParent(parent, false);
        childRect.localScale = Vector3.one;
        childRect.localRotation = Quaternion.identity;
        childRect.localPosition = Vector3.zero;
        return childRect;
    }

    private void ApplyGhostGrid()
    {
        for (int index = 0; index < _cellImages.Length; index++)
            ApplyCellColor(index, _gridColor);
    }

    private void ApplyCells(IReadOnlyCollection<Vector2Int> cells, Color color)
    {
        if (cells == null)
            return;

        foreach (Vector2Int cell in cells)
        {
            int cellIndex = GetCellIndex(cell.x, cell.y);

            if (IsValidCellIndex(cellIndex) == false)
                continue;

            ApplyCellColor(cellIndex, color);
        }
    }

    private void ApplyMarkerColor(int cellIndex, Color color)
    {
        if (IsValidCellIndex(cellIndex) == false)
            return;

        ApplyCellColor(cellIndex, color);
    }

    private void ApplyCellColor(int cellIndex, Color color)
    {
        if (IsValidCellIndex(cellIndex) == false)
            return;

        Image image = _cellImages[cellIndex];

        if (image == null)
            return;

        image.color = color;
    }

    private void MoveCursorToCell(int cellIndex)
    {
        if (IsValidCellIndex(cellIndex) == false)
        {
            HideCursor();
            return;
        }

        _cursorRoot.anchoredPosition = _cellRects[cellIndex].anchoredPosition;
        _cursorImage.enabled = true;
    }

    private void HideCursor()
    {
        if (_cursorImage == null)
            return;

        _cursorImage.enabled = false;
    }

    private void SetCursorVisual(float alphaMultiplier, float scaleMultiplier)
    {
        if (_cursorImage == null)
            return;

        Color color = _cursorColor;
        color.a = color.a * alphaMultiplier;
        _cursorImage.color = color;
        _cursorRoot.localScale = Vector3.one * scaleMultiplier;
    }

    private void UpdateCursorPulse(float timer)
    {
        if (_cursorImage == null)
            return;

        if (_cursorImage.enabled == false)
            return;

        float pulse = Mathf.Sin(timer * _cursorPulseSpeed);
        float alphaMultiplier = 0.70f + ((pulse + 1.0f) * 0.15f);
        float scaleMultiplier = 1.0f + (((pulse + 1.0f) * 0.5f) * _cursorPulseAmplitude);
        SetCursorVisual(alphaMultiplier, scaleMultiplier);
    }

    private void UpdateCursorFade(float progress)
    {
        if (_cursorImage == null)
            return;

        if (_cursorImage.enabled == false)
            return;

        float alphaMultiplier = 1.0f - progress;
        float scaleMultiplier = 1.0f + ((1.0f - progress) * _cursorPulseAmplitude);
        SetCursorVisual(alphaMultiplier, scaleMultiplier);
    }

    private void BuildLoadingLayout()
    {
        _roomRects.Clear();
        _roomCenters.Clear();
        _roomCells.Clear();
        _corridorCells.Clear();
        _revealOrder.Clear();
        _roomCellSet.Clear();
        _corridorCellSet.Clear();
        _revealCellSet.Clear();

        int minimumRooms = Mathf.Max(1, _minimumRooms);
        int maximumRooms = Mathf.Max(minimumRooms, _maximumRooms);
        int targetRoomCount = _random.Next(minimumRooms, maximumRooms + 1);
        int minimumMainRooms = Mathf.Min(3, targetRoomCount);
        int maximumMainRooms = Mathf.Min(targetRoomCount, 5);
        List<RectInt> mainPathRooms = new List<RectInt>(maximumMainRooms);

        RectInt startRoom = BuildStartRoom();
        AddRoom(startRoom);
        mainPathRooms.Add(startRoom);
        _startCellIndex = GetCellIndex(GetRoomCenter(startRoom));
        int placedRooms = 1;
        RectInt lastMainRoom = startRoom;

        if (maximumMainRooms >= minimumMainRooms)
        {
            int mainRoomCount = _random.Next(minimumMainRooms, maximumMainRooms + 1);

            while (placedRooms < mainRoomCount)
            {
                RectInt nextRoom;

                if (TryBuildMainPathRoom(lastMainRoom, out nextRoom) == false)
                {
                    break;
                }

                AddCorridor(GetRoomCenter(lastMainRoom), GetRoomCenter(nextRoom));
                AddRoom(nextRoom);
                mainPathRooms.Add(nextRoom);
                lastMainRoom = nextRoom;
                placedRooms += 1;
            }
        }

        int branchAttemptCount = 0;

        while (placedRooms < targetRoomCount)
        {
            if (branchAttemptCount >= 24)
            {
                break;
            }

            int parentRoomIndex = _random.Next(0, mainPathRooms.Count);
            RectInt parentRoom = mainPathRooms[parentRoomIndex];
            RectInt branchRoom;
            int verticalDirection = _random.Next(0, 2) == 0 ? 1 : -1;

            if (TryBuildBranchRoom(parentRoom, verticalDirection, out branchRoom) == false)
            {
                verticalDirection = verticalDirection * -1;

                if (TryBuildBranchRoom(parentRoom, verticalDirection, out branchRoom) == false)
                {
                    branchAttemptCount += 1;
                    continue;
                }
            }

            AddCorridor(GetRoomCenter(parentRoom), GetRoomCenter(branchRoom));
            AddRoom(branchRoom);
            placedRooms += 1;
            branchAttemptCount += 1;
        }

        if (_roomRects.Count == 0)
        {
            _exitCellIndex = -1;
            return;
        }

        _exitCellIndex = GetCellIndex(GetRoomCenter(lastMainRoom));
    }

    private RectInt BuildStartRoom()
    {
        int width = _random.Next(_minimumRoomWidth, _maximumRoomWidth + 1);
        int height = _random.Next(_minimumRoomHeight, _maximumRoomHeight + 1);
        int x = 1;
        int y = Mathf.Clamp((_gridHeight / 2) - (height / 2), 1, _gridHeight - height - 1);
        return new RectInt(x, y, width, height);
    }

    private bool TryBuildMainPathRoom(RectInt previousRoom, out RectInt nextRoom)
    {
        nextRoom = new RectInt();
        int width = _random.Next(_minimumRoomWidth, _maximumRoomWidth + 1);
        int height = _random.Next(_minimumRoomHeight, _maximumRoomHeight + 1);
        int gap = _random.Next(_minimumCorridorGap, _maximumCorridorGap + 1);
        int x = previousRoom.xMax + gap;
        int y = Mathf.Clamp(GetRoomCenter(previousRoom).y - (height / 2), 1, _gridHeight - height - 1);

        for (int offset = 0; offset <= 2; offset++)
        {
            RectInt centerRoom = new RectInt(x, y, width, height);

            if (IsRoomValid(centerRoom))
            {
                nextRoom = centerRoom;
                return true;
            }

            RectInt upperRoom = new RectInt(x, y + offset, width, height);

            if (IsRoomValid(upperRoom))
            {
                nextRoom = upperRoom;
                return true;
            }

            RectInt lowerRoom = new RectInt(x, y - offset, width, height);

            if (IsRoomValid(lowerRoom))
            {
                nextRoom = lowerRoom;
                return true;
            }
        }

        return false;
    }

    private bool TryBuildBranchRoom(RectInt parentRoom, int verticalDirection, out RectInt nextRoom)
    {
        nextRoom = new RectInt();

        int width = _random.Next(_minimumRoomWidth, _maximumRoomWidth + 1);
        int height = _random.Next(_minimumRoomHeight, _maximumRoomHeight + 1);
        int gap = _random.Next(_minimumCorridorGap, _maximumCorridorGap + 1);
        int centerX = GetRoomCenter(parentRoom).x;
        int x = Mathf.Clamp(centerX - (width / 2), 1, _gridWidth - width - 1);
        int y = 0;

        if (verticalDirection > 0)
        {
            y = parentRoom.yMax + gap;
        }
        else
        {
            y = parentRoom.yMin - gap - height;
        }

        for (int offset = 0; offset <= 2; offset++)
        {
            RectInt centerRoom = new RectInt(x, y, width, height);

            if (IsRoomValid(centerRoom))
            {
                nextRoom = centerRoom;
                return true;
            }

            RectInt rightRoom = new RectInt(x + offset, y, width, height);

            if (IsRoomValid(rightRoom))
            {
                nextRoom = rightRoom;
                return true;
            }

            RectInt leftRoom = new RectInt(x - offset, y, width, height);

            if (IsRoomValid(leftRoom))
            {
                nextRoom = leftRoom;
                return true;
            }
        }

        return false;
    }

    private bool IsRoomValid(RectInt room)
    {
        if (room.xMin < 1 || room.yMin < 1)
            return false;

        if (room.xMax >= _gridWidth - 1 || room.yMax >= _gridHeight - 1)
            return false;

        RectInt expandedRoom = ExpandRect(room, _roomPadding);

        for (int index = 0; index < _roomRects.Count; index++)
        {
            if (expandedRoom.Overlaps(_roomRects[index]))
                return false;
        }

        return true;
    }

    private RectInt ExpandRect(RectInt source, int padding)
    {
        return new RectInt(source.xMin - padding, source.yMin - padding, source.width + (padding * 2), source.height + (padding * 2));
    }

    private void AddRoom(RectInt room)
    {
        _roomRects.Add(room);
        _roomCenters.Add(GetRoomCenter(room));

        Vector2Int roomCenter = GetRoomCenter(room);
        int maxDistance = room.width + room.height;

        for (int distance = 0; distance <= maxDistance; distance++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                for (int x = room.xMin; x < room.xMax; x++)
                {
                    int manhattanDistance = Mathf.Abs(roomCenter.x - x) + Mathf.Abs(roomCenter.y - y);

                    if (manhattanDistance != distance)
                        continue;

                    AddRoomCell(x, y);
                }
            }
        }
    }

    private void AddRoomCell(int x, int y)
    {
        int cellIndex = GetCellIndex(x, y);

        if (IsValidCellIndex(cellIndex) == false)
            return;

        if (_roomCellSet.Add(cellIndex))
            _roomCells.Add(cellIndex);

        AppendRevealCell(cellIndex);
    }

    private void AddCorridor(Vector2Int fromCell, Vector2Int toCell)
    {
        bool horizontalFirst = _random.Next(0, 2) == 0;

        if (horizontalFirst)
        {
            AddHorizontalCorridor(fromCell.x, toCell.x, fromCell.y);
            AddVerticalCorridor(fromCell.y, toCell.y, toCell.x);
            return;
        }

        AddVerticalCorridor(fromCell.y, toCell.y, fromCell.x);
        AddHorizontalCorridor(fromCell.x, toCell.x, toCell.y);
    }

    private void AddHorizontalCorridor(int fromX, int toX, int y)
    {
        int step = fromX <= toX ? 1 : -1;
        int x = fromX;

        while (true)
        {
            AddCorridorCell(x, y);

            if (x == toX)
                break;

            x += step;
        }
    }

    private void AddVerticalCorridor(int fromY, int toY, int x)
    {
        int step = fromY <= toY ? 1 : -1;
        int y = fromY;

        while (true)
        {
            AddCorridorCell(x, y);

            if (y == toY)
                break;

            y += step;
        }
    }

    private void AddCorridorCell(int x, int y)
    {
        int cellIndex = GetCellIndex(x, y);

        if (IsValidCellIndex(cellIndex) == false)
            return;

        if (_roomCellSet.Contains(cellIndex) == false && _corridorCellSet.Add(cellIndex))
            _corridorCells.Add(cellIndex);

        AppendRevealCell(cellIndex);
    }

    private void AppendRevealCell(int cellIndex)
    {
        if (_revealCellSet.Add(cellIndex))
            _revealOrder.Add(cellIndex);
    }

    private Vector2Int GetRoomCenter(RectInt room)
    {
        int x = room.xMin + (room.width / 2);
        int y = room.yMin + (room.height / 2);
        return new Vector2Int(x, y);
    }

    private Vector2 GetCellPosition(int x, int y)
    {
        float width = GetGridWidthInUnits();
        float height = GetGridHeightInUnits();
        float step = _cellSize + _cellSpacing;
        float positionX = (-width * 0.5f) + (_cellSize * 0.5f) + (x * step);
        float positionY = (-height * 0.5f) + (_cellSize * 0.5f) + (y * step);
        return new Vector2(positionX, positionY);
    }

    private float GetGridWidthInUnits()
    {
        return (_gridWidth * _cellSize) + ((_gridWidth - 1) * _cellSpacing);
    }

    private float GetGridHeightInUnits()
    {
        return (_gridHeight * _cellSize) + ((_gridHeight - 1) * _cellSpacing);
    }

    private Color GetColorForCell(int cellIndex)
    {
        if (cellIndex == _startCellIndex)
            return _startColor;

        if (cellIndex == _exitCellIndex)
            return _exitColor;

        if (_roomCellSet.Contains(cellIndex))
            return _roomColor;

        if (_corridorCellSet.Contains(cellIndex))
            return _corridorColor;

        return _gridColor;
    }

    private int GetCellIndex(Vector2Int cell)
    {
        return GetCellIndex(cell.x, cell.y);
    }

    private int GetCellIndex(int x, int y)
    {
        if (x < 0 || y < 0)
            return -1;

        if (x >= _gridWidth || y >= _gridHeight)
            return -1;

        return x + (y * _gridWidth);
    }

    private bool IsValidCellIndex(int cellIndex)
    {
        if (cellIndex < 0)
            return false;

        if (cellIndex >= _cellImages.Length)
            return false;

        return true;
    }
}

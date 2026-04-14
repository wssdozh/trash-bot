using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelRoomStreamer : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField, Min(0)] private int _enableDepth = 1;
    [SerializeField, Min(0)] private int _disableDepth = 1;
    [SerializeField, Min(0f)] private float _startDelay = 0.5f;
    [SerializeField, Min(0.02f)] private float _updateInterval = 0.2f;

    private readonly List<RoomRuntimeState> _rooms = new List<RoomRuntimeState>(64);
    private readonly List<RoomLink> _links = new List<RoomLink>(128);
    private readonly Queue<int> _roomQueue = new Queue<int>(64);

    private LevelRuntimeNavMesh _levelRuntimeNavMesh;
    private Transform _playerTrack;
    private Coroutine _streamCoroutine;
    private WaitForSecondsRealtime _updateDelay;
    private int[] _roomDepths = new int[0];
    private float _startTime;
    private bool _isStarted;

    internal void Setup(Transform player, int enableDepth, int disableDepth, float startDelay, IReadOnlyList<RoomRuntimeState> rooms, IReadOnlyList<LevelRoomStreamLink> roomStreamLinks)
    {
        _player = player;
        _enableDepth = Mathf.Max(0, enableDepth);
        _disableDepth = Mathf.Max(_enableDepth, disableDepth);
        _startDelay = Mathf.Max(0f, startDelay);
        _startTime = Time.unscaledTime;
        _isStarted = _startDelay <= 0f;
        _levelRuntimeNavMesh = GetComponent<LevelRuntimeNavMesh>();
        _updateDelay = new WaitForSecondsRealtime(GetUpdateInterval());
        _playerTrack = null;

        if (_player != null)
        {
            _playerTrack = ResolvePlayerTrack(_player);
        }

        _rooms.Clear();
        _links.Clear();
        _roomQueue.Clear();

        Dictionary<RoomRuntimeState, int> roomIndices = new Dictionary<RoomRuntimeState, int>(rooms.Count);

        for (int index = 0; index < rooms.Count; index++)
        {
            RoomRuntimeState roomRuntimeState = rooms[index];

            if (roomRuntimeState == null)
            {
                continue;
            }

            if (roomIndices.ContainsKey(roomRuntimeState))
            {
                continue;
            }

            roomRuntimeState.SetRoomActive(true);
            roomIndices.Add(roomRuntimeState, _rooms.Count);
            _rooms.Add(roomRuntimeState);
        }

        CacheLinks(roomStreamLinks, roomIndices);
        EnsureBuffers();
        enabled = _rooms.Count > 0;

        if (enabled)
        {
            StartStreamLoop();
        }
    }

    public void ClearRooms()
    {
        for (int index = 0; index < _rooms.Count; index++)
        {
            RoomRuntimeState roomRuntimeState = _rooms[index];

            if (roomRuntimeState == null)
            {
                continue;
            }

            roomRuntimeState.SetRoomActive(true);
        }

        _rooms.Clear();
        _links.Clear();
        _roomQueue.Clear();
        _isStarted = false;
        _playerTrack = null;
        StopStreamLoop();
        enabled = false;
    }

    private void OnDisable()
    {
        StopStreamLoop();
    }

    private IEnumerator StreamLoop()
    {
        while (enabled)
        {
            TickStream();

            yield return _updateDelay;
        }
    }

    private void TickStream()
    {
        if (_rooms.Count == 0)
        {
            return;
        }

        if (_isStarted == false)
        {
            if (Time.unscaledTime - _startTime < _startDelay)
            {
                return;
            }

            _isStarted = true;
        }

        Transform player = GetPlayer();

        if (player == null)
        {
            return;
        }

        UpdateRooms(player.position);
    }

    private Transform GetPlayer()
    {
        if (_playerTrack != null)
        {
            return _playerTrack;
        }

        if (_player != null)
        {
            _playerTrack = ResolvePlayerTrack(_player);

            return _playerTrack;
        }

        Player playerComponent = FindFirstObjectByType<Player>();

        if (playerComponent != null)
        {
            _player = playerComponent.transform;
            _playerTrack = ResolvePlayerTrack(_player);

            return _playerTrack;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return null;
        }

        Player rootPlayer = playerObject.GetComponentInParent<Player>();

        if (rootPlayer != null)
        {
            _player = rootPlayer.transform;
            _playerTrack = ResolvePlayerTrack(_player);

            return _playerTrack;
        }

        _player = playerObject.transform;
        _playerTrack = ResolvePlayerTrack(_player);

        return _playerTrack;
    }

    private Transform ResolvePlayerTrack(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            return null;
        }

        Rigidbody[] rigidbodyComponents = playerTransform.GetComponentsInChildren<Rigidbody>(true);

        for (int index = 0; index < rigidbodyComponents.Length; index++)
        {
            Rigidbody rigidbodyComponent = rigidbodyComponents[index];

            if (rigidbodyComponent == null)
            {
                continue;
            }

            if (rigidbodyComponent.isKinematic)
            {
                continue;
            }

            return rigidbodyComponent.transform;
        }

        return playerTransform;
    }

    private void UpdateRooms(Vector3 playerPoint)
    {
        int playerRoomIndex = FindPlayerRoomIndex(playerPoint);

        if (playerRoomIndex < 0)
        {
            return;
        }

        UpdateRoomDepths(playerRoomIndex);
        bool hasRoomStateChanged = false;

        for (int index = 0; index < _rooms.Count; index++)
        {
            RoomRuntimeState roomRuntimeState = _rooms[index];

            if (roomRuntimeState == null)
            {
                continue;
            }

            int roomDepth = _roomDepths[index];
            bool isReachable = roomDepth >= 0;
            bool isRoomActive = roomRuntimeState.gameObject.activeSelf;

            if (isRoomActive)
            {
                if (isReachable == false || roomDepth > _disableDepth)
                {
                    roomRuntimeState.SetRoomActive(false);
                    hasRoomStateChanged = true;
                }

                continue;
            }

            if (isReachable && roomDepth <= _enableDepth)
            {
                roomRuntimeState.SetRoomActive(true);
                hasRoomStateChanged = true;
            }
        }

        if (hasRoomStateChanged)
        {
            RequestNavMeshUpdate();
        }
    }

    private void RequestNavMeshUpdate()
    {
        if (_levelRuntimeNavMesh == null)
        {
            _levelRuntimeNavMesh = GetComponent<LevelRuntimeNavMesh>();
        }

        if (_levelRuntimeNavMesh == null)
        {
            return;
        }

        _levelRuntimeNavMesh.RequestUpdate();
    }

    private void StartStreamLoop()
    {
        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
        }

        _streamCoroutine = StartCoroutine(StreamLoop());
    }

    private void StopStreamLoop()
    {
        if (_streamCoroutine == null)
        {
            return;
        }

        StopCoroutine(_streamCoroutine);
        _streamCoroutine = null;
    }

    private float GetUpdateInterval()
    {
        return Mathf.Max(0.02f, _updateInterval);
    }

    private void CacheLinks(IReadOnlyList<LevelRoomStreamLink> roomStreamLinks, Dictionary<RoomRuntimeState, int> roomIndices)
    {
        if (roomStreamLinks == null)
        {
            return;
        }

        for (int linkIndex = 0; linkIndex < roomStreamLinks.Count; linkIndex++)
        {
            LevelRoomStreamLink roomStreamLink = roomStreamLinks[linkIndex];

            if (roomStreamLink.FirstRoom == null)
            {
                continue;
            }

            if (roomStreamLink.SecondRoom == null)
            {
                continue;
            }

            if (roomIndices.ContainsKey(roomStreamLink.FirstRoom) == false)
            {
                continue;
            }

            if (roomIndices.ContainsKey(roomStreamLink.SecondRoom) == false)
            {
                continue;
            }

            int firstRoomIndex = roomIndices[roomStreamLink.FirstRoom];
            int secondRoomIndex = roomIndices[roomStreamLink.SecondRoom];

            if (firstRoomIndex == secondRoomIndex)
            {
                continue;
            }

            RoomLink roomLink = new RoomLink(firstRoomIndex, secondRoomIndex);
            _links.Add(roomLink);
        }
    }

    private void EnsureBuffers()
    {
        if (_roomDepths.Length != _rooms.Count)
        {
            _roomDepths = new int[_rooms.Count];
        }
    }

    private int FindPlayerRoomIndex(Vector3 playerPoint)
    {
        int containingRoomIndex = -1;
        float containingRoomDistanceSqr = float.MaxValue;
        int nearestRoomIndex = -1;
        float nearestRoomDistanceSqr = float.MaxValue;

        for (int index = 0; index < _rooms.Count; index++)
        {
            RoomRuntimeState roomRuntimeState = _rooms[index];

            if (roomRuntimeState == null)
            {
                continue;
            }

            float distanceSqr = roomRuntimeState.GetDistanceSqr(playerPoint);

            if (distanceSqr < nearestRoomDistanceSqr)
            {
                nearestRoomDistanceSqr = distanceSqr;
                nearestRoomIndex = index;
            }

            if (roomRuntimeState.ContainsRoomPoint(playerPoint) == false)
            {
                continue;
            }

            if (distanceSqr < containingRoomDistanceSqr)
            {
                containingRoomDistanceSqr = distanceSqr;
                containingRoomIndex = index;
            }
        }

        if (containingRoomIndex >= 0)
        {
            return containingRoomIndex;
        }

        return nearestRoomIndex;
    }

    private void UpdateRoomDepths(int playerRoomIndex)
    {
        EnsureBuffers();

        for (int index = 0; index < _roomDepths.Length; index++)
        {
            _roomDepths[index] = -1;
        }

        _roomQueue.Clear();

        if (playerRoomIndex < 0)
        {
            return;
        }

        if (playerRoomIndex >= _rooms.Count)
        {
            return;
        }

        if (_rooms[playerRoomIndex] == null)
        {
            return;
        }

        _roomDepths[playerRoomIndex] = 0;
        _roomQueue.Enqueue(playerRoomIndex);

        while (_roomQueue.Count > 0)
        {
            int currentRoomIndex = _roomQueue.Dequeue();
            int nextRoomDepth = _roomDepths[currentRoomIndex] + 1;

            for (int linkIndex = 0; linkIndex < _links.Count; linkIndex++)
            {
                RoomLink roomLink = _links[linkIndex];
                int neighborRoomIndex = GetNeighborRoomIndex(currentRoomIndex, roomLink);

                if (neighborRoomIndex < 0)
                {
                    continue;
                }

                if (neighborRoomIndex >= _rooms.Count)
                {
                    continue;
                }

                if (_rooms[neighborRoomIndex] == null)
                {
                    continue;
                }

                if (_roomDepths[neighborRoomIndex] >= 0)
                {
                    continue;
                }

                _roomDepths[neighborRoomIndex] = nextRoomDepth;
                _roomQueue.Enqueue(neighborRoomIndex);
            }
        }
    }

    private int GetNeighborRoomIndex(int currentRoomIndex, RoomLink roomLink)
    {
        if (roomLink.FirstRoomIndex == currentRoomIndex)
        {
            return roomLink.SecondRoomIndex;
        }

        if (roomLink.SecondRoomIndex == currentRoomIndex)
        {
            return roomLink.FirstRoomIndex;
        }

        return -1;
    }

    private readonly struct RoomLink
    {
        public readonly int FirstRoomIndex;
        public readonly int SecondRoomIndex;

        public RoomLink(int firstRoomIndex, int secondRoomIndex)
        {
            FirstRoomIndex = firstRoomIndex;
            SecondRoomIndex = secondRoomIndex;
        }
    }
}

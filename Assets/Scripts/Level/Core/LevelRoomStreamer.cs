using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelRoomStreamer : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField, Min(0f)] private float _enableDistance = 35f;
    [SerializeField, Min(0f)] private float _disableDistance = 45f;
    [SerializeField, Min(0f)] private float _startDelay = 0.5f;
    [SerializeField, Min(0.02f)] private float _updateInterval = 0.2f;

    private readonly List<RoomRuntimeState> _rooms = new List<RoomRuntimeState>(64);

    private LevelRuntimeNavMesh _levelRuntimeNavMesh;
    private Transform _playerTrack;
    private Coroutine _streamCoroutine;
    private WaitForSecondsRealtime _updateDelay;
    private float _startTime;
    private bool _isStarted;

    public void Setup(Transform player, float enableDistance, float disableDistance, float startDelay, IReadOnlyList<RoomRuntimeState> rooms)
    {
        _player = player;
        _enableDistance = Mathf.Max(0f, enableDistance);
        _disableDistance = Mathf.Max(_enableDistance, disableDistance);
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

        for (int index = 0; index < rooms.Count; index++)
        {
            RoomRuntimeState roomRuntimeState = rooms[index];

            if (roomRuntimeState == null)
            {
                continue;
            }

            roomRuntimeState.SetRoomActive(true);
            _rooms.Add(roomRuntimeState);
        }

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
        float enableDistanceSqr = _enableDistance * _enableDistance;
        float disableDistanceSqr = _disableDistance * _disableDistance;
        bool hasRoomStateChanged = false;

        for (int index = _rooms.Count - 1; index >= 0; index--)
        {
            RoomRuntimeState roomRuntimeState = _rooms[index];

            if (roomRuntimeState == null)
            {
                _rooms.RemoveAt(index);

                continue;
            }

            float distanceSqr = roomRuntimeState.GetDistanceSqr(playerPoint);
            bool isRoomActive = roomRuntimeState.gameObject.activeSelf;

            if (isRoomActive)
            {
                if (distanceSqr > disableDistanceSqr)
                {
                    roomRuntimeState.SetRoomActive(false);
                    hasRoomStateChanged = true;
                }

                continue;
            }

            if (distanceSqr <= enableDistanceSqr)
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
}

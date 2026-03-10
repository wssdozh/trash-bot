using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LevelRoomStreamer : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField, Min(0f)] private float _enableDistance = 35f;
    [SerializeField, Min(0f)] private float _disableDistance = 45f;
    [SerializeField, Min(0f)] private float _startDelay = 0.5f;

    private readonly List<RoomRuntimeState> _rooms = new List<RoomRuntimeState>(64);

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
        enabled = false;
    }

    private void LateUpdate()
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
        if (_player != null)
        {
            return _player;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            _player = playerObject.transform;

            return _player;
        }

        Player player = FindFirstObjectByType<Player>();

        if (player == null)
        {
            return null;
        }

        _player = player.transform;

        return _player;
    }

    private void UpdateRooms(Vector3 playerPoint)
    {
        float enableDistanceSqr = _enableDistance * _enableDistance;
        float disableDistanceSqr = _disableDistance * _disableDistance;

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
                }

                continue;
            }

            if (distanceSqr <= enableDistanceSqr)
            {
                roomRuntimeState.SetRoomActive(true);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomRuntimeState))]
public sealed class RoomCombatLock : MonoBehaviour
{
    private const float MinBlockSize = 0.0001f;

    [SerializeField] private Transform _gatesRoot;
    [SerializeField] private Transform _enterTriggersRoot;

    private readonly List<Enemy> _enemies = new List<Enemy>(16);
    private readonly List<Turret> _turrets = new List<Turret>(8);
    private readonly List<ExcavatorBoss> _bosses = new List<ExcavatorBoss>(2);
    private readonly List<RoomDoorGate> _doorGates = new List<RoomDoorGate>(4);
    private readonly List<RoomEnterTrigger> _roomEnterTriggers = new List<RoomEnterTrigger>(4);

    private RoomRuntimeState _roomRuntimeState;
    private float _blockSize = 1f;
    private bool _isLocked;
    private bool _isCleared;

    private void Awake()
    {
        _roomRuntimeState = GetComponent<RoomRuntimeState>();
    }

    private void OnEnable()
    {
        if (_roomRuntimeState == null)
        {
            _roomRuntimeState = GetComponent<RoomRuntimeState>();
        }

        SubscribeEnterTriggers();
        RefreshThreats();
        SetBossesActive(_isLocked);
        EvaluateThreatState(false);
    }

    public void Setup(RoomRuntimeState roomRuntimeState, float blockSize)
    {
        if (roomRuntimeState == null)
        {
            throw new InvalidOperationException(nameof(roomRuntimeState));
        }

        _roomRuntimeState = roomRuntimeState;

        if (blockSize <= MinBlockSize)
        {
            _blockSize = MinBlockSize;
        }
        else
        {
            _blockSize = blockSize;
        }

        BuildGates();
        BuildEnterTriggers();
        SubscribeEnterTriggers();
        RefreshThreats();
        _isLocked = false;
        _isCleared = HasAliveThreats() == false;
        SetBossesActive(false);
        SetGatesClosed(false, true);
        EvaluateThreatState(false);
    }

    private void OnDisable()
    {
        UnsubscribeEnterTriggers();
        SetBossesActive(false);
        UnsubscribeThreats();
    }

    private void LockRoom()
    {
        if (HasAliveThreats() == false)
        {
            _isCleared = true;

            return;
        }

        if (_doorGates.Count == 0)
        {
            BuildGates();
        }

        _isLocked = true;
        SetGatesClosed(true, false);
    }

    private void UnlockRoom()
    {
        _isLocked = false;
        _isCleared = true;
        SetGatesClosed(false, false);
        UnsubscribeThreats();
    }

    private void BuildEnterTriggers()
    {
        Transform enterTriggersRoot = GetEnterTriggersRoot();
        RoomDoorMarker[] roomDoorMarkers = GetComponentsInChildren<RoomDoorMarker>(true);

        UnsubscribeEnterTriggers();
        RemoveLegacyEnterTrigger();
        ClearChildren(enterTriggersRoot);
        _roomEnterTriggers.Clear();

        for (int markerIndex = 0; markerIndex < roomDoorMarkers.Length; markerIndex++)
        {
            RoomDoorMarker roomDoorMarker = roomDoorMarkers[markerIndex];

            if (roomDoorMarker == null)
            {
                continue;
            }

            GameObject triggerObject = new GameObject("Room Enter Trigger");
            triggerObject.transform.SetParent(enterTriggersRoot, false);
            RoomEnterTrigger roomEnterTrigger = triggerObject.AddComponent<RoomEnterTrigger>();
            roomEnterTrigger.Setup(roomDoorMarker, _blockSize);
            _roomEnterTriggers.Add(roomEnterTrigger);
        }
    }

    private void SubscribeEnterTriggers()
    {
        for (int triggerIndex = 0; triggerIndex < _roomEnterTriggers.Count; triggerIndex++)
        {
            RoomEnterTrigger roomEnterTrigger = _roomEnterTriggers[triggerIndex];

            if (roomEnterTrigger == null)
            {
                continue;
            }

            roomEnterTrigger.Entered -= OnRoomEntered;
            roomEnterTrigger.Entered += OnRoomEntered;
        }
    }

    private void UnsubscribeEnterTriggers()
    {
        for (int triggerIndex = 0; triggerIndex < _roomEnterTriggers.Count; triggerIndex++)
        {
            RoomEnterTrigger roomEnterTrigger = _roomEnterTriggers[triggerIndex];

            if (roomEnterTrigger == null)
            {
                continue;
            }

            roomEnterTrigger.Entered -= OnRoomEntered;
        }
    }

    private void BuildGates()
    {
        Transform gatesRoot = GetGatesRoot();
        RoomDoorMarker[] roomDoorMarkers = GetComponentsInChildren<RoomDoorMarker>(true);

        ClearChildren(gatesRoot);
        _doorGates.Clear();

        for (int markerIndex = 0; markerIndex < roomDoorMarkers.Length; markerIndex++)
        {
            RoomDoorMarker roomDoorMarker = roomDoorMarkers[markerIndex];

            if (roomDoorMarker == null)
            {
                continue;
            }

            GameObject gateObject = new GameObject("Door Gate");
            gateObject.transform.SetParent(gatesRoot, true);
            RoomDoorGate roomDoorGate = gateObject.AddComponent<RoomDoorGate>();
            roomDoorGate.Setup(roomDoorMarker, _blockSize);
            _doorGates.Add(roomDoorGate);
        }
    }

    private void SetGatesClosed(bool isClosed, bool isInstant)
    {
        for (int gateIndex = 0; gateIndex < _doorGates.Count; gateIndex++)
        {
            RoomDoorGate roomDoorGate = _doorGates[gateIndex];

            if (roomDoorGate == null)
            {
                continue;
            }

            roomDoorGate.SetClosed(isClosed, isInstant);
        }
    }

    private void RefreshThreats()
    {
        UnsubscribeThreats();
        _enemies.Clear();
        _turrets.Clear();
        _bosses.Clear();

        Enemy[] roomEnemies = GetComponentsInChildren<Enemy>(true);

        for (int enemyIndex = 0; enemyIndex < roomEnemies.Length; enemyIndex++)
        {
            Enemy enemy = roomEnemies[enemyIndex];

            if (enemy == null)
            {
                continue;
            }

            _enemies.Add(enemy);
            enemy.Died += OnThreatDied;
        }

        Turret[] roomTurrets = GetComponentsInChildren<Turret>(true);

        for (int turretIndex = 0; turretIndex < roomTurrets.Length; turretIndex++)
        {
            Turret turret = roomTurrets[turretIndex];

            if (turret == null)
            {
                continue;
            }

            _turrets.Add(turret);
            turret.Died += OnThreatDied;
        }

        ExcavatorBoss[] roomBosses = GetComponentsInChildren<ExcavatorBoss>(true);

        for (int bossIndex = 0; bossIndex < roomBosses.Length; bossIndex++)
        {
            ExcavatorBoss excavatorBoss = roomBosses[bossIndex];

            if (excavatorBoss == null)
            {
                continue;
            }

            _bosses.Add(excavatorBoss);
            excavatorBoss.Died += OnThreatDied;
        }
    }

    private void UnsubscribeThreats()
    {
        for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
        {
            Enemy enemy = _enemies[enemyIndex];

            if (enemy == null)
            {
                continue;
            }

            enemy.Died -= OnThreatDied;
        }

        for (int turretIndex = 0; turretIndex < _turrets.Count; turretIndex++)
        {
            Turret turret = _turrets[turretIndex];

            if (turret == null)
            {
                continue;
            }

            turret.Died -= OnThreatDied;
        }

        for (int bossIndex = 0; bossIndex < _bosses.Count; bossIndex++)
        {
            ExcavatorBoss excavatorBoss = _bosses[bossIndex];

            if (excavatorBoss == null)
            {
                continue;
            }

            excavatorBoss.Died -= OnThreatDied;
        }
    }

    private void SetBossesActive(bool isActive)
    {
        for (int bossIndex = 0; bossIndex < _bosses.Count; bossIndex++)
        {
            ExcavatorBoss excavatorBoss = _bosses[bossIndex];

            if (excavatorBoss == null)
            {
                continue;
            }

            excavatorBoss.SetCombatActive(isActive);
        }
    }

    private bool HasAliveThreats()
    {
        for (int enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
        {
            Enemy enemy = _enemies[enemyIndex];

            if (enemy == null)
            {
                continue;
            }

            if (enemy.IsDead == false)
            {
                return true;
            }
        }

        for (int turretIndex = 0; turretIndex < _turrets.Count; turretIndex++)
        {
            Turret turret = _turrets[turretIndex];

            if (turret == null)
            {
                continue;
            }

            if (turret.IsDead == false)
            {
                return true;
            }
        }

        for (int bossIndex = 0; bossIndex < _bosses.Count; bossIndex++)
        {
            ExcavatorBoss excavatorBoss = _bosses[bossIndex];

            if (excavatorBoss == null)
            {
                continue;
            }

            if (excavatorBoss.IsDead == false)
            {
                return true;
            }
        }

        return false;
    }

    private Transform GetGatesRoot()
    {
        if (_gatesRoot != null)
        {
            return _gatesRoot;
        }

        Transform existingRoot = transform.Find("Combat Gates");

        if (existingRoot != null)
        {
            _gatesRoot = existingRoot;

            return _gatesRoot;
        }

        GameObject gatesObject = new GameObject("Combat Gates");
        _gatesRoot = gatesObject.transform;
        _gatesRoot.SetParent(transform, false);

        return _gatesRoot;
    }

    private Transform GetEnterTriggersRoot()
    {
        if (_enterTriggersRoot != null)
        {
            return _enterTriggersRoot;
        }

        Transform existingRoot = transform.Find("Room Enter Triggers");

        if (existingRoot != null)
        {
            _enterTriggersRoot = existingRoot;

            return _enterTriggersRoot;
        }

        GameObject enterTriggersObject = new GameObject("Room Enter Triggers");
        _enterTriggersRoot = enterTriggersObject.transform;
        _enterTriggersRoot.SetParent(transform, false);

        return _enterTriggersRoot;
    }

    private void RemoveLegacyEnterTrigger()
    {
        Transform legacyTrigger = transform.Find("Room Enter Trigger");

        if (legacyTrigger == null)
        {
            return;
        }

        DestroyGameObject(legacyTrigger.gameObject);
    }

    private void ClearChildren(Transform rootTransform)
    {
        int childCount = rootTransform.childCount;

        for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform childTransform = rootTransform.GetChild(childIndex);
            DestroyGameObject(childTransform.gameObject);
        }
    }

    private void DestroyGameObject(GameObject gameObject)
    {
        if (Application.isPlaying == false)
        {
            DestroyImmediate(gameObject);

            return;
        }

        Destroy(gameObject);
    }

    private void ShowClearView()
    {
        Player player = FindFirstObjectByType<Player>();

        if (player != null)
        {
            Texter playerTexter = player.GetComponentInChildren<Texter>(true);

            if (playerTexter != null && playerTexter.CanShowRoomClear())
            {
                playerTexter.ShowRoomClear();

                return;
            }
        }

        Texter[] texters = FindObjectsByType<Texter>(FindObjectsSortMode.None);

        for (int texterIndex = 0; texterIndex < texters.Length; texterIndex++)
        {
            Texter texter = texters[texterIndex];

            if (texter.CanShowRoomClear() == false)
            {
                continue;
            }

            texter.ShowRoomClear();

            return;
        }
    }

    private void OnRoomEntered()
    {
        if (_isCleared)
        {
            return;
        }

        if (_isLocked)
        {
            return;
        }

        SetBossesActive(true);
        LockRoom();
    }

    private void OnThreatDied()
    {
        EvaluateThreatState(true);
    }

    private void EvaluateThreatState(bool isThreatDied)
    {
        if (_roomRuntimeState == null)
        {
            return;
        }

        if (_isCleared)
        {
            return;
        }

        if (HasAliveThreats())
        {
            return;
        }

        if (_isLocked)
        {
            UnlockRoom();

            if (isThreatDied)
            {
                ShowClearView();
            }

            return;
        }

        _isCleared = true;

        if (isThreatDied)
        {
            ShowClearView();
        }
    }
}

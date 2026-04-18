using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorPhaseThreeController
    {
        private const string MinionsRootName = "Boss Phase Three Minions";
        private const float AlertInterval = 0.12f;

        private readonly BossExcavator _boss;
        private readonly List<BossExcavatorPhaseThreeMinion> _activeMinions = new List<BossExcavatorPhaseThreeMinion>(16);
        private readonly List<BossRoomEnemySpawnPoint> _spawnPoints = new List<BossRoomEnemySpawnPoint>(8);
        private readonly HashSet<int> _usedPrefabIds = new HashSet<int>();
        private readonly System.Random _random;

        private RoomRuntimeState _roomRuntimeState;
        private Transform _minionsRoot;
        private BossExcavatorPhase _lastPhase;
        private float _alertTimer;
        private int _nextWaveSize;
        private bool _isPhaseThreeStarted;
        private bool _isBossDeathHandled;

        public BossExcavatorPhaseThreeController(BossExcavator boss)
        {
            if (boss == null)
            {
                throw new InvalidOperationException(nameof(boss));
            }

            _boss = boss;
            _random = new System.Random(_boss.GetInstanceID());
            _lastPhase = _boss.Phase;
            _nextWaveSize = _boss.Config.PhaseThreeInitialWaveSize;
        }

        public void Reset()
        {
            ForceClearAll();
            _lastPhase = _boss.Phase;
            _alertTimer = 0f;
            _nextWaveSize = _boss.Config.PhaseThreeInitialWaveSize;
            _isPhaseThreeStarted = false;
            _isBossDeathHandled = false;
        }

        public void Tick()
        {
            CleanupReleasedMinions();

            if (UpdatePhaseState())
            {
                return;
            }

            if (_boss.IsDead)
            {
                if (_isBossDeathHandled == false)
                {
                    KillAllMinions();
                    _isBossDeathHandled = true;
                }

                return;
            }

            _isBossDeathHandled = false;

            if (_boss.IsFinalPhase == false)
            {
                return;
            }

            if (_isPhaseThreeStarted == false)
            {
                _isPhaseThreeStarted = true;
                TrySpawnNextWave();

                return;
            }

            TickAlerts();

            if (_activeMinions.Count > 0)
            {
                return;
            }

            TrySpawnNextWave();
        }

        private bool UpdatePhaseState()
        {
            if (_lastPhase == _boss.Phase)
            {
                return false;
            }

            _lastPhase = _boss.Phase;

            if (_boss.IsFinalPhase)
            {
                _isPhaseThreeStarted = true;
                _nextWaveSize = _boss.Config.PhaseThreeInitialWaveSize;
                TrySpawnNextWave();

                return true;
            }

            ForceClearAll();
            _nextWaveSize = _boss.Config.PhaseThreeInitialWaveSize;
            _isPhaseThreeStarted = false;

            return true;
        }

        private void TickAlerts()
        {
            Transform target = _boss.Target;

            if (target == null)
            {
                return;
            }

            _alertTimer -= Time.deltaTime;

            if (_alertTimer > 0f)
            {
                return;
            }

            AlertMinions(target.position);
            _alertTimer = AlertInterval;
        }

        private int TrySpawnNextWave()
        {
            if (ResolveRoomRuntimeState() == false)
            {
                return 0;
            }

            EnsureRoomAlert();
            EnsureMinionsRoot();
            RefreshSpawnPoints();

            IReadOnlyList<EnemySpawnConfig> enemySpawns = _boss.Config.PhaseThreeEnemyPrefabs;

            if (enemySpawns.Count == 0)
            {
                return 0;
            }

            if (_spawnPoints.Count == 0)
            {
                return 0;
            }

            List<BossRoomEnemySpawnPoint> availableSpawnPoints = new List<BossRoomEnemySpawnPoint>(_spawnPoints.Count);
            int spawnPointIndex = 0;

            while (spawnPointIndex < _spawnPoints.Count)
            {
                BossRoomEnemySpawnPoint spawnPoint = _spawnPoints[spawnPointIndex];
                spawnPointIndex += 1;

                if (spawnPoint == null)
                {
                    continue;
                }

                if (spawnPoint.IsBusy)
                {
                    continue;
                }

                availableSpawnPoints.Add(spawnPoint);
            }

            int targetSpawnCount = Mathf.Min(Mathf.Max(_nextWaveSize, 1), availableSpawnPoints.Count);

            if (targetSpawnCount <= 0)
            {
                return 0;
            }

            int spawnedCount = 0;
            _usedPrefabIds.Clear();

            while (spawnedCount < targetSpawnCount)
            {
                EnemySpawnConfig enemySpawn = PickEnemySpawn(enemySpawns);

                if (enemySpawn == null)
                {
                    break;
                }

                BossRoomEnemySpawnPoint spawnPoint = PickSpawnPoint(availableSpawnPoints);

                if (spawnPoint == null)
                {
                    break;
                }

                BossExcavatorPhaseThreeMinion minion = spawnPoint.TrySpawn(enemySpawn, _minionsRoot, _roomRuntimeState);
                availableSpawnPoints.Remove(spawnPoint);

                if (minion == null)
                {
                    continue;
                }

                minion.Died += OnMinionDied;
                _activeMinions.Add(minion);
                _usedPrefabIds.Add(enemySpawn.Prefab.GetInstanceID());
                spawnedCount += 1;
            }

            if (spawnedCount <= 0)
            {
                return 0;
            }

            _nextWaveSize += _boss.Config.PhaseThreeWaveSizeStep;
            _alertTimer = 0f;
            AlertMinions(_boss.Target != null ? _boss.Target.position : _boss.transform.position);

            return spawnedCount;
        }

        private BossRoomEnemySpawnPoint PickSpawnPoint(List<BossRoomEnemySpawnPoint> availableSpawnPoints)
        {
            if (availableSpawnPoints == null)
            {
                return null;
            }

            if (availableSpawnPoints.Count == 0)
            {
                return null;
            }

            int pickIndex = _random.Next(0, availableSpawnPoints.Count);

            return availableSpawnPoints[pickIndex];
        }

        private EnemySpawnConfig PickEnemySpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns)
        {
            bool preferUnique = HasUniqueSpawn(enemySpawns);
            EnemySpawnConfig pickedSpawn = PickWeightedEnemySpawn(enemySpawns, preferUnique);

            if (pickedSpawn != null)
            {
                return pickedSpawn;
            }

            if (preferUnique == false)
            {
                return null;
            }

            return PickWeightedEnemySpawn(enemySpawns, false);
        }

        private bool HasUniqueSpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns)
        {
            int spawnIndex = 0;

            while (spawnIndex < enemySpawns.Count)
            {
                EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];
                spawnIndex += 1;

                if (IsValidEnemySpawn(enemySpawn) == false)
                {
                    continue;
                }

                int prefabId = enemySpawn.Prefab.GetInstanceID();

                if (_usedPrefabIds.Contains(prefabId))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private EnemySpawnConfig PickWeightedEnemySpawn(IReadOnlyList<EnemySpawnConfig> enemySpawns, bool uniqueOnly)
        {
            int totalWeight = 0;
            int spawnIndex = 0;

            while (spawnIndex < enemySpawns.Count)
            {
                EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];
                spawnIndex += 1;

                if (IsValidEnemySpawn(enemySpawn) == false)
                {
                    continue;
                }

                if (uniqueOnly)
                {
                    int prefabId = enemySpawn.Prefab.GetInstanceID();

                    if (_usedPrefabIds.Contains(prefabId))
                    {
                        continue;
                    }
                }

                totalWeight += Mathf.Max(enemySpawn.Weight, 1);
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int weightPick = _random.Next(0, totalWeight);
            int accumulatedWeight = 0;
            spawnIndex = 0;

            while (spawnIndex < enemySpawns.Count)
            {
                EnemySpawnConfig enemySpawn = enemySpawns[spawnIndex];
                spawnIndex += 1;

                if (IsValidEnemySpawn(enemySpawn) == false)
                {
                    continue;
                }

                if (uniqueOnly)
                {
                    int prefabId = enemySpawn.Prefab.GetInstanceID();

                    if (_usedPrefabIds.Contains(prefabId))
                    {
                        continue;
                    }
                }

                accumulatedWeight += Mathf.Max(enemySpawn.Weight, 1);

                if (weightPick < accumulatedWeight)
                {
                    return enemySpawn;
                }
            }

            return null;
        }

        private bool IsValidEnemySpawn(EnemySpawnConfig enemySpawn)
        {
            if (enemySpawn == null)
            {
                return false;
            }

            if (enemySpawn.Prefab == null)
            {
                return false;
            }

            return true;
        }

        private void AlertMinions(Vector3 point)
        {
            int minionIndex = 0;

            while (minionIndex < _activeMinions.Count)
            {
                BossExcavatorPhaseThreeMinion minion = _activeMinions[minionIndex];
                minionIndex += 1;

                if (minion == null)
                {
                    continue;
                }

                minion.Alert(point);
            }
        }

        private void KillAllMinions()
        {
            int minionIndex = _activeMinions.Count - 1;

            while (minionIndex >= 0)
            {
                BossExcavatorPhaseThreeMinion minion = _activeMinions[minionIndex];
                minionIndex -= 1;

                if (minion == null)
                {
                    continue;
                }

                minion.Died -= OnMinionDied;
                minion.Kill();
                minion.Dispose();
            }

            _activeMinions.Clear();
        }

        private void CleanupReleasedMinions()
        {
            int minionIndex = _activeMinions.Count - 1;

            while (minionIndex >= 0)
            {
                BossExcavatorPhaseThreeMinion minion = _activeMinions[minionIndex];

                if (minion != null)
                {
                    if (minion.RootObject != null)
                    {
                        if (minion.IsAlive)
                        {
                            minionIndex -= 1;

                            continue;
                        }
                    }
                }

                RemoveMinionAt(minionIndex);
                minionIndex -= 1;
            }
        }

        private void OnMinionDied(BossExcavatorPhaseThreeMinion minion)
        {
            int minionIndex = _activeMinions.IndexOf(minion);

            if (minionIndex < 0)
            {
                return;
            }

            RemoveMinionAt(minionIndex);
        }

        private void RemoveMinionAt(int minionIndex)
        {
            BossExcavatorPhaseThreeMinion minion = _activeMinions[minionIndex];
            _activeMinions.RemoveAt(minionIndex);

            if (minion == null)
            {
                return;
            }

            minion.Died -= OnMinionDied;
            minion.Dispose();
        }

        private void ForceClearAll()
        {
            if (ResolveRoomRuntimeState())
            {
                RefreshSpawnPoints();

                int spawnPointIndex = 0;

                while (spawnPointIndex < _spawnPoints.Count)
                {
                    BossRoomEnemySpawnPoint spawnPoint = _spawnPoints[spawnPointIndex];
                    spawnPointIndex += 1;

                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    spawnPoint.ForceClear();
                }
            }

            int minionIndex = 0;

            while (minionIndex < _activeMinions.Count)
            {
                BossExcavatorPhaseThreeMinion minion = _activeMinions[minionIndex];
                minionIndex += 1;

                if (minion == null)
                {
                    continue;
                }

                minion.Died -= OnMinionDied;
                minion.Dispose();
            }

            _activeMinions.Clear();
        }

        private bool ResolveRoomRuntimeState()
        {
            if (_roomRuntimeState != null)
            {
                return true;
            }

            if (_boss.Base != null)
            {
                _roomRuntimeState = _boss.Base.GetComponentInParent<RoomRuntimeState>();

                if (_roomRuntimeState != null)
                {
                    return true;
                }
            }

            RoomCombatLock roomCombatLock = null;

            if (_boss.Base != null)
            {
                roomCombatLock = _boss.Base.GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock == null)
            {
                roomCombatLock = _boss.GetComponentInParent<RoomCombatLock>();
            }

            if (roomCombatLock == null)
            {
                return false;
            }

            _roomRuntimeState = roomCombatLock.GetComponent<RoomRuntimeState>();

            return _roomRuntimeState != null;
        }

        private void EnsureRoomAlert()
        {
            EnemyRoomAlert enemyRoomAlert = _roomRuntimeState.GetComponent<EnemyRoomAlert>();

            if (enemyRoomAlert != null)
            {
                return;
            }

            _roomRuntimeState.gameObject.AddComponent<EnemyRoomAlert>();
        }

        private void EnsureMinionsRoot()
        {
            if (_minionsRoot != null)
            {
                return;
            }

            Transform existingRoot = _roomRuntimeState.transform.Find(MinionsRootName);

            if (existingRoot != null)
            {
                _minionsRoot = existingRoot;

                return;
            }

            GameObject rootObject = new GameObject(MinionsRootName);
            _minionsRoot = rootObject.transform;
            _minionsRoot.SetParent(_roomRuntimeState.transform, false);
        }

        private void RefreshSpawnPoints()
        {
            _spawnPoints.Clear();

            BossRoomEnemySpawnPoint[] spawnPoints = _roomRuntimeState.GetComponentsInChildren<BossRoomEnemySpawnPoint>(true);
            int spawnPointIndex = 0;

            while (spawnPointIndex < spawnPoints.Length)
            {
                BossRoomEnemySpawnPoint spawnPoint = spawnPoints[spawnPointIndex];
                spawnPointIndex += 1;

                if (spawnPoint == null)
                {
                    continue;
                }

                _spawnPoints.Add(spawnPoint);
            }
        }
    }
}

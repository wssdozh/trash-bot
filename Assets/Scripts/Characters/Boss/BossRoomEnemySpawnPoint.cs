using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossRoomEnemySpawnPoint : MonoBehaviour
    {
        private const float MinBlockSize = 0.0001f;
        private const float MinRiseDuration = 0.05f;

        [SerializeField] private float _riseDuration = 0.65f;
        [SerializeField] private float _riseDepthInBlocks = 1.45f;
        [SerializeField] private float _spawnHeightOffsetInBlocks = 0f;

        private readonly List<Collider> _disabledColliders = new List<Collider>(16);
        private readonly List<Rigidbody> _disabledRigidbodies = new List<Rigidbody>(8);
        private readonly List<bool> _disabledRigidbodiesWasKinematic = new List<bool>(8);
        private readonly List<MonoBehaviour> _disabledBehaviours = new List<MonoBehaviour>(12);

        private BossExcavatorPhaseThreeMinion _currentMinion;
        private GameObject _currentRootObject;
        private RoomRuntimeState _roomRuntimeState;
        private Vector3 _riseStartPoint;
        private Vector3 _riseEndPoint;
        private float _riseTimer;
        private float _blockSize = 1f;
        private bool _isRising;

        public bool IsBusy
        {
            get
            {
                return _currentRootObject != null;
            }
        }

        public void SetBlockSize(float blockSize)
        {
            if (blockSize <= MinBlockSize)
            {
                _blockSize = MinBlockSize;

                return;
            }

            _blockSize = blockSize;
        }

        public BossExcavatorPhaseThreeMinion TrySpawn(EnemySpawnConfig enemySpawn, Transform spawnRoot, RoomRuntimeState roomRuntimeState)
        {
            if (enemySpawn == null)
            {
                throw new InvalidOperationException(nameof(enemySpawn));
            }

            if (spawnRoot == null)
            {
                throw new InvalidOperationException(nameof(spawnRoot));
            }

            if (roomRuntimeState == null)
            {
                throw new InvalidOperationException(nameof(roomRuntimeState));
            }

            if (IsBusy)
            {
                return null;
            }

            if (enemySpawn.Prefab == null)
            {
                return null;
            }

            GameObject enemyObject = UnityEngine.Object.Instantiate(enemySpawn.Prefab, GetSpawnAnchor(), transform.rotation);
            enemyObject.transform.SetParent(spawnRoot, true);

            DisableSummonDrops(enemyObject);
            SetupEnemy(enemyObject, enemySpawn);

            Vector3 endPoint = ResolveEndPoint(enemyObject, enemySpawn);
            Vector3 startPoint = endPoint + (Vector3.down * GetRiseDepth(enemyObject));

            enemyObject.transform.position = startPoint;
            DisableSpawnRuntime(enemyObject);

            _roomRuntimeState = roomRuntimeState;
            _currentRootObject = enemyObject;
            _currentMinion = new BossExcavatorPhaseThreeMinion(this, enemyObject);
            _currentMinion.Died += OnCurrentMinionDied;
            _riseStartPoint = startPoint;
            _riseEndPoint = endPoint;
            _riseTimer = 0f;
            _isRising = true;

            return _currentMinion;
        }

        public void ForceClear()
        {
            if (_currentMinion != null)
            {
                _currentMinion.Died -= OnCurrentMinionDied;
                _currentMinion.Dispose();
            }

            if (_currentRootObject != null)
            {
                UnityEngine.Object.Destroy(_currentRootObject);
            }

            ClearCurrentRuntime();
        }

        private void Update()
        {
            if (_currentRootObject == null)
            {
                if (_currentMinion != null)
                {
                    ClearCurrentRuntime();
                }

                return;
            }

            if (_isRising == false)
            {
                return;
            }

            TickRise();
        }

        private void TickRise()
        {
            _riseTimer += Time.deltaTime;
            float riseDuration = Mathf.Max(_riseDuration, MinRiseDuration);
            float riseProgress = Mathf.Clamp01(_riseTimer / riseDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, riseProgress);
            _currentRootObject.transform.position = Vector3.Lerp(_riseStartPoint, _riseEndPoint, easedProgress);

            if (riseProgress < 1f)
            {
                return;
            }

            _isRising = false;
            BindEnemyRoom(_currentRootObject);
            RestoreSpawnRuntime();
        }

        private void DisableSummonDrops(GameObject enemyObject)
        {
            CurrencyDropOnDeath[] currencyDrops = enemyObject.GetComponentsInChildren<CurrencyDropOnDeath>(true);
            int currencyIndex = 0;

            while (currencyIndex < currencyDrops.Length)
            {
                CurrencyDropOnDeath currencyDrop = currencyDrops[currencyIndex];
                currencyIndex += 1;

                if (currencyDrop == null)
                {
                    continue;
                }

                currencyDrop.enabled = false;
            }

            PickupOnDeath[] pickups = enemyObject.GetComponentsInChildren<PickupOnDeath>(true);
            int pickupIndex = 0;

            while (pickupIndex < pickups.Length)
            {
                PickupOnDeath pickupOnDeath = pickups[pickupIndex];
                pickupIndex += 1;

                if (pickupOnDeath == null)
                {
                    continue;
                }

                pickupOnDeath.enabled = false;
            }
        }

        private void SetupEnemy(GameObject enemyObject, EnemySpawnConfig enemySpawn)
        {
            EnemyAnimation enemyAnimation = enemyObject.GetComponentInChildren<EnemyAnimation>(true);

            if (enemyAnimation != null)
            {
                enemyAnimation.SetWeapon(enemySpawn.WeaponPrefab);
            }

            EnemyMeleeBrain enemyMeleeBrain = enemyObject.GetComponentInChildren<EnemyMeleeBrain>(true);

            if (enemyMeleeBrain != null)
            {
                enemyMeleeBrain.ApplyRole();
            }
        }

        private Vector3 ResolveEndPoint(GameObject enemyObject, EnemySpawnConfig enemySpawn)
        {
            Vector3 endPoint = GetSpawnAnchor();

            if (IsAirEnemy(enemyObject) || IsTurretEnemy(enemyObject))
            {
                endPoint.y += enemySpawn.SpawnHeight * _blockSize;

                return endPoint;
            }

            Collider[] bodyColliders = enemyObject.GetComponentsInChildren<Collider>(true);
            float bottomOffset = GetBottomOffset(enemyObject.transform, bodyColliders);
            endPoint.y += bottomOffset;

            return endPoint;
        }

        private float GetRiseDepth(GameObject enemyObject)
        {
            float riseDepth = Mathf.Max(_riseDepthInBlocks * _blockSize, 0.5f);
            Collider[] bodyColliders = enemyObject.GetComponentsInChildren<Collider>(true);
            float colliderHeight = GetColliderHeight(bodyColliders);

            if (colliderHeight > riseDepth)
            {
                riseDepth = colliderHeight;
            }

            return riseDepth;
        }

        private Vector3 GetSpawnAnchor()
        {
            return transform.position + (Vector3.up * (_spawnHeightOffsetInBlocks * _blockSize));
        }

        private bool IsAirEnemy(GameObject enemyObject)
        {
            EnemyDroneMove enemyDroneMove = enemyObject.GetComponentInChildren<EnemyDroneMove>(true);

            if (enemyDroneMove != null)
            {
                return true;
            }

            return false;
        }

        private bool IsTurretEnemy(GameObject enemyObject)
        {
            Turret turret = enemyObject.GetComponentInChildren<Turret>(true);

            if (turret != null)
            {
                return true;
            }

            return false;
        }

        private float GetBottomOffset(Transform enemyTransform, Collider[] bodyColliders)
        {
            float lowestY = enemyTransform.position.y;
            bool hasCollider = false;
            int colliderIndex = 0;

            while (colliderIndex < bodyColliders.Length)
            {
                Collider bodyCollider = bodyColliders[colliderIndex];
                colliderIndex += 1;

                if (CanUseBodyCollider(bodyCollider) == false)
                {
                    continue;
                }

                float colliderBottomY = bodyCollider.bounds.min.y;

                if (hasCollider == false || colliderBottomY < lowestY)
                {
                    lowestY = colliderBottomY;
                    hasCollider = true;
                }
            }

            if (hasCollider == false)
            {
                return 0f;
            }

            return enemyTransform.position.y - lowestY;
        }

        private float GetColliderHeight(Collider[] bodyColliders)
        {
            float minY = 0f;
            float maxY = 0f;
            bool hasCollider = false;
            int colliderIndex = 0;

            while (colliderIndex < bodyColliders.Length)
            {
                Collider bodyCollider = bodyColliders[colliderIndex];
                colliderIndex += 1;

                if (CanUseBodyCollider(bodyCollider) == false)
                {
                    continue;
                }

                float colliderMinY = bodyCollider.bounds.min.y;
                float colliderMaxY = bodyCollider.bounds.max.y;

                if (hasCollider == false)
                {
                    minY = colliderMinY;
                    maxY = colliderMaxY;
                    hasCollider = true;

                    continue;
                }

                if (colliderMinY < minY)
                {
                    minY = colliderMinY;
                }

                if (colliderMaxY > maxY)
                {
                    maxY = colliderMaxY;
                }
            }

            if (hasCollider == false)
            {
                return 0f;
            }

            return maxY - minY;
        }

        private bool CanUseBodyCollider(Collider bodyCollider)
        {
            if (bodyCollider == null)
            {
                return false;
            }

            if (bodyCollider.enabled == false)
            {
                return false;
            }

            if (bodyCollider.isTrigger)
            {
                return false;
            }

            return true;
        }

        private void DisableSpawnRuntime(GameObject enemyObject)
        {
            Collider[] colliders = enemyObject.GetComponentsInChildren<Collider>(true);
            int colliderIndex = 0;

            while (colliderIndex < colliders.Length)
            {
                Collider collider = colliders[colliderIndex];
                colliderIndex += 1;

                if (collider == null)
                {
                    continue;
                }

                if (collider.enabled == false)
                {
                    continue;
                }

                _disabledColliders.Add(collider);
                collider.enabled = false;
            }

            Rigidbody[] rigidbodies = enemyObject.GetComponentsInChildren<Rigidbody>(true);
            int rigidbodyIndex = 0;

            while (rigidbodyIndex < rigidbodies.Length)
            {
                Rigidbody rigidbody = rigidbodies[rigidbodyIndex];
                rigidbodyIndex += 1;

                if (rigidbody == null)
                {
                    continue;
                }

                _disabledRigidbodies.Add(rigidbody);
                _disabledRigidbodiesWasKinematic.Add(rigidbody.isKinematic);
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                rigidbody.isKinematic = true;
            }

            DisableBehaviour(enemyObject.GetComponentInChildren<EnemyMeleeBrain>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<EnemyDroneBrain>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<TargetVision>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<TargetRotator>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<IdleRotator>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<FireExecutor>(true));
            DisableBehaviour(enemyObject.GetComponentInChildren<EnemyRoomLock>(true));
        }

        private void RestoreSpawnRuntime()
        {
            int rigidbodyIndex = 0;

            while (rigidbodyIndex < _disabledRigidbodies.Count)
            {
                Rigidbody rigidbody = _disabledRigidbodies[rigidbodyIndex];
                bool wasKinematic = _disabledRigidbodiesWasKinematic[rigidbodyIndex];
                rigidbodyIndex += 1;

                if (rigidbody == null)
                {
                    continue;
                }

                rigidbody.isKinematic = wasKinematic;
            }

            int colliderIndex = 0;

            while (colliderIndex < _disabledColliders.Count)
            {
                Collider collider = _disabledColliders[colliderIndex];
                colliderIndex += 1;

                if (collider == null)
                {
                    continue;
                }

                collider.enabled = true;
            }

            int behaviourIndex = 0;

            while (behaviourIndex < _disabledBehaviours.Count)
            {
                MonoBehaviour behaviour = _disabledBehaviours[behaviourIndex];
                behaviourIndex += 1;

                if (behaviour == null)
                {
                    continue;
                }

                behaviour.enabled = true;
            }

            _disabledRigidbodies.Clear();
            _disabledRigidbodiesWasKinematic.Clear();
            _disabledColliders.Clear();
            _disabledBehaviours.Clear();
        }

        private void DisableBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null)
            {
                return;
            }

            if (behaviour.enabled == false)
            {
                return;
            }

            _disabledBehaviours.Add(behaviour);
            behaviour.enabled = false;
        }

        private void BindEnemyRoom(GameObject enemyObject)
        {
            EnsureRoomAlert();

            EnemyRoomLock enemyRoomLock = enemyObject.GetComponent<EnemyRoomLock>();

            if (enemyRoomLock == null)
            {
                enemyRoomLock = enemyObject.AddComponent<EnemyRoomLock>();
            }

            enemyRoomLock.Setup(_roomRuntimeState);
            enemyRoomLock.SnapInside();
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

        private void OnCurrentMinionDied(BossExcavatorPhaseThreeMinion minion)
        {
            if (_currentMinion != minion)
            {
                return;
            }

            _currentMinion.Died -= OnCurrentMinionDied;

            if (_currentRootObject != null)
            {
                UnityEngine.Object.Destroy(_currentRootObject);
            }

            ClearCurrentRuntime();
        }

        private void ClearCurrentRuntime()
        {
            _currentMinion = null;
            _currentRootObject = null;
            _roomRuntimeState = null;
            _riseStartPoint = Vector3.zero;
            _riseEndPoint = Vector3.zero;
            _riseTimer = 0f;
            _isRising = false;
            _disabledRigidbodies.Clear();
            _disabledRigidbodiesWasKinematic.Clear();
            _disabledColliders.Clear();
            _disabledBehaviours.Clear();
        }
    }
}

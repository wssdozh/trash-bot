using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    public sealed class BossExcavatorPhaseThreeMinion
    {
        private readonly GameObject _rootObject;
        private readonly BossRoomEnemySpawnPoint _spawnPoint;
        private readonly Health _health;
        private readonly List<MonoBehaviour> _alertReceivers = new List<MonoBehaviour>(4);

        private bool _isDisposed;

        public event Action<BossExcavatorPhaseThreeMinion> Died;

        public BossRoomEnemySpawnPoint SpawnPoint => _spawnPoint;
        public GameObject RootObject => _rootObject;

        public bool IsAlive
        {
            get
            {
                if (_rootObject == null)
                {
                    return false;
                }

                if (_health == null)
                {
                    return false;
                }

                return _health.Value > _health.MinValue;
            }
        }

        public BossExcavatorPhaseThreeMinion(BossRoomEnemySpawnPoint spawnPoint, GameObject rootObject)
        {
            if (spawnPoint == null)
            {
                throw new InvalidOperationException(nameof(spawnPoint));
            }

            if (rootObject == null)
            {
                throw new InvalidOperationException(nameof(rootObject));
            }

            _spawnPoint = spawnPoint;
            _rootObject = rootObject;
            _health = rootObject.GetComponentInChildren<Health>(true);

            if (_health == null)
            {
                throw new InvalidOperationException(nameof(_health));
            }

            CollectAlertReceivers();
            _health.Ended += OnHealthEnded;
        }

        public void Alert(Vector3 point)
        {
            if (IsAlive == false)
            {
                return;
            }

            int receiverIndex = 0;

            while (receiverIndex < _alertReceivers.Count)
            {
                MonoBehaviour receiver = _alertReceivers[receiverIndex];
                receiverIndex += 1;

                if (receiver == null)
                {
                    continue;
                }

                IEnemyAlert enemyAlert = receiver as IEnemyAlert;

                if (enemyAlert == null)
                {
                    continue;
                }

                enemyAlert.ApplyAlert(point);
            }
        }

        public void Kill()
        {
            if (_health == null)
            {
                return;
            }

            if (_health.Value <= _health.MinValue)
            {
                return;
            }

            _health.Decrease(_health.MaxValue);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_health != null)
            {
                _health.Ended -= OnHealthEnded;
            }

            _alertReceivers.Clear();
        }

        private void CollectAlertReceivers()
        {
            MonoBehaviour[] receivers = _rootObject.GetComponentsInChildren<MonoBehaviour>(true);
            int receiverIndex = 0;

            while (receiverIndex < receivers.Length)
            {
                MonoBehaviour receiver = receivers[receiverIndex];
                receiverIndex += 1;

                if (receiver == null)
                {
                    continue;
                }

                IEnemyAlert enemyAlert = receiver as IEnemyAlert;

                if (enemyAlert == null)
                {
                    continue;
                }

                _alertReceivers.Add(receiver);
            }
        }

        private void OnHealthEnded()
        {
            if (_isDisposed)
            {
                return;
            }

            Dispose();
            Died?.Invoke(this);
        }
    }
}

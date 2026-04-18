using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapTrailBlock : MonoBehaviour
    {
        private const float SinkDuration = 1.15f;
        private const float SinkDistanceFactor = 1.35f;

        [SerializeField] private BoxCollider _boxCollider;

        private readonly List<Collider> _ignoredColliders = new List<Collider>(16);

        private BossScrapTrailBlockSpawner _spawner;
        private float _lifetimeTimer;
        private float _sinkTimer;
        private Vector3 _sinkStartPoint;
        private Vector3 _sinkEndPoint;
        private bool _isActive;
        private bool _isReturned;
        private bool _isSinking;

        public void BindSpawner(BossScrapTrailBlockSpawner spawner)
        {
            if (spawner == null)
            {
                throw new InvalidOperationException(nameof(spawner));
            }

            _spawner = spawner;
        }

        public void Activate(Vector3 position, Quaternion rotation, Vector3 size, float lifetime, Collider[] ignoredColliders)
        {
            if (_spawner == null)
            {
                throw new InvalidOperationException(nameof(_spawner));
            }

            if (_boxCollider == null)
            {
                throw new InvalidOperationException(nameof(_boxCollider));
            }

            if (lifetime <= 0f)
            {
                throw new InvalidOperationException(nameof(lifetime));
            }

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = size;
            _lifetimeTimer = lifetime;
            _sinkTimer = 0f;
            _sinkStartPoint = position;
            _sinkEndPoint = position;
            _isActive = true;
            _isReturned = false;
            _isSinking = false;
            _boxCollider.enabled = true;

            ApplyIgnoredCollisions(ignoredColliders);
        }

        private void Awake()
        {
            if (_boxCollider == null)
            {
                throw new InvalidOperationException(nameof(_boxCollider));
            }
        }

        private void Update()
        {
            if (_isActive == false)
            {
                return;
            }

            if (_isSinking)
            {
                TickSink();

                return;
            }

            _lifetimeTimer -= Time.deltaTime;

            if (_lifetimeTimer > 0f)
            {
                return;
            }

            StartSink();
        }

        private void OnDisable()
        {
            _isActive = false;
            _lifetimeTimer = 0f;
            _sinkTimer = 0f;
            _isSinking = false;
            _boxCollider.enabled = true;
            RestoreIgnoredCollisions();
        }

        private void ApplyIgnoredCollisions(Collider[] ignoredColliders)
        {
            RestoreIgnoredCollisions();

            if (ignoredColliders == null)
            {
                return;
            }

            int colliderIndex = 0;

            while (colliderIndex < ignoredColliders.Length)
            {
                Collider ignoredCollider = ignoredColliders[colliderIndex];
                colliderIndex += 1;

                if (ignoredCollider == null)
                {
                    continue;
                }

                if (ignoredCollider.enabled == false)
                {
                    continue;
                }

                if (ignoredCollider.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(_boxCollider, ignoredCollider, true);
                _ignoredColliders.Add(ignoredCollider);
            }
        }

        private void RestoreIgnoredCollisions()
        {
            int colliderIndex = 0;

            while (colliderIndex < _ignoredColliders.Count)
            {
                Collider ignoredCollider = _ignoredColliders[colliderIndex];
                colliderIndex += 1;

                if (ignoredCollider == null)
                {
                    continue;
                }

                Physics.IgnoreCollision(_boxCollider, ignoredCollider, false);
            }

            _ignoredColliders.Clear();
        }

        private void ReturnToPool()
        {
            if (_isReturned)
            {
                return;
            }

            _isReturned = true;
            _isActive = false;
            _spawner.Despawn(this);
        }

        private void StartSink()
        {
            if (_isSinking)
            {
                return;
            }

            _isSinking = true;
            _sinkTimer = 0f;
            _sinkStartPoint = transform.position;
            _sinkEndPoint = _sinkStartPoint + (Vector3.down * GetSinkDistance());
            _boxCollider.enabled = false;
        }

        private void TickSink()
        {
            _sinkTimer += Time.deltaTime;

            float sinkProgress = Mathf.Clamp01(_sinkTimer / SinkDuration);
            transform.position = Vector3.Lerp(_sinkStartPoint, _sinkEndPoint, sinkProgress);

            if (sinkProgress < 1f)
            {
                return;
            }

            ReturnToPool();
        }

        private float GetSinkDistance()
        {
            return Mathf.Max(transform.localScale.y * SinkDistanceFactor, 0.25f);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapTrailBlock : MonoBehaviour
    {
        [SerializeField] private BoxCollider _boxCollider;

        private readonly List<Collider> _ignoredColliders = new List<Collider>(16);

        private BossScrapTrailBlockSpawner _spawner;
        private float _lifetimeTimer;
        private bool _isActive;
        private bool _isReturned;

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
            _isActive = true;
            _isReturned = false;

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

            _lifetimeTimer -= Time.deltaTime;

            if (_lifetimeTimer > 0f)
            {
                return;
            }

            ReturnToPool();
        }

        private void OnDisable()
        {
            _isActive = false;
            _lifetimeTimer = 0f;
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
    }
}

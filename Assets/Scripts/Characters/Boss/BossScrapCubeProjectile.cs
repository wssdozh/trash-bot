using System;
using UnityEngine;

namespace JunkyardBoss
{
    [DisallowMultipleComponent]
    public sealed class BossScrapCubeProjectile : Ammo
    {
        private static readonly string[] AllowedNameTokens = { "Block", "Cube", "Wall" };
        private static readonly string[] IgnoredNameTokens = { "Floor", "Ground" };

        private const string PlayerLayerName = "Player";
        private const string GroundLayerName = "Ground";
        private const string EnvironmentLayerName = "Environment";
        private const string EnemiesLayerName = "Enemies";
        private const string ShellsLayerName = "shells";

        protected override void OnHitTarget(Collider other)
        {
            Health health = other.GetComponentInParent<Health>();

            if (health == null)
            {
                return;
            }

            health.Decrease(Damage);
        }

        protected override bool ShouldIgnoreCollision(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                return false;
            }

            int otherLayer = other.gameObject.layer;

            if (IsNamedLayer(otherLayer, GroundLayerName))
            {
                return true;
            }

            if (IsNamedLayer(otherLayer, EnemiesLayerName))
            {
                return true;
            }

            if (IsNamedLayer(otherLayer, ShellsLayerName))
            {
                return true;
            }

            if (IsNamedLayer(otherLayer, EnvironmentLayerName))
            {
                return false;
            }

            if (HasAllowedCollisionName(other.transform))
            {
                return false;
            }

            return true;
        }

        private bool IsPlayerCollider(Collider other)
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);

            if (other.gameObject.layer == playerLayer)
            {
                return true;
            }

            Player player = other.GetComponentInParent<Player>();

            if (player == null)
            {
                return false;
            }

            return true;
        }

        private bool IsNamedLayer(int layer, string layerName)
        {
            int namedLayer = LayerMask.NameToLayer(layerName);

            if (namedLayer < 0)
            {
                return false;
            }

            if (layer == namedLayer)
            {
                return true;
            }

            return false;
        }

        private bool HasAllowedCollisionName(Transform current)
        {
            bool hasAllowedToken = false;

            while (current != null)
            {
                string currentName = current.name;

                if (ContainsToken(currentName, IgnoredNameTokens))
                {
                    return false;
                }

                if (ContainsToken(currentName, AllowedNameTokens))
                {
                    hasAllowedToken = true;
                }

                current = current.parent;
            }

            return hasAllowedToken;
        }

        private bool ContainsToken(string source, string[] tokens)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            int tokenIndex = 0;

            while (tokenIndex < tokens.Length)
            {
                string token = tokens[tokenIndex];
                tokenIndex += 1;

                if (source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

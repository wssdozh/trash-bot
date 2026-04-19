using UnityEngine;

namespace JunkyardBoss
{
    public sealed partial class BossExcavator
    {
        private bool CanUseTarget()
        {
            if (_target == null)
            {
                return false;
            }

            if (IsTargetSelf(_target))
            {
                return false;
            }

            return true;
        }

        private bool TryResolveTarget()
        {
            if (CanUseTarget())
            {
                return true;
            }

            Player player = FindFirstObjectByType<Player>();

            if (player == null)
            {
                return false;
            }

            Transform playerBody = GetPlayerBody(player);

            if (playerBody == null)
            {
                return false;
            }

            if (IsTargetSelf(playerBody))
            {
                return false;
            }

            _target = playerBody;
            _move.SetTarget(playerBody);
            _aim.SetTarget(playerBody);

            return true;
        }

        private Transform GetPlayerBody(Player player)
        {
            if (player == null)
            {
                return null;
            }

            return player.transform.Find("Body");
        }

        private bool IsTargetSelf(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            if (target == transform)
            {
                return true;
            }

            return target.IsChildOf(transform);
        }
    }
}

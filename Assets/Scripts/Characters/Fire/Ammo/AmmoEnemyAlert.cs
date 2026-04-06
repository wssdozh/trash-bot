using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmmoEnemyAlert : AmmoLifeListener
{
    private const int AlertBufferSize = 16;
    private const int AlertIdSize = 16;

    [Header("Настройки")]
    [SerializeField] private float _alertRadius = 0.85f;

    private readonly Collider[] _alertBuffer = new Collider[AlertBufferSize];
    private readonly int[] _alertIds = new int[AlertIdSize];

    private bool _canAlertEnemies;
    private int _alertIdCount;

    protected override void Awake()
    {
        base.Awake();

        if (_alertRadius <= 0f)
        {
            throw new InvalidOperationException(nameof(_alertRadius));
        }
    }

    protected override void OnAmmoEnabled()
    {
        _alertIdCount = 0;
        _canAlertEnemies = CanAlertEnemies();
    }

    protected override void OnAmmoMoved(Vector3 startPoint, Vector3 endPoint)
    {
        if (_canAlertEnemies == false)
        {
            return;
        }

        AlertByPath(startPoint, endPoint);
    }

    protected override void OnAmmoTargetImpacted(Collider hitCollider)
    {
        TryAlertTarget(hitCollider);
    }

    private bool CanAlertEnemies()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemies");

        if (enemyLayer < 0)
        {
            return false;
        }

        return (Ammo.TargetLayers.value & (1 << enemyLayer)) != 0;
    }

    private void AlertByPath(Vector3 startPoint, Vector3 endPoint)
    {
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            startPoint,
            endPoint,
            _alertRadius,
            _alertBuffer,
            Ammo.TargetLayers,
            QueryTriggerInteraction.Ignore);
        int hitIndex = 0;

        while (hitIndex < hitCount)
        {
            Collider hitCollider = _alertBuffer[hitIndex];
            _alertBuffer[hitIndex] = null;

            TryAlertTarget(hitCollider);
            hitIndex += 1;
        }
    }

    private void TryAlertTarget(Collider hitCollider)
    {
        if (_canAlertEnemies == false)
        {
            return;
        }

        if (hitCollider == null)
        {
            return;
        }

        MonoBehaviour alertReceiver = GetAlertReceiver(hitCollider);

        if (alertReceiver == null)
        {
            return;
        }

        int alertId = alertReceiver.gameObject.GetInstanceID();

        if (ContainsAlertId(alertId))
        {
            return;
        }

        if (alertReceiver is IEnemyAlert enemyAlert)
        {
            enemyAlert.ApplyAlert(GetAlertPoint());
        }

        RememberAlertId(alertId);
    }

    private MonoBehaviour GetAlertReceiver(Collider hitCollider)
    {
        Turret turret = hitCollider.GetComponentInParent<Turret>();

        if (turret != null)
        {
            return turret;
        }

        EnemyMeleeBrain enemyMeleeBrain = hitCollider.GetComponentInParent<EnemyMeleeBrain>();

        if (enemyMeleeBrain != null)
        {
            return enemyMeleeBrain;
        }

        EnemyDroneBrain enemyDroneBrain = hitCollider.GetComponentInParent<EnemyDroneBrain>();

        if (enemyDroneBrain != null)
        {
            return enemyDroneBrain;
        }

        EnemyBomberBrain enemyBomberBrain = hitCollider.GetComponentInParent<EnemyBomberBrain>();

        if (enemyBomberBrain != null)
        {
            return enemyBomberBrain;
        }

        return null;
    }

    private bool ContainsAlertId(int alertId)
    {
        int alertIndex = 0;

        while (alertIndex < _alertIdCount)
        {
            if (_alertIds[alertIndex] == alertId)
            {
                return true;
            }

            alertIndex += 1;
        }

        return false;
    }

    private void RememberAlertId(int alertId)
    {
        if (_alertIdCount >= _alertIds.Length)
        {
            return;
        }

        _alertIds[_alertIdCount] = alertId;
        _alertIdCount += 1;
    }

    private Vector3 GetAlertPoint()
    {
        if (Ammo.IgnoredRoot != null)
        {
            return Ammo.IgnoredRoot.position;
        }

        return transform.position;
    }
}

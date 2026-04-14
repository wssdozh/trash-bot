using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomAlert : MonoBehaviour
{
    private const int PulsePoolSize = 12;
    private const int ReceiverPoolSize = 24;

    private readonly List<EnemyAlertPulse> _pulsePool = new List<EnemyAlertPulse>(PulsePoolSize);
    private readonly List<MonoBehaviour> _alertReceivers = new List<MonoBehaviour>(ReceiverPoolSize);

    private void Awake()
    {
        WarmupPool();
    }

    public void AlertPoint(Vector3 point, MonoBehaviour sender)
    {
        CollectReceivers(sender);
        int receiverIndex = 0;

        while (receiverIndex < _alertReceivers.Count)
        {
            TryAlertReceiver(point, sender, _alertReceivers[receiverIndex]);
            receiverIndex += 1;
        }

        _alertReceivers.Clear();
    }

    public void AlertPoint(Vector3 point, MonoBehaviour sender, int maxCount, System.Random random)
    {
        if (maxCount <= 0)
        {
            return;
        }

        if (random == null)
        {
            throw new InvalidOperationException(nameof(random));
        }

        CollectReceivers(sender);

        int alertCount = 0;
        int remainingCount = _alertReceivers.Count;

        while (remainingCount > 0)
        {
            if (alertCount >= maxCount)
            {
                break;
            }

            int pickIndex = random.Next(0, remainingCount);
            MonoBehaviour alertReceiver = _alertReceivers[pickIndex];
            remainingCount -= 1;
            _alertReceivers[pickIndex] = _alertReceivers[remainingCount];
            _alertReceivers[remainingCount] = alertReceiver;

            if (TryAlertReceiver(point, sender, alertReceiver))
            {
                alertCount += 1;
            }
        }

        _alertReceivers.Clear();
    }

    private void WarmupPool()
    {
        int pulseIndex = 0;

        while (pulseIndex < PulsePoolSize)
        {
            EnemyAlertPulse pulse = CreatePulse();
            pulse.gameObject.SetActive(false);
            _pulsePool.Add(pulse);
            pulseIndex += 1;
        }
    }

    private void PlayPulse(MonoBehaviour sender, MonoBehaviour receiver)
    {
        if (sender == null)
        {
            return;
        }

        if (receiver == null)
        {
            return;
        }

        EnemyAlertPulse pulse = GetPulse();

        if (pulse == null)
        {
            return;
        }

        pulse.Play(sender.transform.position, receiver.transform);
    }

    private void CollectReceivers(MonoBehaviour sender)
    {
        _alertReceivers.Clear();

        MonoBehaviour[] alertReceivers = GetComponentsInChildren<MonoBehaviour>(true);
        int receiverIndex = 0;

        while (receiverIndex < alertReceivers.Length)
        {
            MonoBehaviour alertReceiver = alertReceivers[receiverIndex];

            if (alertReceiver == null)
            {
                receiverIndex += 1;

                continue;
            }

            if (object.ReferenceEquals(alertReceiver, sender))
            {
                receiverIndex += 1;

                continue;
            }

            if (alertReceiver is Turret)
            {
                receiverIndex += 1;

                continue;
            }

            IEnemyAlert enemyAlert = alertReceiver as IEnemyAlert;

            if (enemyAlert != null)
            {
                _alertReceivers.Add(alertReceiver);
            }

            receiverIndex += 1;
        }
    }

    private bool TryAlertReceiver(Vector3 point, MonoBehaviour sender, MonoBehaviour receiver)
    {
        IEnemyAlert enemyAlert = receiver as IEnemyAlert;

        if (enemyAlert == null)
        {
            return false;
        }

        if (enemyAlert.ApplyAlert(point) == false)
        {
            return false;
        }

        PlayPulse(sender, receiver);

        return true;
    }

    private EnemyAlertPulse GetPulse()
    {
        int pulseIndex = 0;

        while (pulseIndex < _pulsePool.Count)
        {
            EnemyAlertPulse pulse = _pulsePool[pulseIndex];

            if (pulse.gameObject.activeSelf == false)
            {
                return pulse;
            }

            pulseIndex += 1;
        }

        return null;
    }

    private EnemyAlertPulse CreatePulse()
    {
        GameObject pulseObject = new GameObject("Alert Pulse");
        pulseObject.transform.SetParent(transform, false);
        EnemyAlertPulse pulse = pulseObject.AddComponent<EnemyAlertPulse>();
        pulse.Setup(18f, 0.16f, 0.22f, 0.1f, new Color(0.35f, 0.95f, 1f, 1f));

        return pulse;
    }
}

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomAlert : MonoBehaviour
{
    private const int PulsePoolSize = 12;

    private readonly List<EnemyAlertPulse> _pulsePool = new List<EnemyAlertPulse>(PulsePoolSize);

    private void Awake()
    {
        WarmupPool();
    }

    public void AlertPoint(Vector3 point, MonoBehaviour sender)
    {
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

            if (alertReceiver is IEnemyAlert enemyAlert)
            {
                if (enemyAlert.ApplyAlert(point))
                {
                    PlayPulse(sender, alertReceiver);
                }
            }

            receiverIndex += 1;
        }
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

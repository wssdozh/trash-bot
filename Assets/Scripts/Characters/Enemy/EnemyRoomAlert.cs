using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRoomAlert : MonoBehaviour
{
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

            if (ReferenceEquals(alertReceiver, sender))
            {
                receiverIndex += 1;

                continue;
            }

            if (alertReceiver is IEnemyAlert enemyAlert)
            {
                enemyAlert.ApplyAlert(point);
            }

            receiverIndex += 1;
        }
    }
}

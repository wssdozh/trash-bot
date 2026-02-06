using UnityEngine;

public sealed class PickupIdle : MonoBehaviour
{
    [Header("Зависимости")]
    [SerializeField] private PickupIdleBehaviour[] _idleBehaviours;

    private void OnEnable()
    {
        SetIdleActive(true);
    }

    private void OnDisable()
    {
        SetIdleActive(false);
    }

    public void SetIdleActive(bool isIdleActive)
    {
        if (_idleBehaviours == null || _idleBehaviours.Length == 0)
        {
            return;
        }


        for (int index = 0; index < _idleBehaviours.Length; index++)
        {
            PickupIdleBehaviour idleBehaviour = _idleBehaviours[index];

            if (idleBehaviour == null)
            {
                continue;
            }

            idleBehaviour.SetIdleActive(isIdleActive);
        }
    }
}

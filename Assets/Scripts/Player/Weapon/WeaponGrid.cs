using UnityEngine;

public class WeaponGrip : MonoBehaviour
{
    [SerializeField] private Vector3 _localPositionOffset;
    [SerializeField] private Vector3 _localRotationOffsetEuler;

    public Vector3 LocalPositionOffset
    {
        get { return _localPositionOffset; }
    }

    public Vector3 LocalRotationOffsetEuler
    {
        get { return _localRotationOffsetEuler; }
    }
}

using UnityEngine;

public struct FireShotContext
{
    public float TimeSeconds;

    public Vector3 Position;
    public Quaternion Rotation;

    public LayerMask TargetLayers;

    public IDamageCalculator DamageCalculator;
}

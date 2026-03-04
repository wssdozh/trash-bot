using UnityEngine;

public abstract class AttackShapeBase : ScriptableObject
{
    public abstract int GetTargets(Transform originTransform, float range, LayerMask hitLayers, Collider[] resultBuffer);
    public abstract void DrawGizmos(Transform originTransform, float range);
}

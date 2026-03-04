using UnityEngine;

[CreateAssetMenu(fileName = "SphereForwardAttackShape", menuName = "Combat/Attack Shapes/Sphere Forward")]
public class SphereForwardAttackShape : AttackShapeBase
{
    public override int GetTargets(Transform originTransform, float range, LayerMask hitLayers, Collider[] resultBuffer)
    {
        Vector3 center = originTransform.position + originTransform.forward * (range * 0.5f);

        return Physics.OverlapSphereNonAlloc(center, range, resultBuffer, hitLayers);
    }

    public override void DrawGizmos(Transform originTransform, float range)
    {
        Vector3 center = originTransform.position + originTransform.forward * (range * 0.5f);
        Gizmos.DrawWireSphere(center, range);
    }
}

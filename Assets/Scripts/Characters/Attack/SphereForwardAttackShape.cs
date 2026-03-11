using UnityEngine;

[CreateAssetMenu(fileName = "SphereForwardAttackShape", menuName = "Combat/Attack Shapes/Sphere Forward")]
public class SphereForwardAttackShape : AttackShapeBase
{
    [SerializeField] private float _hitRadiusFactor = 1f;
    [SerializeField] private float _forwardOffsetFactor = 0.5f;

    public override int GetTargets(Vector3 originPoint, Vector3 attackDirection, float range, LayerMask hitLayers, Collider[] resultBuffer)
    {
        Vector3 normalizedDirection = GetDirection(attackDirection);
        Vector3 center = originPoint + normalizedDirection * GetForwardOffset(range);

        return Physics.OverlapSphereNonAlloc(
            center,
            GetHitRadius(range),
            resultBuffer,
            hitLayers,
            QueryTriggerInteraction.Ignore);
    }

    public override void DrawGizmos(Vector3 originPoint, Vector3 attackDirection, float range)
    {
        Vector3 normalizedDirection = GetDirection(attackDirection);
        Vector3 center = originPoint + normalizedDirection * GetForwardOffset(range);
        Gizmos.DrawWireSphere(center, GetHitRadius(range));
    }

    private Vector3 GetDirection(Vector3 attackDirection)
    {
        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        attackDirection.Normalize();

        return attackDirection;
    }

    private float GetHitRadius(float range)
    {
        return Mathf.Max(range * _hitRadiusFactor, 0.01f);
    }

    private float GetForwardOffset(float range)
    {
        return Mathf.Max(range * _forwardOffsetFactor, 0f);
    }
}

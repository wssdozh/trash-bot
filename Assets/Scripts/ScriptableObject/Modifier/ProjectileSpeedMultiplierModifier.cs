using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSpeedMultiplierModifier", menuName = "Weapons/Modifiers/Projectile Speed Multiplier")]
public sealed class ProjectileSpeedMultiplierModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _multiplier = 1.3f;

    public float Multiplier => _multiplier;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.ProjectileSpeedMultiplier *= _multiplier;
    }
}

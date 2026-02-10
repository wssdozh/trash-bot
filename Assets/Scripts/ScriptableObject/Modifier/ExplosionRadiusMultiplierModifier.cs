using UnityEngine;

[CreateAssetMenu(fileName = "ExplosionRadiusMultiplierModifier", menuName = "Weapons/Modifiers/Explosion Radius Multiplier")]
public sealed class ExplosionRadiusMultiplierModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _multiplier = 1.25f;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.ExplosionRadiusMultiplier *= _multiplier;
    }
}

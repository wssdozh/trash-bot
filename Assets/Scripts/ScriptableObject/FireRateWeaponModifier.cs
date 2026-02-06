using UnityEngine;

[CreateAssetMenu(fileName = "FireRateWeaponModifier", menuName = "Weapons/Modifiers/FireRate")]
public sealed class FireRateWeaponModifier : WeaponModifier
{
    [SerializeField] private float _multiplier = 2f;

    public override void Apply(ref WeaponModifierContext context)
    {
        context.FireRateMultiplier *= _multiplier;
    }
}

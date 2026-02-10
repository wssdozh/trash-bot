using UnityEngine;

[CreateAssetMenu(fileName = "PelletBonusModifier", menuName = "Weapons/Modifiers/Pellet Bonus")]
public sealed class PelletBonusModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private int _bonus = 2;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.PelletBonus += _bonus;
    }
}

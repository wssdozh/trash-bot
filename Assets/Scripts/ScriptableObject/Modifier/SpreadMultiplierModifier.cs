using UnityEngine;

[CreateAssetMenu(fileName = "SpreadMultiplierModifier", menuName = "Weapons/Modifiers/Spread Multiplier")]
public sealed class SpreadMultiplierModifier : WeaponTypeFilteredModifier
{
    [SerializeField] private float _multiplier = 0.85f;

    public float Multiplier => _multiplier;

    protected override void ApplyAllowed(ref WeaponModifierContext context)
    {
        context.SpreadMultiplier *= _multiplier;
    }
}

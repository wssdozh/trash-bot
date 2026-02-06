using UnityEngine;

public abstract class WeaponModifier : ScriptableObject
{
    public abstract void Apply(ref WeaponModifierContext context);
}

using UnityEngine;

public abstract class PlayerModifier : ScriptableObject
{
    public abstract void Apply(ref PlayerModifierContext context);
}

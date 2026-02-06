public struct WeaponModifierContext
{
    public WeaponType WeaponType;

    public float FireRateMultiplier;
    public float DamageMultiplier;

    public static WeaponModifierContext CreateDefault()
    {
        WeaponModifierContext context = new WeaponModifierContext();

        context.WeaponType = WeaponType.None;

        context.FireRateMultiplier = 1f;
        context.DamageMultiplier = 1f;

        return context;
    }
}

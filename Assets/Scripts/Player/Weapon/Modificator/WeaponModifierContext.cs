public struct WeaponModifierContext
{
    public WeaponType WeaponType;

    public float FireRateMultiplier;
    public float DamageMultiplier;

    public float SpreadMultiplier;
    public int PelletBonus;

    public float ProjectileSpeedMultiplier;
    public float ExplosionRadiusMultiplier;

    public float CriticalChance01;
    public float CriticalDamageMultiplier;

    public static WeaponModifierContext CreateDefault()
    {
        WeaponModifierContext context = new WeaponModifierContext();

        context.WeaponType = WeaponType.None;

        context.FireRateMultiplier = 1f;
        context.DamageMultiplier = 1f;

        context.SpreadMultiplier = 1f;
        context.PelletBonus = 0;

        context.ProjectileSpeedMultiplier = 1f;
        context.ExplosionRadiusMultiplier = 1f;

        context.CriticalChance01 = 0f;
        context.CriticalDamageMultiplier = 2f;

        return context;
    }
}

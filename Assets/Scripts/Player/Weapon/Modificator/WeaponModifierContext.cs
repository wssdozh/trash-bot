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

    public void SetDefaults()
    {
        WeaponType = WeaponType.None;

        FireRateMultiplier = 1f;
        DamageMultiplier = 1f;

        SpreadMultiplier = 1f;
        PelletBonus = 0;

        ProjectileSpeedMultiplier = 1f;
        ExplosionRadiusMultiplier = 1f;

        CriticalChance01 = 0f;
        CriticalDamageMultiplier = 2f;
    }
}

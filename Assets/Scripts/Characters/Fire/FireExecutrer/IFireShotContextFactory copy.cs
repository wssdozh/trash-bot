public sealed class BaseFireRateCalculator : IFireRateCalculator
{
    public float GetEffectiveFireRatePerSecond(float baseFireRatePerSecond)
    {
        return baseFireRatePerSecond;
    }
}

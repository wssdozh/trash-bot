using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModifierOffer", menuName = "Shop/Modifier Offer")]
public sealed class ModifierOffer : ScriptableObject
{
    private const int CommonPrice = 25;
    private const int RarePrice = 55;
    private const int LegendaryPrice = 95;
    private const int ExtraModifierPrice = 12;

    private const int RarePercent = 18;
    private const int LegendaryPercent = 35;

    private const int RarePelletBonus = 2;
    private const int LegendaryPelletBonus = 3;

    private const int RareCritScore = 28;
    private const int LegendaryCritScore = 48;

    [SerializeField] private string _title;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Item _requiredItem;
    [SerializeField] private ModifierOfferRarity _rarity = ModifierOfferRarity.Rare;
    [SerializeField] private int _price;
    [SerializeField] private WeaponModifier[] _modifiers;

    public string Title => GetTitle();

    public Sprite Icon => _icon;

    public Item RequiredItem => _requiredItem;

    public ModifierOfferRarity Rarity => GetRarity();

    public string RarityText => GetRarityText(Rarity);

    public int Price => GetPrice();

    public string Description => GetDescription();

    public WeaponModifier[] Modifiers => _modifiers;

    public bool IsCompatible(Inventory inventory)
    {
        if (_requiredItem == null)
        {
            return true;
        }

        if (inventory == null)
        {
            return false;
        }

        IReadOnlyList<InventorySlot> slots = inventory.Slots;

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot.IsEmpty())
            {
                continue;
            }

            if (slot.Item == _requiredItem)
            {
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        _title = GetTitle();
        _rarity = GetRarity();
        _price = GetPrice();
    }

    private string GetTitle()
    {
        WeaponModifier modifier = GetPrimaryModifier();

        if (modifier == null)
        {
            return "Модификатор";
        }

        if (modifier is DamageMultiplierModifier || modifier is DamageMultiplierFilteredModifier)
        {
            return "Урон";
        }

        if (modifier is FireRateWeaponModifier || modifier is FireRateWeaponFilteredModifier)
        {
            return "Скорострельность";
        }

        if (modifier is CriticalHitModifier)
        {
            return "Крит";
        }

        if (modifier is SpreadMultiplierModifier)
        {
            return "Разброс";
        }

        if (modifier is ProjectileSpeedMultiplierModifier)
        {
            return "Скорость пули";
        }

        if (modifier is PelletBonusModifier)
        {
            return "Дробь";
        }

        if (modifier is ExplosionRadiusMultiplierModifier)
        {
            return "Взрыв";
        }

        return modifier.name;
    }

    private string GetDescription()
    {
        if (_modifiers == null)
        {
            return string.Empty;
        }

        if (GetModifierCount() == 1)
        {
            WeaponModifier primaryModifier = GetPrimaryModifier();

            if (primaryModifier == null)
            {
                return string.Empty;
            }

            return GetSingleModifierDescription(primaryModifier);
        }

        string description = string.Empty;

        for (int i = 0; i < _modifiers.Length; i++)
        {
            WeaponModifier modifier = _modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            if (description.Length > 0)
            {
                description += "\n";
            }

            description += GetModifierDescription(modifier);
        }

        return description;
    }

    private string GetSingleModifierDescription(WeaponModifier modifier)
    {
        DamageMultiplierModifier damageMultiplierModifier = modifier as DamageMultiplierModifier;

        if (damageMultiplierModifier != null)
        {
            return BuildValuePercentDescription(damageMultiplierModifier.Multiplier);
        }

        DamageMultiplierFilteredModifier damageMultiplierFilteredModifier = modifier as DamageMultiplierFilteredModifier;

        if (damageMultiplierFilteredModifier != null)
        {
            return BuildValuePercentDescription(damageMultiplierFilteredModifier.Multiplier);
        }

        FireRateWeaponModifier fireRateWeaponModifier = modifier as FireRateWeaponModifier;

        if (fireRateWeaponModifier != null)
        {
            return BuildValuePercentDescription(fireRateWeaponModifier.Multiplier);
        }

        FireRateWeaponFilteredModifier fireRateWeaponFilteredModifier = modifier as FireRateWeaponFilteredModifier;

        if (fireRateWeaponFilteredModifier != null)
        {
            return BuildValueAddPercentDescription(fireRateWeaponFilteredModifier.AddToMultiplier);
        }

        CriticalHitModifier criticalHitModifier = modifier as CriticalHitModifier;

        if (criticalHitModifier != null)
        {
            return BuildShortCriticalDescription(criticalHitModifier);
        }

        SpreadMultiplierModifier spreadMultiplierModifier = modifier as SpreadMultiplierModifier;

        if (spreadMultiplierModifier != null)
        {
            return BuildValueInversePercentDescription(spreadMultiplierModifier.Multiplier);
        }

        ProjectileSpeedMultiplierModifier projectileSpeedMultiplierModifier = modifier as ProjectileSpeedMultiplierModifier;

        if (projectileSpeedMultiplierModifier != null)
        {
            return BuildValuePercentDescription(projectileSpeedMultiplierModifier.Multiplier);
        }

        PelletBonusModifier pelletBonusModifier = modifier as PelletBonusModifier;

        if (pelletBonusModifier != null)
        {
            return BuildPelletDescription(pelletBonusModifier.Bonus);
        }

        ExplosionRadiusMultiplierModifier explosionRadiusMultiplierModifier = modifier as ExplosionRadiusMultiplierModifier;

        if (explosionRadiusMultiplierModifier != null)
        {
            return "Радиус " + BuildValuePercentDescription(explosionRadiusMultiplierModifier.Multiplier);
        }

        return modifier.name;
    }

    private ModifierOfferRarity GetRarity()
    {
        ModifierOfferRarity rarity = ModifierOfferRarity.Rare;
        int modifierCount = 0;

        if (_modifiers == null)
        {
            return rarity;
        }

        for (int i = 0; i < _modifiers.Length; i++)
        {
            WeaponModifier modifier = _modifiers[i];

            if (modifier == null)
            {
                continue;
            }

            modifierCount += 1;

            ModifierOfferRarity modifierRarity = GetModifierRarity(modifier);

            if ((int)modifierRarity > (int)rarity)
            {
                rarity = modifierRarity;
            }
        }

        if (modifierCount > 1)
        {
            rarity = PromoteRarity(rarity);
        }

        return rarity;
    }

    private int GetPrice()
    {
        ModifierOfferRarity rarity = GetRarity();
        int price = CommonPrice;

        if (rarity == ModifierOfferRarity.Epic)
        {
            price = RarePrice;
        }

        if (rarity == ModifierOfferRarity.Legendary)
        {
            price = LegendaryPrice;
        }

        int modifierCount = GetModifierCount();

        if (modifierCount <= 1)
        {
            return price;
        }

        return price + (modifierCount - 1) * ExtraModifierPrice;
    }

    private WeaponModifier GetPrimaryModifier()
    {
        if (_modifiers == null)
        {
            return null;
        }

        for (int i = 0; i < _modifiers.Length; i++)
        {
            WeaponModifier modifier = _modifiers[i];

            if (modifier != null)
            {
                return modifier;
            }
        }

        return null;
    }

    private string GetModifierDescription(WeaponModifier modifier)
    {
        DamageMultiplierModifier damageMultiplierModifier = modifier as DamageMultiplierModifier;

        if (damageMultiplierModifier != null)
        {
            return BuildPercentDescription("Урон", damageMultiplierModifier.Multiplier);
        }

        DamageMultiplierFilteredModifier damageMultiplierFilteredModifier = modifier as DamageMultiplierFilteredModifier;

        if (damageMultiplierFilteredModifier != null)
        {
            return BuildPercentDescription("Урон", damageMultiplierFilteredModifier.Multiplier);
        }

        FireRateWeaponModifier fireRateWeaponModifier = modifier as FireRateWeaponModifier;

        if (fireRateWeaponModifier != null)
        {
            return BuildPercentDescription("Скорострельность", fireRateWeaponModifier.Multiplier);
        }

        FireRateWeaponFilteredModifier fireRateWeaponFilteredModifier = modifier as FireRateWeaponFilteredModifier;

        if (fireRateWeaponFilteredModifier != null)
        {
            return BuildAddPercentDescription("Скорострельность", fireRateWeaponFilteredModifier.AddToMultiplier);
        }

        CriticalHitModifier criticalHitModifier = modifier as CriticalHitModifier;

        if (criticalHitModifier != null)
        {
            return BuildCriticalDescription(criticalHitModifier);
        }

        SpreadMultiplierModifier spreadMultiplierModifier = modifier as SpreadMultiplierModifier;

        if (spreadMultiplierModifier != null)
        {
            return BuildInversePercentDescription("Разброс", spreadMultiplierModifier.Multiplier);
        }

        ProjectileSpeedMultiplierModifier projectileSpeedMultiplierModifier = modifier as ProjectileSpeedMultiplierModifier;

        if (projectileSpeedMultiplierModifier != null)
        {
            return BuildPercentDescription("Скорость пули", projectileSpeedMultiplierModifier.Multiplier);
        }

        PelletBonusModifier pelletBonusModifier = modifier as PelletBonusModifier;

        if (pelletBonusModifier != null)
        {
            return BuildIntDescription("Дробь", pelletBonusModifier.Bonus);
        }

        ExplosionRadiusMultiplierModifier explosionRadiusMultiplierModifier = modifier as ExplosionRadiusMultiplierModifier;

        if (explosionRadiusMultiplierModifier != null)
        {
            return BuildPercentDescription("Радиус взрыва", explosionRadiusMultiplierModifier.Multiplier);
        }

        return modifier.name;
    }

    private ModifierOfferRarity GetModifierRarity(WeaponModifier modifier)
    {
        DamageMultiplierModifier damageMultiplierModifier = modifier as DamageMultiplierModifier;

        if (damageMultiplierModifier != null)
        {
            return GetPercentRarity(damageMultiplierModifier.Multiplier);
        }

        DamageMultiplierFilteredModifier damageMultiplierFilteredModifier = modifier as DamageMultiplierFilteredModifier;

        if (damageMultiplierFilteredModifier != null)
        {
            return GetPercentRarity(damageMultiplierFilteredModifier.Multiplier);
        }

        FireRateWeaponModifier fireRateWeaponModifier = modifier as FireRateWeaponModifier;

        if (fireRateWeaponModifier != null)
        {
            return GetPercentRarity(fireRateWeaponModifier.Multiplier);
        }

        FireRateWeaponFilteredModifier fireRateWeaponFilteredModifier = modifier as FireRateWeaponFilteredModifier;

        if (fireRateWeaponFilteredModifier != null)
        {
            return GetAddPercentRarity(fireRateWeaponFilteredModifier.AddToMultiplier);
        }

        CriticalHitModifier criticalHitModifier = modifier as CriticalHitModifier;

        if (criticalHitModifier != null)
        {
            return GetCriticalRarity(criticalHitModifier);
        }

        SpreadMultiplierModifier spreadMultiplierModifier = modifier as SpreadMultiplierModifier;

        if (spreadMultiplierModifier != null)
        {
            return GetInversePercentRarity(spreadMultiplierModifier.Multiplier);
        }

        ProjectileSpeedMultiplierModifier projectileSpeedMultiplierModifier = modifier as ProjectileSpeedMultiplierModifier;

        if (projectileSpeedMultiplierModifier != null)
        {
            return GetPercentRarity(projectileSpeedMultiplierModifier.Multiplier);
        }

        PelletBonusModifier pelletBonusModifier = modifier as PelletBonusModifier;

        if (pelletBonusModifier != null)
        {
            return GetPelletRarity(pelletBonusModifier.Bonus);
        }

        ExplosionRadiusMultiplierModifier explosionRadiusMultiplierModifier = modifier as ExplosionRadiusMultiplierModifier;

        if (explosionRadiusMultiplierModifier != null)
        {
            return GetPercentRarity(explosionRadiusMultiplierModifier.Multiplier);
        }

        return ModifierOfferRarity.Rare;
    }

    private static string GetRarityText(ModifierOfferRarity rarity)
    {
        if (rarity == ModifierOfferRarity.Epic)
        {
            return "Редкая";
        }

        if (rarity == ModifierOfferRarity.Legendary)
        {
            return "Легендарная";
        }

        return "Обычная";
    }

    private ModifierOfferRarity GetPercentRarity(float multiplier)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);

        return GetPercentTier(percent);
    }

    private ModifierOfferRarity GetAddPercentRarity(float addToMultiplier)
    {
        int percent = Mathf.RoundToInt(addToMultiplier * 100f);

        return GetPercentTier(percent);
    }

    private ModifierOfferRarity GetInversePercentRarity(float multiplier)
    {
        int percent = Mathf.RoundToInt((1f - multiplier) * 100f);

        return GetPercentTier(percent);
    }

    private ModifierOfferRarity GetPercentTier(int percent)
    {
        if (percent >= LegendaryPercent)
        {
            return ModifierOfferRarity.Legendary;
        }

        if (percent >= RarePercent)
        {
            return ModifierOfferRarity.Epic;
        }

        return ModifierOfferRarity.Rare;
    }

    private ModifierOfferRarity GetPelletRarity(int bonus)
    {
        if (bonus >= LegendaryPelletBonus)
        {
            return ModifierOfferRarity.Legendary;
        }

        if (bonus >= RarePelletBonus)
        {
            return ModifierOfferRarity.Epic;
        }

        return ModifierOfferRarity.Rare;
    }

    private ModifierOfferRarity GetCriticalRarity(CriticalHitModifier modifier)
    {
        int chancePercent = Mathf.RoundToInt(modifier.ChanceAdd01 * 100f);
        int critDamageScore = Mathf.RoundToInt((modifier.DamageMultiplier - 1f) * 25f);
        int critScore = chancePercent + critDamageScore;

        if (critScore >= LegendaryCritScore)
        {
            return ModifierOfferRarity.Legendary;
        }

        if (critScore >= RareCritScore)
        {
            return ModifierOfferRarity.Epic;
        }

        return ModifierOfferRarity.Rare;
    }

    private ModifierOfferRarity PromoteRarity(ModifierOfferRarity rarity)
    {
        if (rarity == ModifierOfferRarity.Rare)
        {
            return ModifierOfferRarity.Epic;
        }

        if (rarity == ModifierOfferRarity.Epic)
        {
            return ModifierOfferRarity.Legendary;
        }

        return ModifierOfferRarity.Legendary;
    }

    private int GetModifierCount()
    {
        if (_modifiers == null)
        {
            return 0;
        }

        int modifierCount = 0;

        for (int i = 0; i < _modifiers.Length; i++)
        {
            if (_modifiers[i] == null)
            {
                continue;
            }

            modifierCount += 1;
        }

        return modifierCount;
    }

    private static string BuildPercentDescription(string label, float multiplier)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);

        if (percent == 0)
        {
            return label;
        }

        if (percent > 0)
        {
            return label + " +" + percent + "%";
        }

        return label + " " + percent + "%";
    }

    private static string BuildAddPercentDescription(string label, float addToMultiplier)
    {
        int percent = Mathf.RoundToInt(addToMultiplier * 100f);

        if (percent == 0)
        {
            return label;
        }

        if (percent > 0)
        {
            return label + " +" + percent + "%";
        }

        return label + " " + percent + "%";
    }

    private static string BuildInversePercentDescription(string label, float multiplier)
    {
        int percent = Mathf.RoundToInt((1f - multiplier) * 100f);

        if (percent == 0)
        {
            return label;
        }

        if (percent > 0)
        {
            return label + " -" + percent + "%";
        }

        return label + " +" + (-percent) + "%";
    }

    private static string BuildIntDescription(string label, int value)
    {
        if (value == 0)
        {
            return label;
        }

        if (value > 0)
        {
            return label + " +" + value;
        }

        return label + " " + value;
    }

    private static string BuildCriticalDescription(CriticalHitModifier modifier)
    {
        int chancePercent = Mathf.RoundToInt(modifier.ChanceAdd01 * 100f);
        string description = "Шанс крита";

        if (chancePercent > 0)
        {
            description += " +" + chancePercent + "%";
        }
        else
        {
            if (chancePercent < 0)
            {
                description += " " + chancePercent + "%";
            }
        }

        if (modifier.DamageMultiplier > 1f)
        {
            description += ", x" + modifier.DamageMultiplier.ToString("0.##");
        }

        return description;
    }

    private static string BuildShortCriticalDescription(CriticalHitModifier modifier)
    {
        int chancePercent = Mathf.RoundToInt(modifier.ChanceAdd01 * 100f);
        string description = "Шанс";

        if (chancePercent > 0)
        {
            description += " +" + chancePercent + "%";
        }
        else
        {
            if (chancePercent < 0)
            {
                description += " " + chancePercent + "%";
            }
        }

        if (modifier.DamageMultiplier > 1f)
        {
            description += ", x" + modifier.DamageMultiplier.ToString("0.##");
        }

        return description;
    }

    private static string BuildValuePercentDescription(float multiplier)
    {
        int percent = Mathf.RoundToInt((multiplier - 1f) * 100f);

        if (percent == 0)
        {
            return "0%";
        }

        if (percent > 0)
        {
            return "+" + percent + "%";
        }

        return percent + "%";
    }

    private static string BuildValueAddPercentDescription(float addToMultiplier)
    {
        int percent = Mathf.RoundToInt(addToMultiplier * 100f);

        if (percent == 0)
        {
            return "0%";
        }

        if (percent > 0)
        {
            return "+" + percent + "%";
        }

        return percent + "%";
    }

    private static string BuildValueInversePercentDescription(float multiplier)
    {
        int percent = Mathf.RoundToInt((1f - multiplier) * 100f);

        if (percent == 0)
        {
            return "0%";
        }

        if (percent > 0)
        {
            return "-" + percent + "%";
        }

        return "+" + (-percent) + "%";
    }

    private static string BuildPelletDescription(int value)
    {
        if (value == 0)
        {
            return "0";
        }

        if (value > 0)
        {
            return "+" + value + " пули";
        }

        return value + " пули";
    }
}

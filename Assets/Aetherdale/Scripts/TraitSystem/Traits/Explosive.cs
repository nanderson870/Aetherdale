using UnityEngine;

public class Explosive : Trait
{
    const int PERCENT_RADIUS_INCREASE = 33;

    public override string GetName()
    {
        return "Explosive";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Your area of effect traits and abilities become +{PERCENT_RADIUS_INCREASE}% larger.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().explosiveSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public Explosive()
    {
        maxStacks = 3;
        
        AddStatChange(new(Stats.AOERadiusMultiplier, StatChangeType.Flat, GetRadiusIncrease() * 0.01F));
    }

    int GetRadiusIncrease()
    {
        return PERCENT_RADIUS_INCREASE * numberOfStacks;
    }
}
using UnityEngine;

/// <summary>
/// +lifesteal, -armor
/// </summary>
public class Bloodlust : Trait
{
    const int PERCENT_LIFESTEAL = 33;
    const int PERCENT_MAX_ARMOR_LOSS = 33;

    public override string GetName()
    {
        return "Bloodlust";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_LIFESTEAL * numberOfStacks}% lifesteal\n-{PERCENT_MAX_ARMOR_LOSS * numberOfStacks}% armor";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().bloodlustSprite;
    }


    public Bloodlust()
    {
        maxStacks = 3;

        AddStatChange(new(Stats.Lifesteal, StatChangeType.Flat, PERCENT_LIFESTEAL * numberOfStacks * 0.01F));
        AddStatChange(new(Stats.MaxArmor, StatChangeType.Multiplier, -PERCENT_MAX_ARMOR_LOSS * numberOfStacks * 0.01F));
    }
}

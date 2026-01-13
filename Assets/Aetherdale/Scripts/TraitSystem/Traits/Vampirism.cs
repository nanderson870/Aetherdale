using UnityEngine;

/// <summary>
/// Lifesteal Trait
/// </summary>
public class Vampirism : Trait
{
    const int PERCENT_LIFESTEAL = 5;

    public override string GetName()
    {
        return "Vampirism";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_LIFESTEAL}% lifesteal on hit.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().vampirismSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public Vampirism()
    {
        AddStatChange(new StatChange(Stats.Lifesteal, StatChangeType.Flat,  PERCENT_LIFESTEAL * 0.01F));
    }
}

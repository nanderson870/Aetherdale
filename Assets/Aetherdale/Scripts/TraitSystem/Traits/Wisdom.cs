using UnityEngine;

/// <summary>
/// Experience Gain
/// </summary>
public class Wisdom : Trait
{
    const int PERCENT_EXTRA_EXPERIENCE = 33;

    public override string GetName()
    {
        return "Wisdom";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Gain {PERCENT_EXTRA_EXPERIENCE}% more experience.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().wisdomSprite;
    }


    public Wisdom()
    {
        AddStatChange(new(Stats.ExperienceMultiplier, StatChangeType.Flat, PERCENT_EXTRA_EXPERIENCE * 0.01F));
    }
}

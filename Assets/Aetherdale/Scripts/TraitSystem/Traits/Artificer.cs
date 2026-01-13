using UnityEngine;

/// <summary>
/// Trinket Cooldown
/// </summary>
public class Artificer : Trait
{
    const int PERCENT_CDR = 25;
    const int MAX_STACKS = 3;

    public override string GetName()
    {
        return "Artificer";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"-{PERCENT_CDR}% to trinket cooldowns.";
    }

    public override bool PlayerMeetsRequirements(Player player)
    {
        return player.GetTrinket() != null;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().artificerSprite;
    }

    public Artificer()
    {
        maxStacks = MAX_STACKS;

        AddStatChange(new(Stats.TrinketCooldownMultiplier, StatChangeType.Flat, PERCENT_CDR * -0.01F));
    }
}
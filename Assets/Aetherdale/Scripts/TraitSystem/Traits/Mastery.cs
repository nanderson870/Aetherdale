using UnityEngine;

/// <summary>
/// Ability Cooldown Reduction Trait
/// </summary>
public class Mastery : Trait
{
    const int PERCENT_COOLDOWN_REDUCTION = 15;

    public const int MAX_STACKS = 4;

    public override string GetName()
    {
        return "Mastery";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"-{PERCENT_COOLDOWN_REDUCTION}% to ability cooldowns";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().masterySprite;
    }

    public Mastery()
    {
        maxStacks = MAX_STACKS;

        AddStatChange(new(Stats.AbilityCooldownRatio, StatChangeType.Multiplier, -PERCENT_COOLDOWN_REDUCTION * 0.01F));
    }
}

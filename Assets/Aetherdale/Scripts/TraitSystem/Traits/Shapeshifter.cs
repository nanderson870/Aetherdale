using UnityEngine;

/// <summary>
/// Idol Recharge Rate Trait
/// </summary>
public class Shapeshifter : Trait
{
    const int PERCENT_IDOL_CHARGE_RATE = 40;

    public override string GetName()
    {
        return "Shapeshifter";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Inactive form charges {PERCENT_IDOL_CHARGE_RATE}% faster.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().shapeshifterSprite;
    }

    public override bool PlayerMeetsRequirements(Player player)
    {
        return player.GetIdolForm() != null;
    }


    public Shapeshifter()
    {
        AddStatChange(new(Stats.InactiveFormHealthRegen, StatChangeType.Multiplier, PERCENT_IDOL_CHARGE_RATE * 0.01F));
    }
}

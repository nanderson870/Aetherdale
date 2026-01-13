using UnityEngine;

/// <summary>
/// Attack Damage Trait
/// </summary>
public class Ferocity : Trait
{
    const int PERCENT_ATTACK_DAMAGE = 15;
    public override string GetName()
    {
        return "Ferocity";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_ATTACK_DAMAGE}% attack damage.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().ferocitySprite;
    }


    public Ferocity()
    {
        AddStatChange(new(Stats.AttackDamageMultiplier, StatChangeType.Multiplier, PERCENT_ATTACK_DAMAGE * 0.01F));
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stacking attack damage and movespeed on kill
/// </summary>
public class Rampage : Trait
{
    const int MAX_STACKS=10;
    const int RAMPAGE_STACK_DURATION = 5;
    const int PERCENT_ATTACK_DAMAGE_PER_STACK = 5;
    const int PERCENT_ATTACK_SPEED_PER_STACK = 5;
    const int PERCENT_MOVE_SPEED_PER_STACK = 5;

    readonly Effect rampageEffect;
    

    public override string GetName()
    {
        return "Rampage";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Upon killing an enemy, gain a stack of Rampage for {RAMPAGE_STACK_DURATION} seconds, up to a maximum of {MAX_STACKS}. Each stack grants +{PERCENT_ATTACK_DAMAGE_PER_STACK}% attack damage, +{PERCENT_ATTACK_SPEED_PER_STACK}% attack speed, and +{PERCENT_MOVE_SPEED_PER_STACK}% movement speed.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().rampageSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public Rampage()
    {
        rampageEffect = EffectLibrary.GetEffectLibrary().rampageEffect;
    }

    public override void OnKill(HitInfo hitResult)
    {
        hitResult.damageDealer.AddEffect(rampageEffect, hitResult.damageDealer);
    }
}
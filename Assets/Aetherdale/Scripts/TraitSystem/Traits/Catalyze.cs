

using UnityEngine;

public class Catalyze : Trait
{
    const int DAMAGE = 30;

    const float HIT_DELAY = 0.25F;

    public override string GetName()
    {
        return "Catalyze";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().catalyzeSprite;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Enemies explode for {DAMAGE} fire damage on death.";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    
    public override void OnKill(HitInfo hitResult)
    {
        AreaOfEffect.AOEProperties explosion = AreaOfEffect.Create(AetherdaleData.GetAetherdaleData().catalyzeAOE, hitResult.hitPosition, hitResult.damageDealer, HitType.Ability);
        explosion.hitDelay = HIT_DELAY;
        explosion.damage = GetDamage();
    }

    int GetDamage()
    {
        return DAMAGE * numberOfStacks;
    }
}
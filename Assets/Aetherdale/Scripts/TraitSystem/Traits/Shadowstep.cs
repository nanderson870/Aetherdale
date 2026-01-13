using UnityEngine;

/// <summary>
/// Invisibility after dodge
/// </summary>
public class Shadowstep : Trait
{
    const float COOLDOWN = 5;
    const float INVISIBILITY_DURATION = 2.0F;

    float lastUse = 0;

    public override string GetName()
    {
        return "Shadowstep";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Turn invisible for {INVISIBILITY_DURATION}s upon dodging. ({COOLDOWN}s cooldown)";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().shadowstepSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }


    public Shadowstep()
    {
        maxStacks = 1;
    }

    public override void OnDodgeStart(Entity entity)
    {
        if (Time.time - lastUse >= COOLDOWN)
        {
            entity.AddEffect(EffectLibrary.GetEffectLibrary().shadowstepInvisibilityEffect, entity);
            lastUse = Time.time;
        }
    }
}

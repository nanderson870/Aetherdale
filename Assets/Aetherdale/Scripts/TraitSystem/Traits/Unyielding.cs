using UnityEngine;

/// <summary>
/// Experience Gain
/// </summary>
public class Unyielding : Trait
{
    const int TRIGGER_THRESHOLD = 30;
    const int MAX_HEALTH_REGENERATED=50;
    const int DURATION=5;
    const int COOLDOWN=180;

    float lastTrigger = -900;

    public override string GetName()
    {
        return "Unyielding";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"When below {TRIGGER_THRESHOLD}% health, regenerate {MAX_HEALTH_REGENERATED}% of your maximum health over {DURATION}s. ({COOLDOWN}s cooldown).";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().unyieldingSprite;
    }


    public Unyielding()
    {
    }

    public override void OnDamaged(HitInfo damagingHitInfo)
    {
        if (damagingHitInfo.entityHit.GetHealthRatio() <= (TRIGGER_THRESHOLD * 0.01F)
            && Time.time - lastTrigger >= COOLDOWN)
        {
            TriggerUnyielding(damagingHitInfo.entityHit);
        }
    }

    void TriggerUnyielding(Entity origin)
    {
        lastTrigger = Time.time;

        EffectInstance instance = origin.AddEffect(AetherdaleData.GetAetherdaleData().unyieldingTraitEffect, origin);
        instance.SetMagnitude(MAX_HEALTH_REGENERATED); // Use magnitude to scale, set effect up to compensate for this
    }
}

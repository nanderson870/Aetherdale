using UnityEngine;

/// <summary>
/// Additional health orbs trait
/// </summary>
public class Harvesting : Trait
{
    const int ADDITIONAL_ORBS = 2;

    public override string GetName()
    {
        return "Harvesting";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{ADDITIONAL_ORBS} additional health orbs per kill.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().harvestingSprite;
    }

    public override void OnKill(HitInfo hitResult)
    {
        for (int i = 0; i < ADDITIONAL_ORBS; i++)
        {
            hitResult.entityHit.DropHealthOrb();
        }
    }
}

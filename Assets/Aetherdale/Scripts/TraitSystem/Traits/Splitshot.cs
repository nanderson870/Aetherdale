using Mirror;
using UnityEngine;

/// <summary>
/// Multi-shot projectiles trait
/// </summary>
public class Splitshot : Trait
{
    const int PERCENT_CHANCE_PER_STACK = 50;

    const float ANGLE_RANDOMIZATION = 0.05F;

    public override string GetName()
    {
        return "Splitshot";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Uncommon;
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"{PERCENT_CHANCE_PER_STACK}% chance to duplicate any projectile created.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().splitshotSprite;
    }


    int GetDuplicationChance()
    {
        return PERCENT_CHANCE_PER_STACK * numberOfStacks;
    }

    public Splitshot()
    {
    }

    public override void OnProjectileCreated(Projectile projctilePrefab, Projectile projectileInstance)
    {
        if (projectileInstance == null)
        {
            return;
        }
        
        int total = GetDuplicationChance();
        int remainderChance = total % 100;
        int guaranteedDuplicates = total / 100;

        int actualNumber = guaranteedDuplicates;
        if (Random.Range(0, 100) < remainderChance)
        {
            actualNumber++;
        }

        for (int i = 0; i < actualNumber; i++)
        {
            Projectile duplicate = Projectile.Create(projctilePrefab, projectileInstance.transform.position, projectileInstance.transform.rotation, projectileInstance.progenitor, projectileInstance.velocity, triggerCreationEvent: false);
            duplicate.SetInaccuracy(ANGLE_RANDOMIZATION);
        }
    }
}

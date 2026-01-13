using UnityEngine;

/// <summary>
/// Periodic seeking projectiles
/// </summary>
public class SeekingSpirits : Trait
{
    const int BASE_DAMAGE = 25;
    const int MAX_STACKS = 3;
    const float BASE_INTERVAL = 4;
    const float MINUS_INTERVAL_PER_STACK = 1.25F;

    const float RANGE = 35.0F;
    const float SPEED = 50.0F;
    const float ACCELERATION = 20F;

    float lastFired = 0;

    public override string GetName()
    {
        return "Seeking Spirits";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Unleash a seeking projectile that deals {BASE_DAMAGE} Light damage every {GetInterval()} seconds.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().seekingSpiritsSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }


    public SeekingSpirits()
    {
        maxStacks = MAX_STACKS;
    }

    public override void OnProcessTraits(Player player)
    {
        base.OnProcessTraits(player);

        if (Time.time - lastFired >= GetInterval())
        {
            ControlledEntity playerEntity = player.GetControlledEntity();
            Entity nearestEnemy = playerEntity.GetPreferredEnemy(RANGE);

            if (nearestEnemy != null)
            {
                EmitSpirit(playerEntity, nearestEnemy);
            }
        }
    }

    public float GetInterval()
    {
        return BASE_INTERVAL - (numberOfStacks * MINUS_INTERVAL_PER_STACK);
    }

    void EmitSpirit(Entity emitter, Entity target)
    {
        GuaranteedSeekingProjectile projectile = AetherdaleData.GetAetherdaleData().seekingSpiritsProjectile;

        GuaranteedSeekingProjectile inst = Projectile.Create(projectile, emitter.GetWorldPosCenter(), emitter.transform.rotation, emitter.gameObject,  emitter.transform.forward * SPEED);
        inst.SetTarget(target.gameObject);
        inst.SetMovementProperties(0, SPEED, ACCELERATION);
        
        inst.OnExplosionCreated += ConfigureExplosion;

        lastFired = Time.time;
    }

    void ConfigureExplosion(AreaOfEffect.AOEProperties props)
    {
        props.damage = BASE_DAMAGE;
    }
}

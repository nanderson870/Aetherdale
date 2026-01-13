using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class FireElite : ElementalElite
{

    const float BURN_APPLY_INTERVAL = 5F;
    const float BURN_RANGE = 10.0F;

    EventInstance idleInstance;

    public override string GetElitePrefix()
    {
        return "Burning";
    }

    public override Element GetElement()
    {
        return Element.Fire;
    }

    public override void Start()
    {
        base.Start();

        InvokeRepeating(nameof(Burn), 1, BURN_APPLY_INTERVAL);

        idleInstance = RuntimeManager.CreateInstance(AetherdaleData.GetAetherdaleData().soundData.fireEliteIdleSound);
        RuntimeManager.AttachInstanceToGameObject(idleInstance, transform);

        idleInstance.start();
    }

    void Burn()
    {
        foreach (Entity enemy in entity.GetNearbyEnemies(BURN_RANGE, true))
        {
            if (entity.isServer)
            {
                enemy.AddEffect(AetherdaleData.GetAetherdaleData().fireEliteBurnEffect, entity);
            }
        }
    }

    public override void OnEntityDeath(Entity entity, Entity killer)
    {
        base.OnEntityDeath(entity, killer);

        //AreaOfEffect.Create(AetherdaleData.GetAetherdaleData().fireEliteDeathAOE, entity.GetWorldPosCenter(), this.entity, HitType.Ability, impact: 60).DelayHit(0.33F);
    }

    void OnDestroy()
    {
        
        idleInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }
}
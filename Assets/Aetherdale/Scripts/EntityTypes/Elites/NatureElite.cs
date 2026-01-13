using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NatureElite : ElementalElite 
{
    public const float HEALING_BURST_INTERVAL = 8.0F;
    public const float HEALING_BURST_RANGE = 30.0F;

    float lastHealingBurst = 0;

    Effect regenEffect;

    public override string GetElitePrefix()
    {
        return "Earthen";
    }

    public override Element GetElement()
    {
        return Element.Nature;
    }

    public override void Start()
    {
        base.Start();

        regenEffect = AetherdaleData.GetAetherdaleData().natureEliteRegenEffect;
    }

    // TODO effect - occasionally heal nearby allies
    public void Update()
    {
        if (NetworkServer.active)
        {
            if (Time.time - lastHealingBurst > HEALING_BURST_INTERVAL)
            {
                HealingBurst();
            } 
        }
    }

    void HealingBurst()
    {
        List<Entity> nearbyAllies = entity.GetNearbyAllies(HEALING_BURST_RANGE);

        foreach (Entity ally in nearbyAllies)
        {
            EffectInstance instance = ally.AddEffect(regenEffect, entity);
            instance.SetMagnitude(Equation.ENTITY_HEALTH_SCALING.Calculate(entity.GetLevel()));
        }
        lastHealingBurst = Time.time;
    }
}
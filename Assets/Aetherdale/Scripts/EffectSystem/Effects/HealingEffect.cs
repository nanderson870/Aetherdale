using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingEffect : ProcEffect
{
    [SerializeField] int healingDone;
    [SerializeField] StatValueCalculationMode valueCalculationMode;

    
    public StatValueCalculationMode GetValueCalculationMode() { return valueCalculationMode; }
    public int GetHealingDone() { return healingDone; }

    public override void Proc(EffectInstance instance, Entity target, Entity origin)
    {
        if (target.isServer)
        {
            int actualStatChange = healingDone;
            if (valueCalculationMode == StatValueCalculationMode.PercentageOfMaximum)
            {
                float healthPortionHealed = healingDone / 100.0F;
                actualStatChange = (int) (target.GetMaxHealth() * healthPortionHealed);
            }

            target.Heal(actualStatChange, origin);
        }
    }
}
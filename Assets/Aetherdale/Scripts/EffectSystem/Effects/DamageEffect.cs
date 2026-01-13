using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageEffect : ProcEffect
{
    [SerializeField] bool damageScalesWithStacks = true;
    [SerializeField] int damage = 0;
    [SerializeField] Element damageType;
    
    public Element GetDamageType() { return damageType; }

    public override void Proc(EffectInstance instance, Entity target, Entity origin)
    {
        float totalDamage = damage * instance.magnitude;
        if (damageScalesWithStacks)
        {
            totalDamage *= instance.GetNumberOfStacks();
        }

        target.Damage((int) totalDamage, damageType, HitType.Effect, origin, scaleTick:false);
    }
}
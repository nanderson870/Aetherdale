using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolarizedEffect : Effect
{
    [SerializeField] float stunChance = 10.0F;
    [SerializeField] float stunDuration = 1.5F;


    public override void OnEffectTargetDamaged(EffectInstance instance, Entity target, HitInfo damageHitResult)
    {
        base.OnEffectTargetDamaged(instance, target, damageHitResult);

        float roll = Random.Range(0, 100);

        //Debug.Log("Polarized - " + roll + " vs " + stunChance * instance.stacks);
        if (roll < stunChance * instance.GetNumberOfStacks())
        {
            target.Stun(stunDuration);
            target.RemoveEffect(this);
        }
    }
}
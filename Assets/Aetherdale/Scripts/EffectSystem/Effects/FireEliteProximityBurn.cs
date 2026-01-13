

using UnityEngine;

public class FireEliteProximityBurn : DamageEffect
{
    public override void OnEffectStart(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectStart(instance, target, origin);

        if (target.isOwned)
        {
            AetherdalePostProcessing.AddBurningEffectStrength(1F);
        }
    }

    public override void OnEffectEnd(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectEnd(instance, target, origin);

        if (target.isOwned)
        {
            AetherdalePostProcessing.AddBurningEffectStrength(-1F);
        }
    }
}
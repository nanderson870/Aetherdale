using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunEffect : Effect
{
    public override void OnEffectStart(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectStart(instance, target, origin);

        target.Stun(GetDuration());
    }
}
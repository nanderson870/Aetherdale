using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvisibilityEffect : Effect
{
    [SerializeField] Material invisibleMaterial;

    public override void OnEffectStart(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectStart(instance, target, origin);

        //target.SetMaterial(invisibleMaterial);
        target.RpcSetMaterial(invisibleMaterial.name, Entity.MaterialChangeProperties.None);

        target.SetInvisible(true);
    }

    public override void OnEffectEnd(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectEnd(instance, target, origin);

        //target.ResetMaterials();
        target.RpcResetMaterials();

        target.SetInvisible(false);
    }

}
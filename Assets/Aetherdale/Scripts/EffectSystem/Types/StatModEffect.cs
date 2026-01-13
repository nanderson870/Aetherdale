
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stat Mod Effect", menuName = "Aetherdale/Effects/Stat Mod Effect", order = 0)]
public class StatModEffect : Effect
{
    [SerializeField] StatChange[] statChanges;

    public StatChange[] GetStatChanges()
    {
        return statChanges;
    }

    public override void OnEffectStart(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectStart(instance, target, origin);

        for (int i = 0; i < instance.GetNumberOfStacks(); i++)
        {
            foreach (StatChange statChange in statChanges)
            {
                target.AddPostTraitStatChange(statChange);
            }
        }
    }

    public override void OnEffectEnd(EffectInstance instance, Entity target, Entity origin)
    {
        base.OnEffectEnd(instance, target, origin);

        for (int i = 0; i < instance.GetNumberOfStacks(); i++)
        {
            foreach (StatChange statChange in statChanges)
            {
                target.RemovePostTraitStatChange(statChange);
            }
        }
    }
}
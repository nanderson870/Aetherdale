
using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "New Resurrection Effect", menuName = "Aetherdale/Effects/Resurrection Effect", order = 0)]
public class ResurrectionEffect : Effect
{
    [SerializeField] EventReference resurrectionSound;

    public override void OnEffectTargetDied(EffectInstance effectInstance)
    {
        base.OnEffectTargetDied(effectInstance);

        if (effectInstance.target.GetOwningPlayer() is Player player && player != null)
        {
            player.ResurrectInSeconds(3.0F);
        }

        effectInstance.RemoveStack();
    }
}
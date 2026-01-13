
using UnityEngine;

public class DarkElite : ElementalElite
{
    public float lifestealRatio=1.5F;
    public override string GetElitePrefix()
    {
        return "Elusive";
    }

    public override Element GetElement()
    {
        return Element.Dark;
    }

    // TODO effect - lifesteal + lifesteal VFX
    public override void OnHitEntity(HitInfo hitResult)
    {
        base.OnHitEntity(hitResult);

        entity.Heal(hitResult.damageDealt * lifestealRatio, entity);
    }
}


/// <summary>
/// Damage based on missing health
/// </summary>
public class DarkDamageOverTimeEffect : ProcEffect
{
    int minDamage = 1; //at 100% health
    int maxDamage = 5; //at 0% health

    int GetDamage(Entity entity)
    {
        int extraDamagePotential = maxDamage - minDamage;

        float missingHealthRatio = 1 - entity.GetHealthRatio();

        int damage = (int) (minDamage +  (missingHealthRatio * extraDamagePotential));
        if (damage < 1) damage = 1;
        
        return damage;
    }

    public override void Proc(EffectInstance instance, Entity target, Entity origin)
    {
        base.Proc(instance, target, origin);

        target.Damage(GetDamage(target), Element.Dark, HitType.Effect, origin);
    }
}
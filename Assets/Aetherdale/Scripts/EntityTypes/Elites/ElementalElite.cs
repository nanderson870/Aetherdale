

using UnityEngine;
using UnityEngine.VFX;

public abstract class ElementalElite : Elite
{
    const float DAMAGE_REDUCTION_SAME_ELEMENT = 0.6F; // Same exact element gets reduced by this multiplier
    const float DAMAGE_REDUCTION_SAME_ELEMENT_TYPE = 0.4F; // Physical is one type, all elements are another type. On match, reduced by this mult
    const float DAMAGE_REDUCTION_DEFAULT = 0.3F; // If no other damage reduction applies, elites still reduce damage by this multiplier

    public abstract Element GetElement();

    public override Color GetPrimaryColor()
    {
        return ColorPalette.GetPrimaryColorForElement(GetElement());
    }

    public override Color GetSecondaryColor()
    {
        return ColorPalette.GetSecondaryColorForElement(GetElement());
    }

    public override float ModifyDamageWithEliteResistances(float originalDamage, Element damageElement)
    {
        if (damageElement == GetElement())
        {
            return (1.0F - DAMAGE_REDUCTION_SAME_ELEMENT) * originalDamage;
        }
        else if ((damageElement == Element.Physical) && (GetElement() == Element.Physical)
            || (damageElement != Element.Physical) && (GetElement() != Element.Physical))
        {
            return (1.0F - DAMAGE_REDUCTION_SAME_ELEMENT_TYPE) * originalDamage;
        }
        else
        {
            return (1.0F - DAMAGE_REDUCTION_DEFAULT) * originalDamage;
        }
    }

    public override GameObject GetAddons()
    {
        return VisualEffectIndex.GetDefaultEffectIndex().GetElementalEliteVisualEffect(GetElement());
    }

    public override void OnHitEntity(HitInfo hitResult)
    {
        base.OnHitEntity(hitResult);

        Effect elementalEffect = EffectLibrary.GetElementStatusEffect(GetElement());

        if (elementalEffect != null && hitResult.hitType != HitType.Effect)
        {
            hitResult.entityHit.AddEffect(elementalEffect, entity);
        }
    }
}
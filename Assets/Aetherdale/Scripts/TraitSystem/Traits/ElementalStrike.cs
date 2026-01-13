
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strike Trait - additional elemental damage on hit
/// </summary>
public class ElementalStrike : Trait
{
    Element element;

    public const int PERCENT_ADDITIONAL_DAMAGE = 10;

    public override string GetName()
    {
        return element.ToString() + " Strike";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"+{PERCENT_ADDITIONAL_DAMAGE}% {element.ToString()} damage on hit. Only one Strike trait may be carried at once.";
    }
    
    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().elementalStrikeSprite;
    }

    public ElementalStrike()
    {
        element = (Element) UnityEngine.Random.Range((int) Element.Fire, (int) Element.Dark + 1);
    }

    public ElementalStrike(Element element)
    {
        this.element = element;
    }

    public override void OnTraitAcquired(Player player, TraitList receivingList)
    {
        List<Trait> traits = receivingList.ToList();
        for (int i = traits.Count - 1; i >= 0; i--)
        {
            if (traits[i] is ElementalStrike strikeTrait && strikeTrait != this)
            {
                receivingList.RemoveTrait(strikeTrait);
                numberOfStacks++;
            }
        }
    }

    public override void OnHit(HitInfo hitResult)
    {
        int elementalDamage = (int) (hitResult.damageDealt * PERCENT_ADDITIONAL_DAMAGE * 0.01F * numberOfStacks);
        if (elementalDamage < numberOfStacks)
        {
            elementalDamage = numberOfStacks;
        }

        hitResult.entityHit.Damage(elementalDamage, element, HitType.Effect, hitResult.damageDealer);
    }
}

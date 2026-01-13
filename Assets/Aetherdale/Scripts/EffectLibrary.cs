using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "Effect Library", menuName = "Aetherdale/Libraries/Effect Library", order = 0)]
public class EffectLibrary : ScriptableObject
{
    [SerializeField] public Effect rampageEffect;
    public Effect shadowstepInvisibilityEffect;

    [SerializeField] Effect fireStatusEffect;
    [SerializeField] Effect natureStatusEffect;
    [SerializeField] Effect waterStatusEffect;
    [SerializeField] Effect stormStatusEffect;
    [SerializeField] Effect lightStatusEffect;
    [SerializeField] Effect darkStatusEffect;

    
    public static EffectLibrary GetEffectLibrary()
    {
        return Resources.Load<EffectLibrary>("Effect Library");
    }

    public static Effect GetElementStatusEffect(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                return GetEffectLibrary().fireStatusEffect;

            case Element.Water:
                return GetEffectLibrary().waterStatusEffect;

            case Element.Nature:
                return GetEffectLibrary().natureStatusEffect;
                
            case Element.Storm:
                return GetEffectLibrary().stormStatusEffect;
                
            case Element.Light:
                return GetEffectLibrary().lightStatusEffect;
                
            case Element.Dark:
                return GetEffectLibrary().darkStatusEffect;
                

            default:
                return null;
        }
    }
}

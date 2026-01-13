using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[CreateAssetMenu(fileName = "Visual Effect Library", menuName = "Aetherdale/Libraries/Visual Effect Library", order = 0)]
public class VisualEffectIndex : ScriptableObject
{
    public ParticleSystem stunnedParticles;

    public VisualEffect markOfAnnihilationParticles;
    public GameObject voltageHitSplat;

    public GameObject spiralProcVFX;

    public GameObject fireEliteVFX;
    public GameObject natureEliteVFX;
    public GameObject waterEliteVFX;
    public GameObject stormEliteVFX;
    public GameObject lightEliteVFX;
    public GameObject darkEliteVFX;

    
    public static VisualEffectIndex GetDefaultEffectIndex()
    {
        return Resources.Load<VisualEffectIndex>("Visual Effects");
    }


    public GameObject GetElementalEliteVisualEffect(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                return fireEliteVFX;
                
            case Element.Water:
                return waterEliteVFX;

            case Element.Nature:
                return natureEliteVFX;

            case Element.Storm:
                return stormEliteVFX;

            case Element.Light:
                return lightEliteVFX;

            case Element.Dark:
                return darkEliteVFX;

            default:
                return null;
        }
    }
}

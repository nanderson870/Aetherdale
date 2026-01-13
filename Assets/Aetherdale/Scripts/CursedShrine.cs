using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CursedShrine : NetworkBehaviour
{
    public TraitOffering traitOfferingPrefab;
    public Transform[] traitOfferingTransforms;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isServer)
        {
            List<Trait> traits = Trait.GetRandomUniqueCursedTraits(traitOfferingTransforms.Length);
            SetTraits(traits);
        }
    }
    
    void SetTraits(List<Trait> traits)
    {
        foreach(Transform transform in traitOfferingTransforms)
        {
            Trait trait = traits[0];
            traits.Remove(trait);
            TraitOffering offeringInstance = Instantiate(traitOfferingPrefab, transform);
            offeringInstance.OnTaken += DestroyAll;

            offeringInstance.SetTrait(trait);

            NetworkServer.Spawn(offeringInstance.gameObject);
        }
    }

    void DestroyAll(Player player)
    {
        if (isServer)
        {
            foreach (TraitOffering traitOffering in GetComponentsInChildren<TraitOffering>())
            {
                NetworkServer.UnSpawn(traitOffering.gameObject);
                Destroy(traitOffering.gameObject);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class TraitList
{
    Trait[] traits = new Trait[0];

    [NonSerialized] public Action<Trait[]> OnModified;

    public void AddTrait(Player player, Trait trait)
    {
        List<Trait> traitsAsList = traits.ToList();
        try
        {
            Trait existingTrait = traitsAsList.Find(matchedTrait => trait.GetName() == matchedTrait.GetName());
            existingTrait.AddStack();
        }
        catch
        {
            // Trait not possessed, add it new
            traitsAsList.Add(trait);
        }
        traits = traitsAsList.ToArray();

        trait.OnTraitAcquired(player, this);

        OnModified?.Invoke(traits);
    }

    public void RemoveTrait(Trait trait)
    {
        List<Trait> traitsAsList = traits.ToList();
        traitsAsList.Remove(trait);
        traits = traitsAsList.ToArray();

        OnModified?.Invoke(traits);
    }

    public void SetTraits(Trait[] traits)
    {
        this.traits = traits;

        OnModified?.Invoke(traits);
    }

    public List<Trait> ToList()
    {
        List<Trait> traitsAsList = traits.ToList();
        return traitsAsList;
    }

    public List<Trait> ToProcOrderList()
    {
        List<Trait> traitsAsList = traits.ToList();
        
        traitsAsList.Sort(delegate (Trait trait1, Trait trait2)
        { 
            int procOrder1 = trait1.GetProcOrder();
            int procOrder2 = trait2.GetProcOrder();

            return procOrder1.CompareTo(procOrder2);
        });

        return traitsAsList;
    }

    public List<StatChange> GetStatChanges()
    {
        List<StatChange> aggregate = new();
        foreach (Trait trait in traits)
        {
            foreach (StatChange statChange in trait.GetStatChanges())
            {
                aggregate.Add(statChange);
            }
        }

        return aggregate;
    }

    public List<Action<HitInfo>> GetOnHitActions()
    {
        List<Action<HitInfo>> ret = new();

        foreach (Trait trait in traits)
        {
            ret.Add(trait.OnHit);
        }

        return ret;
    }

    public List<Action<HitInfo>> GetOnKillActions()
    {
        List<Action<HitInfo>> ret = new();

        foreach (Trait trait in traits)
        {
            ret.Add(trait.OnKill);
        }

        return ret;
    }

    public void PrintTraits()
    {
        foreach(Trait trait in traits)
        {
            Debug.Log("| " + trait.GetName() + "(" + trait.GetNumberOfStacks() + " stacks)");
        }
    }

    public IEnumerator<Trait> GetEnumerator()
    {
        foreach(Trait trait in traits)
        {
            yield return trait;
        }
    }

    public int StacksOf(Trait trait)
    {
        foreach (Trait ownedTrait in this)
        {
            if (ownedTrait.GetName() == trait.GetName())
            {
                return trait.GetNumberOfStacks();
            }
        }

        return 0;
    }
}

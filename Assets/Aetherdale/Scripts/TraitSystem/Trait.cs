using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Unity.Mathematics;
using UnityEngine;

/* A temporary upgrade to the Wraith */

public abstract class Trait : object, IShopOffering
{
    public static readonly LinearEquation SHOP_COST = new (15.0F, 50);
    public const float COMMON_SHOP_COST_MULT = 1.0F;
    public const float UNCOMMON_SHOP_COST_MULT = 1.5F;
    public const float RARE_SHOP_COST_MULT = 2.5F;
    public const float EPIC_SHOP_COST_MULT = 3.0F;
    public const float LEGENDARY_SHOP_COST_MULT = 3.0F;
    public const float CURSED_SHOP_COST_MULT = 1.5F;

    
    const float UNCOMMON_TRAIT_CHANCE = 10;
    const float RARE_TRAIT_CHANCE = 3;
    const float EPIC_TRAIT_CHANCE = 0.5F;
    const float LEGENDARY_TRAIT_CHANCE = 0.01F;
    const float CURSED_TRAIT_CHANCE=0; // disabled by default


    [SerializeField] protected int numberOfStacks = 1;
    [SerializeField] protected int maxStacks = -1;

    [SerializeField] protected StatChange[] statChanges = new StatChange[0];

    public static List<Trait> GetStandardTraits()
    {
        return new ()
        {
            new Acrobat(),
            new Agility(),
            new Artificer(),
            new Attunement(),
            new Absorption(),
            new Catalyze(),
            new Celerity(),
            new Cutpurse(),
            new Expertise(),
            new Explosive(),
            new Ferocity(),
            new Fortitude(),
            new Harvesting(),
            new Haste(),
            new LethalAccuracy(),
            new Lucky(),
            new Mastery(),
            new Obliterate(),
            new Potential(),
            new PrimordialSpiral(),
            new Rampage(),
            new Recovery(),
            new SeekingSpirits(),
            new Shadowstep(),
            //new Shapeshifter(),
            new Sharpened(),
            new Splitshot(),
            new Vampirism(),
            new Voltage(),
            new Wisdom(),
        };
    }
    public static List<Trait> GetCursedTraits()
    {
        return new ()
        {
            new Berserk(),
            new Bloodlust(),
            new CursedCoin(),
            //new Deathbringer(),
            new Featherweight(),
            new NarrowMinded(),
            new Reckless()
        };
    }


    
    public delegate bool TraitRequirementCheck(Trait trait);
    public static List<Trait> GetAvailableTraitsByRequirements(TraitRequirementCheck requirementCheck = null)
    {
        List<Trait> traits = new();
        foreach (Trait trait in GetStandardTraits())
        {
            traits.Add(trait);
        }

        List<Trait> ret = new();
        foreach (Trait trait in traits)
        {
            if (requirementCheck == null || requirementCheck(trait))
            {
                ret.Add(trait);
            } 
        }

        return ret;
    }

    static Trait GetRandomTraitByRequirements(TraitRequirementCheck requirementCheck = null)
    {
        List<Trait> requirementMeetingTraits = new();
        foreach (Trait trait in GetStandardTraits())
        {
            if (requirementCheck == null || requirementCheck(trait))
            {
                requirementMeetingTraits.Add(trait);
            }
        }

        if (requirementMeetingTraits.Count == 0)
        {
            Debug.LogWarning("Zero traits meet a search requirement");
            return null;
        }

        return requirementMeetingTraits[UnityEngine.Random.Range(0, requirementMeetingTraits.Count)];
    }

    public static Trait GetRandomCursedTrait()
    {
        List<Trait> cursedTraits = GetCursedTraits();
        return cursedTraits[UnityEngine.Random.Range(0, cursedTraits.Count())];
    }

    public static List<Trait> GetRandomUniqueCursedTraits(int number)
    {
        List<Trait> ret = new();
        List<Trait> cursedTraits= GetCursedTraits();

        for (int i = 0; i < number && i < cursedTraits.Count(); i++)
        {
            Trait trait = cursedTraits[UnityEngine.Random.Range(0, cursedTraits.Count())];

            ret.Add(trait);
            cursedTraits.Remove(trait);
        }

        return ret;
    }

    public static List<Trait> GetLevelupTraits(Player player, int numberOfOptions)
    {
        List<Trait> allTraits = new();
        foreach (Trait trait in GetStandardTraits()) allTraits.Add(trait);
    
        List<Trait> returnedTraits = new();

        for (int i = 0; i < numberOfOptions; i++)
        {
            Rarity traitRarity = RollTraitRarity(player.GetStat(Stats.Luck, 1.0F));

            Trait rolledTrait = null;

            Rarity workingRarity = traitRarity;
            while (rolledTrait == null) 
            {
                rolledTrait = GetRandomTraitByRequirements(
                    trait =>
                    {
                        return returnedTraits.Find(x => x.GetName() == trait.GetName()) == null
                            && trait.PlayerMeetsRequirements(player)
                            && trait.GetRarity() == workingRarity
                            && (trait.maxStacks <= 0 || player.GetTraits().StacksOf(trait) < trait.maxStacks);
                    }
                );

                if (rolledTrait == null) // No trait found, decrease rarity and try again
                {
                    workingRarity -= 1;
                }

                if (workingRarity < Rarity.Common)
                {
                    break;
                }
            }

            if (rolledTrait == null)
            {
                throw new Exception("No trait found, retries exceeded max. Original rarity was " + traitRarity);
            }

            returnedTraits.Add(rolledTrait);
            allTraits.Remove(rolledTrait);

        }

        return returnedTraits;
    }

    static Rarity RollTraitRarity(float luck = 1.0F)
    {
        float rarityRoll = UnityEngine.Random.Range(0.0F, 100.0F) *  ((100.0F - luck) / 100.0F);
        
        //Assuming luck is 25%:
        // A roll of 100 becomes 75
        // A roll of 12 becomes 8

        if (rarityRoll < LEGENDARY_TRAIT_CHANCE * luck) return Rarity.Legendary;
        else if (rarityRoll < EPIC_TRAIT_CHANCE * luck) return Rarity.Epic;
        else if (rarityRoll < RARE_TRAIT_CHANCE * luck) return Rarity.Rare;
        else if (rarityRoll < UNCOMMON_TRAIT_CHANCE * luck) return Rarity.Uncommon;
        else /* COMMON */                            return Rarity.Common;
    }

    /// <summary>
    /// Returns a random trait. Factors in the rarity of traits so that uncommon traits are less likely than common, etc.
    /// </summary>
    /// <returns></returns>
    public static Trait GetRandomTraitAccountingForRarity(float luck = 1.0F)
    {   
        Rarity traitRarity = RollTraitRarity(luck);

        return GetRandomTraitByRequirements(
            (Trait trait) =>
            {
                return trait.GetRarity() == traitRarity;
            }
        );
    }

    
    /// <summary>
    /// Returns a random trait. Factors in the rarity of traits so that uncommon traits are less likely than common, etc.
    /// </summary>
    /// <returns></returns>
    public static Trait GetRandomTraitAccountingForRarityAndRequirements(TraitRequirementCheck traitRequirementCheck)
    {   
        Rarity traitRarity = RollTraitRarity();

        return GetRandomTraitByRequirements(
            (Trait trait) =>
            {
                return trait.GetRarity() == traitRarity && traitRequirementCheck(trait);
            }
        );
    }

    public void AddStack()
    {
        if (maxStacks > 0 && numberOfStacks + 1 > maxStacks)
        {
            numberOfStacks = maxStacks;
        }
        else
        {
            numberOfStacks++;
        }
    }

    public void AddStacks(int number)
    {
        if (maxStacks > 0 && numberOfStacks + number > maxStacks)
        {
            numberOfStacks = maxStacks;
        }
        else
        {
            numberOfStacks += number;
        }
    }

    public void SetNumberOfStacks(int numStacks)
    {
        numberOfStacks = numStacks;
    }

    protected void AddStatChange(StatChange statChange)
    {
        List<StatChange> statChangesAsList = statChanges.ToList();
        statChangesAsList.Add(statChange);
        statChanges = statChangesAsList.ToArray();
    }

    public int GetNumberOfStacks()
    {
        return numberOfStacks;
    }

    public virtual Rarity GetRarity()
    {
        return Rarity.Common;
    }

    public abstract string GetName();
    public abstract string GetStatsDescription(Player targetPlayer = null);
    public virtual string GetDescription() {return "";}
    public abstract Sprite GetSpriteIcon();
    public virtual int GetProcOrder() { return 0; } // Arbitrary for now. Define it with respect to other traits that need to be in order with this

    public TraitInfo ToInfo()
    {
        return new()
        {
            rarity = GetRarity(),
            traitName = GetName(),
            traitDescription = GetStatsDescription(),
            numStacks = GetNumberOfStacks()
        };
    }

    public List<StatChange> GetStatChanges()
    {
        List<StatChange> ret = new();

        foreach (StatChange statChange in statChanges)
        {
            ret.Add(new(statChange.stat, statChange.calcMode, statChange.amount * numberOfStacks));
        }

        return ret;
    }


    public virtual bool PlayerMeetsRequirements(Player player)
    {
        return true;
    }

    
    public void GiveToPlayer(Player player)
    {
        player.AddTrait(this);
    }

    public Sprite GetIcon()
    {
        return GetSpriteIcon();
    }

    public ShopOfferingInfo GetInfo()
    {
        return new()
        {
            type = ShopOfferingType.Trait,
            name = GetName(),
            typeName = nameof(Trait),
            statsDescription = GetStatsDescription(),
            description = GetDescription(),
            goldCost = GetShopCost()
        };
    }

    
    public GameObject GetPreviewPrefab()
    {
        TraitTomeMesh tomeInst = GameObject.Instantiate(AetherdaleData.GetAetherdaleData().traitTomePreviewPrefab);
        tomeInst.SetTrait(this);

        tomeInst.gameObject.transform.position = new Vector3(0, 8000, 0);

        return tomeInst.gameObject;
    }

    public int GetShopCost()
    {
        return (int) (SHOP_COST.Calculate(AreaSequencer.GetAreaSequencer().GetNextAreaLevel()) * GetShopCostMult());
    }

    float GetShopCostMult()
    {
        return GetRarity() switch
        {
            Rarity.Common => COMMON_SHOP_COST_MULT,
            Rarity.Uncommon => UNCOMMON_SHOP_COST_MULT,
            Rarity.Rare => RARE_SHOP_COST_MULT,
            Rarity.Epic => EPIC_SHOP_COST_MULT,
            Rarity.Legendary => LEGENDARY_SHOP_COST_MULT,
            Rarity.Cursed => CURSED_SHOP_COST_MULT,
            _ => 1.0F,
        };
    }

    public virtual EventReference GetAcquiredSound()
    {
        return GetRarity() switch
        {
            Rarity.Common => AetherdaleData.GetAetherdaleData().soundData.traits.traitAcquiredCommon,
            _ => AetherdaleData.GetAetherdaleData().soundData.traits.traitAcquiredCommon,  
        };
    }


    #region Functionality Overrides
    // Overridden by traits to provide functionality to certain events
    public virtual void OnProcessTraits(Player player) {} // periodically invoked
    public virtual void OnTraitAcquired(Player player, TraitList receivingList) {}
    public virtual void OnKill(HitInfo hitResult) {}
    public virtual void OnHit(HitInfo hitResult) {}
    public virtual void OnDamaged(HitInfo damagingHitInfo) {}
    public virtual void OnTransform(Entity previous, Entity newEntity) {}
    public virtual void OnAbility(Entity owner, int energyUsed) {}
    public virtual void OnDodgeStart(Entity entity) {}
    public virtual void OnDodgeEnd(Entity entity) {}
    public virtual void OnProjectileCreated(Projectile projectilePrefab, Projectile projectileInstance) {}
    public virtual void OnNewArea(Player player) {}
    public virtual float ModifyDamageForTarget(Entity attacker, Entity target, float damage) { return damage; }
    public virtual void ModifyEnemyItemDrops(Entity killer, List<DropInstance> dropTableEntries) {}

    #endregion
}

public class TraitInfo
{
    public string traitName;
    public string traitDescription;
    public int numStacks;
    public Rarity rarity;

    public Trait ToTrait()
    {
        Trait newTrait = (Trait) Activator.CreateInstance(Type.GetType(traitName.Replace(" ", "")));
        newTrait.SetNumberOfStacks(numStacks);

        return newTrait;
    }
}


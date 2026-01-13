

using System.Collections.Generic;
using UnityEngine;

public class Voltage : Trait
{
    const int PERCENT_CHANCE_PER_STACK = 30;
    const int DAMAGE_PERCENT = 80;

    const float MAX_CHAIN_LIGHTNING_DISTANCE = 18.0F;

    static Dictionary<int /* instance id */, List<Entity> /* entities this chain has hit */> chainInstances = new(); 
    const float CHAIN_INSTANCES_RESET_INTERVAL = 5F;
    const int VOLTAGE_IMPACT = 50;
    float lastReset = 0;


    public override string GetName()
    {
        return "Voltage";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"{PERCENT_CHANCE_PER_STACK}% to shock another nearby enemy for ${DAMAGE_PERCENT} of the original damage, on hit.";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().voltageSprite;
    }

    public override Rarity GetRarity()
    {
        return Rarity.Rare;
    }

    public Voltage()
    {
        maxStacks = 2;
    }

    public int GetProcChance()
    {
        return numberOfStacks * PERCENT_CHANCE_PER_STACK;
    }

    public override void OnHit(HitInfo hitResult)
    {
        if (hitResult.damageDealer != null && (hitResult.hitType == HitType.Attack || hitResult.hitType == HitType.Ability))
        {
            if (UnityEngine.Random.Range(0, 100) <= GetProcChance())
            {
                List<Entity> excludedList = new();
                excludedList.Add(hitResult.entityHit);

                if (hitResult.originEffectInstanceId != 0 && chainInstances.ContainsKey(hitResult.originEffectInstanceId))
                {
                    foreach (Entity entity in chainInstances[hitResult.originEffectInstanceId])
                    {
                        excludedList.Add(entity);
                    }
                }
                
                Entity nearestOtherEnemy = WorldManager.GetNearestEnemyOfFaction(hitResult.damageDealer.GetFaction(), hitResult.entityHit.GetWorldPosCenter(), MAX_CHAIN_LIGHTNING_DISTANCE, excludedList);
                
                if (nearestOtherEnemy != null)
                {
                    ChainLightningBolt chainLightningBolt = AetherdaleData.GetAetherdaleData().voltageTraitBolt;
                    ChainLightningBolt boltInstance = GameObject.Instantiate(chainLightningBolt);
                    boltInstance.SetData(0.25F, hitResult.entityHit, nearestOtherEnemy);

                    int id = boltInstance.GetInstanceID();
                    if (hitResult.originEffectInstanceId != 0 && chainInstances.ContainsKey(id))
                    {
                        // Existing proc chain
                        id = hitResult.originEffectInstanceId;
                        chainInstances[id].Add(hitResult.entityHit);
                    }
                    else
                    {
                        // New proc chain
                        chainInstances.Add(id, excludedList);
                    }
                    
                    //boltInstance.OnLifespanRanOut += () => {
                        nearestOtherEnemy.Damage((int) (hitResult.premitigationDamage * (DAMAGE_PERCENT / 100.0F)), Element.Storm, HitType.Ability, hitResult.damageDealer, originEffectInstanceId:id, impact:VOLTAGE_IMPACT);
                    //};

                    // Spawn sparks
                    GameObject.Instantiate(VisualEffectIndex.GetDefaultEffectIndex().voltageHitSplat, nearestOtherEnemy.GetWorldPosCenter(), Quaternion.identity);
                }
            }
        }
        
        if (Time.time - lastReset >= CHAIN_INSTANCES_RESET_INTERVAL)
        {
            chainInstances = new();
            lastReset = Time.time;
        }
    }
}
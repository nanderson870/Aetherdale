

using Mirror;
using UnityEngine;

public class Deathbringer : Trait
{
    public const int SCYTHE_DAMAGE_PER_HIT_PER_STACK = 15;
    public const int SCYTHE_DURATION = 25;
    public const int SCYTHES_PER_INTERVAL_PER_STACK = 6;
    public const int SCYTHE_SUMMON_RANGE = 60;

    float lastSummon = -SCYTHE_DURATION;

    public Deathbringer()
    {
    }

    public override string GetName()
    {
        return "Deathbringer";
    }

    public override Rarity GetRarity()
    {
        return Rarity.Cursed;
    }


    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Summon spinning curse scythes around you which harm enemies, but also you";
    }

    public override Sprite GetSpriteIcon()
    {
        return AetherdaleData.GetAetherdaleData().deathbringerSprite;
    }

    public override void OnProcessTraits(Player player)
    {
        base.OnProcessTraits(player);

        if ((Time.time - lastSummon) > SCYTHE_DURATION)
        {
            SummonScythes(player.GetControlledEntity());
        }
    }

    void SummonScythes(Entity entity)
    {
        lastSummon = Time.time;

        for (int i = 0; i < (SCYTHES_PER_INTERVAL_PER_STACK * numberOfStacks); i++)
        {
            CreateScythe(entity);
        }
    }

    void CreateScythe(Entity origin)
    {
        Vector3 position = origin.GetWorldPosCenter() + Random.insideUnitSphere * SCYTHE_SUMMON_RANGE;
        Quaternion rotation = Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f));
        AreaOfEffect scytheAOE = MonoBehaviour.Instantiate(AetherdaleData.GetAetherdaleData().spinningCurseScytheAOE, position, rotation);
        NetworkServer.Spawn(scytheAOE.gameObject);

        scytheAOE.SetDuration(SCYTHE_DURATION);
        scytheAOE.SetDamage(SCYTHE_DAMAGE_PER_HIT_PER_STACK * numberOfStacks);
    }

}
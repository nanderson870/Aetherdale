using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class AncientIllusionistDecoy : StatefulCombatEntity
{

    const float ILLUSORY_ENTITY_INTERVAL = 10;
    const float ILLUSORY_ENTITY_RADIUS = 10;
    const float ENTITIES_PER_INTERVAL = 3;

    const float MAGIC_BLAST_INITIAL_VELOCITY = 30;


    [SerializeField] Material illusoryEntityMaterial;
    [SerializeField] VisualEffect illusoryEntityDeathVFX;

    [SerializeField] SeekingProjectile magicBlastProjectile;
    [SerializeField] Transform magicBlastOrigin;
    
    [SerializeField] EventReference illusionIdleSound;
    [SerializeField] EventReference illusionDamagedSound;

    [SerializeField] GameObject illusionistParticlesVFXPrefab;

    List<Entity> illusoryEntities = new();

    int maxIllusoryEntities = 0;
    float lastIllusionSummon = 0;

    public override void Start()
    {
        base.Start();


        if (isServer)
        {
            maxIllusoryEntities = AreaManager.CurrentAreaManager.GetMaxEnemies();
        }

        Instantiate(illusionistParticlesVFXPrefab, transform.position, transform.rotation);
    }


    public override void Attack(Entity target = null)
    {
        if (CanCreateIllusions())
        {
            CreateIllusions();
        }
        else
        {
            MagicBlast(target);
        }
    }

    public override void Die()
    {
        ValidateIllusoryEntities();

        foreach (Entity illusion in illusoryEntities)
        {
            Debug.Log(illusion + " should die too now");
            illusion.Die();
        }

        base.Die();
    }


    #region Illusions

    bool CanCreateIllusions()
    {
        return (Time.time - lastIllusionSummon) > ILLUSORY_ENTITY_INTERVAL;
    }

    void CreateIllusions()
    {
        // AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        SetAnimatorTrigger("Summon");
        SetAttacking();
    }


    void SummonCallback()
    {
        if (isServer)
        {
            CreateIllusoryEntities();
        }
    }

    void CreateIllusoryEntities()
    {
        lastIllusionSummon = Time.time;
        maxIllusoryEntities = AreaManager.CurrentAreaManager.GetMaxEnemies();

        ValidateIllusoryEntities();

        if (illusoryEntities.Count >= maxIllusoryEntities)
        {
            return;
        }

        for (int i = 0; i < ENTITIES_PER_INTERVAL; i++)
        {
            Entity entityPrefab = AreaManager.CurrentAreaManager.GetArea().region.spawnList.GetEntity(AreaSequencer.GetAreaSequencer().GetEnemyLevel());
            StartCoroutine(CreateIllusoryEntity(entityPrefab));
        }
    }

    IEnumerator CreateIllusoryEntity(Entity entityPrefab)
    {
        Vector2 offset = Random.insideUnitCircle * ILLUSORY_ENTITY_RADIUS;
        Vector3 position = transform.position + new Vector3(offset.x, 0, offset.y);

        Entity illusion = Instantiate(entityPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(illusion.gameObject);

        illusion.SetFaction(GetFaction());
        illusion.SetCountedBySpawners(false);

        illusion.OnDeath += OnIllusionDied;


        yield return null;

        illusion.SetStat(Stats.GlobalDamageMultiplier, 0);
        illusion.SetStat(Stats.MaxHealth, 1);
        illusion.SetStat(Stats.DroppedLootMult, 0);
        illusion.SetStat(Stats.ExperienceRewardedMultiplier, 0);

        yield return null;

        illusion.RpcSetMaterial(illusoryEntityMaterial.name, MaterialChangeProperties.All);

        illusoryEntities.Add(illusion);

        RpcSetupIllusoryEntity(illusion);

    }

    [ClientRpc]
    void RpcSetupIllusoryEntity(Entity illusoryEntity)
    {
        illusoryEntity.idleSound = illusionIdleSound;
        illusoryEntity.damagedSound = illusionDamagedSound;
    }

    void ValidateIllusoryEntities()
    {
        for (int i = illusoryEntities.Count - 1; i >= 0; i--)
        {
            if (illusoryEntities[i] == null)
            {
                illusoryEntities.RemoveAt(i);
            }
        }
    }

    [Server]
    private void OnIllusionDied(Entity illusion, Entity killer)
    {
        illusion.gameObject.AddComponent<AutoDestroy>().lifespan = 1.0F;

        illusion.createsCorpse = false;

        illusion.AttachVisualEffect(illusoryEntityDeathVFX);
    }
    #endregion


    #region Magic Blast
    void MagicBlast(Entity target)
    {
        currentTarget = target;
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        lastAttack = Time.time;
        SetAnimatorTrigger("MagicBlast");
        SetAttacking();
    }

    public void MagicBlastCallback()
    {
        if (isServer)
        {
            Projectile.FireAtEntityWithPrediction(this, currentTarget, magicBlastProjectile, magicBlastOrigin.position, MAGIC_BLAST_INITIAL_VELOCITY, 0.5F);
        }
    }
    #endregion
}

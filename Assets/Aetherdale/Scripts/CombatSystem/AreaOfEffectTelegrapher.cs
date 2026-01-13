using UnityEngine;
using Mirror;
using FMODUnity;

public class AreaOfEffectTelegrapher : NetworkBehaviour
{
    public AreaOfEffect areaOfEffect;
    public AreaOfEffect.AOEProperties properties;
    public float lifespan = 1.0F;
    public EventReference spawnSound;


    Entity damageDealer;
    HitType hitType;
    int impact;

    float creationTime;


    public static AreaOfEffect.AOEProperties Create(AreaOfEffectTelegrapher telegrapherPrefab, AreaOfEffect aoePrefab, Vector3 position, Entity damageDealer, HitType hitType = HitType.None, int impact = 0, Transform parentTransform = null, bool useParentRotation = false)
    {
        AreaOfEffectTelegrapher telegrapherInstance;
        if (parentTransform != null)
        {
            telegrapherInstance = Instantiate(telegrapherPrefab, position, useParentRotation ? parentTransform.rotation : Quaternion.identity, parentTransform);
        }
        else
        {
            telegrapherInstance = Instantiate(telegrapherPrefab, position, Quaternion.identity);
        }

        telegrapherInstance.areaOfEffect = aoePrefab;
        telegrapherInstance.damageDealer = damageDealer;
        telegrapherInstance.hitType = hitType;
        telegrapherInstance.impact = impact;

        telegrapherInstance.properties = aoePrefab.properties.Copy();

        NetworkServer.Spawn(telegrapherInstance.gameObject);

        return telegrapherInstance.properties;
    }



    public void Start()
    {
        if (isServer)
        {
            creationTime = Time.time;
        }

        AudioManager.Singleton.PlayOneShot(spawnSound, transform.position);
    }

    public void Update()
    {
        if (isServer)
        {
            if ((Time.time - creationTime) > lifespan)
            {
                CreateAOE();
            }
        }
    }


    public void SetProperties(Entity damageDealer, HitType hitType, int impact)
    {
        this.damageDealer = damageDealer;
        this.hitType = hitType;
        this.impact = impact;
    }

    [Server]
    public void CreateAOE()
    {
        if (areaOfEffect == null)
        {
            Debug.LogError($"No AOE set on telegrapher {this}");
            Teardown();
            return;
        }

        AreaOfEffect.Create(areaOfEffect, transform.position, damageDealer, hitType, impact, skipTelegraph: true);
        Teardown();
    }

    void Teardown()
    {
        NetworkServer.UnSpawn(gameObject);
        Destroy(gameObject);
    }
}
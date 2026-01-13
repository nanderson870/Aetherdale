using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RogueWraithRogue : StatefulCombatEntity
{

    [SerializeField] Projectile knifeProjectile;
    [SerializeField] int percentDoubleThrowChance = 33;
    [SerializeField] float knifeProjectileSpeed = 30.0F;

    [SerializeField] Transform mainhandKnifeSpawn;
    [SerializeField] Transform offhandKnifeSpawn;

    [SerializeField] GameObject mainhandKnifeObject;
    [SerializeField] GameObject offhandKnifeObject;
    
    const float knifeDespawnLength = 2.0F; // how long the knife mesh stays inactive after thrown
    float lastMainhandKnifeDespawn = -10.0F;
    float lastOffhandKnifeDespawn = -10.0F;

    Entity mainhandTarget;
    Entity offhandTarget;
    
    protected override void Animate()
    {
        base.Animate();
        
        if (isClient)
        {
            if (!mainhandKnifeObject.activeSelf && Time.time - lastMainhandKnifeDespawn > knifeDespawnLength)
            {
                mainhandKnifeObject.SetActive(true);
            }
            
            if (!offhandKnifeObject.activeSelf && Time.time - lastOffhandKnifeDespawn > knifeDespawnLength)
            {
                offhandKnifeObject.SetActive(true);
            }
        }

    }
    
    [Server]
    public override void Attack(Entity target = null)
    {
        SetAttacking();
        SetAnimatorTrigger("Throw1");
        mainhandTarget = target;

        if (Random.Range(0, 100) <= percentDoubleThrowChance)
        {
            SetAnimatorTrigger("Throw2");
            offhandTarget = target;
        }
    }

    [ServerCallback]
    public void ThrowMainhandKnife()
    {
        RpcSetMainHandKnifeActive(false);

        Projectile.FireAtEntityWithPrediction(this, mainhandTarget, knifeProjectile, mainhandKnifeSpawn.position, knifeProjectileSpeed);

        mainhandTarget = null;
    }

    [ServerCallback]
    public void ThrowOffhandKnife()
    {
        RpcSetOffHandKnifeActive(false);

        Projectile.FireAtEntityWithPrediction(this, offhandTarget, knifeProjectile, mainhandKnifeSpawn.position, knifeProjectileSpeed);

        offhandTarget = null;
    }

    [ClientRpc]
    void RpcSetMainHandKnifeActive(bool active)
    {
        mainhandKnifeObject.SetActive(active);
        
        if (!active)
        {
            lastMainhandKnifeDespawn = Time.time;
        }
    }

    [ClientRpc]
    void RpcSetOffHandKnifeActive(bool active)
    {
        offhandKnifeObject.SetActive(active);
        
        if (!active)
        {
            lastOffhandKnifeDespawn = Time.time;
        }
    }
}

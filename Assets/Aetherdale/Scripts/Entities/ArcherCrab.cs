using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.VFX;

public class ArcherCrab : StatefulCombatEntity
{
    const float PROJECTILE_CHARGE_TIME = 1.0F;

    [SerializeField] Projectile waterProjectile;

    [SerializeField] Transform[] projectileSpawnTransforms;
    [SerializeField] VisualEffect[] projectileChargeVFXs;

    // Fleeing
    readonly float enemyTooClose = 4.5F; // how close an enemy has to be for the crab to flee
    readonly float fleeSpeed = 12.0f;
    readonly float fleeDuration = 6.0F;
    readonly float fleeTimeout = 8.0F;


    // Projectiles
    readonly float projectileVelocity = 60.0F;
    readonly float projectilePredictionStrength = 0.15F;


    float lastFlee;

    int lastFiredSpawnTransform = 0;


    protected override State GetPreferredState()
    {
        Entity nearestEnemy = GetPreferredEnemy(aggroRadius);
        State currentState = stateMachine.GetState();

        if (currentState is FleeState fleeState)
        {
            if (fleeState.ReadyForExit())
            {
                lastFlee = Time.time;

                if (nearestEnemy != null)
                {
                    return CreatePursuitState(nearestEnemy);
                }
                else
                {
                    return CreateWanderState();
                }
            }

            return null;
        }
        else if (stateMachine.GetState() is PursuitState pursuitState)
        {
            if (pursuitState.target == null)
            {
                return CreateWanderState();
            }

            if (Vector3.Distance(pursuitState.target.transform.position, transform.position) <= enemyTooClose
                && (Time.time - lastFlee) > fleeTimeout)
            {
                // Enemy is getting too close, crab should flee
                return new FleeState(this, nearestEnemy, fleeSpeed, fleeDuration);
            }

            return null;
        }
        else
        {
            if (nearestEnemy != null)
            {
                return CreatePursuitState(nearestEnemy);
            }
        }

        return base.GetPreferredState();
    }

    [Server]
    public override void Attack(Entity target = null)
    {
        lastAttack = Time.time;

        StartCoroutine(ShootProjectile(target));
    }

    IEnumerator ShootProjectile(Entity target)
    {
        lastFiredSpawnTransform++;
        if (lastFiredSpawnTransform >= projectileSpawnTransforms.Length)
        {
            lastFiredSpawnTransform = 0;
        }

        RpcStartChargeVFX(lastFiredSpawnTransform);

        yield return new WaitForSeconds(PROJECTILE_CHARGE_TIME);

        lastAttack = Time.time;

        if (target != null && SeesEntity(target))
        {
            Projectile.FireAtEntityWithPrediction(this, target, waterProjectile, projectileSpawnTransforms[lastFiredSpawnTransform].position, projectileVelocity, projectilePredictionStrength);

            PlayAnimation("Fire", 0.05F);
        }

        RpcStopChargeVFX(lastFiredSpawnTransform);
    }

    [ClientRpc]
    void RpcStartChargeVFX(int index)
    {
        projectileChargeVFXs[index].SendEvent("Start");
    }


    [ClientRpc]
    void RpcStopChargeVFX(int index)
    {
        projectileChargeVFXs[index].SendEvent("Stop");
    }

}

using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class SeekingProjectile : Projectile
{
    public enum NoTargetBehaviour
    {
        Destroy = 0,
        MaintainLastVelocity = 1,
        WaitForTarget = 2
    }

    public float minSpeed = 0;
    public float maxSpeed = 100;
    public float acceleration = 5;

    public NoTargetBehaviour noTargetBehaviour = NoTargetBehaviour.MaintainLastVelocity;

    [Header("Wait For Target Parameters")]
    public Faction faction = null;
    public float targetAcquisitionRange = 25.0F;

    float lastEnemyCheck = 0;
    const float WAIT_FOR_ENEMY_CHECK_INTERVAL = 0.5F;

    public static SeekingProjectile Create(SeekingProjectile projectile, Vector3 spawnPosition, GameObject progenitor, Entity target, float initialVelocity)
    {
        Vector3 direction = (target.GetWorldPosCenter() - spawnPosition).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        Vector3 velocity = direction * initialVelocity;
        // TODO - add initial angle "inaccuracy" so we can close in like plasma shrimp

        SeekingProjectile seekingProjectile = Projectile.Create(projectile, spawnPosition, rotation, progenitor, velocity);
        seekingProjectile.SetTarget(target.gameObject);

        return seekingProjectile;
    }


    public override void Start()
    {
        base.Start();

        if (isServer)
        {
            //if (body != null)
            //{
            //    body.isKinematic = true;
            //}
        }
    }

    public override void FixedUpdate()
    {
        if (isServer)
        {
            Vector3 direction = transform.forward;
            if (target != null)
            {
                Vector3 targetPos = target.transform.position;
                if (target.TryGetComponent(out Entity entity))
                {
                    targetPos = entity.GetWorldPosCenter();
                }

                Vector3 accelerationDirection = targetPos - transform.position;
                Vector3 additionalVelocity = acceleration * Time.deltaTime * accelerationDirection.normalized;
                velocity += additionalVelocity;

                velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude, minSpeed, maxSpeed);

                body.linearVelocity = velocity;
                transform.rotation = Quaternion.LookRotation(body.linearVelocity, Vector3.up);
            }
            else //target == null
            {
                switch (noTargetBehaviour)
                {
                    case NoTargetBehaviour.Destroy:
                        EndFlight();
                        break;

                    case NoTargetBehaviour.MaintainLastVelocity:
                        body.linearVelocity = velocity;
                        break;

                    case NoTargetBehaviour.WaitForTarget:
                        velocity = Vector3.zero;

                        if ((Time.time - lastEnemyCheck) >= WAIT_FOR_ENEMY_CHECK_INTERVAL)
                        {
                            if (target == null && WorldManager.GetNearestEnemyOfFaction(faction, transform.position, targetAcquisitionRange) is Entity entity && entity != null)
                            {
                                target = entity.gameObject;
                                active = true;
                            }
                            lastEnemyCheck = Time.time;
                        }
                        break;

                }
            }

        }
    }


    public void SetMovementProperties(float minSpeed, float maxSpeed, float acceleration)
    {
        this.minSpeed = minSpeed;
        this.maxSpeed = maxSpeed;
        this.acceleration = acceleration;

        velocity = new(0, minSpeed, 0);
    }

    public void SetActive(bool active, float secondsUntilActive = 0)
    {
        StartCoroutine(SetActiveCoroutine(active, secondsUntilActive));
    }

    IEnumerator SetActiveCoroutine(bool active, float secondsUntilActive)
    {
        yield return new WaitForSeconds(secondsUntilActive);

        this.active = active;
    }
    
    
    public override void HitCollider(Collider collider)
    {
        Damageable potentialDamageable = collider.GetComponentInParent<Damageable>();

        // Only care about collisions if activated
        if (active || noTargetBehaviour != NoTargetBehaviour.WaitForTarget)
        {
            base.HitCollider(collider);
        }
    }
}

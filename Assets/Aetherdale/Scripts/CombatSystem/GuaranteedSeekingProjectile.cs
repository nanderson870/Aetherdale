using System.Collections;
using UnityEngine;

public class GuaranteedSeekingProjectile : Projectile
{
    public enum NoTargetBehaviour
    {
        Destroy=0,
        MaintainLastVelocity=1,
        WaitForTarget=2
    }

    public float minSpeed = 0;
    public float maxSpeed = 100;
    public float acceleration = 5;

    public NoTargetBehaviour noTargetBehaviour = NoTargetBehaviour.MaintainLastVelocity;

    [Header("Wait For Target Parameters")]
    public Faction faction = null;
    public float targetAcquisitionRange = 25.0F;

    float lastEnemyCheck = 0;
    const float WAIT_FOR_TARGET_CHECK_INTERVAL = 0.5F;

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

                float currentSpeed = body.linearVelocity.magnitude;
                currentSpeed = Mathf.Clamp(currentSpeed + (acceleration * Time.deltaTime), minSpeed, maxSpeed);

                Vector3 velocityDirection = (targetPos - transform.position).normalized;
                
                body.linearVelocity = currentSpeed * velocityDirection;
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

                        if (target == null && (Time.time - lastEnemyCheck) >= WAIT_FOR_TARGET_CHECK_INTERVAL)
                        {
                            if (WorldManager.GetNearestEnemyOfFaction(faction, transform.position, targetAcquisitionRange) is Entity entity && entity != null)
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

        velocity = new (0, minSpeed, 0);
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
            Debug.Log(active);
            base.HitCollider(collider);
        }
    }
}
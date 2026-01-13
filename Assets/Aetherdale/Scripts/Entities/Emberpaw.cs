using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.VFX;
using Mirror;
using UnityEngine.Serialization;

public class Emberpaw : StatefulCombatEntity
{
    /// <summary> degrees </summary>
    const float MAX_HEAD_AIM_HEADING = 70.0F;

    const float HEAD_AIM_LERP_SPEED = 1.0F;

    const float FIRE_BREATH_COOLDOWN = 10.0F;
    [SerializeField] float fireBreathDuration = 2.0F;

    const float POUNCE_COOLDOWN = 3.0F;
    [SerializeField] float maxPounceRange = 24.0F;
    [SerializeField] float minPounceRange = 16.0F;

    const float MIN_FIRE_BREATH_RANGE = 12.0F;
    const float MAX_FIRE_BREATH_RANGE = 20.0F;

    const float BACKPEDAL_SPEED_MULT = 0.4F;

    [SerializeField] [FormerlySerializedAs("jumpHeight")] float pounceHeight = 3.0F;
    [SerializeField] float pouncePreparationTime = 0.75F;

    [SerializeField] Transform headAimTransform;
    [SerializeField] MultiAimConstraint headAimConstraint;
    [SerializeField] VisualEffect flameBreathEffect;
    [SerializeField] Projectile flameHitProjectile;

    [SerializeField] Hitbox swipe1Hitbox;
    const int SWIPE1_DAMAGE = 15;

    [SyncVar] bool breathingFire = false;
    [SyncVar] float headAimTargetWeight;

    float lastFireBreath = 0.0F;

    float lastPounce = 0.0F;

    public override void OnStartClient()
    {
        base.OnStartClient();

        flameBreathEffect.enabled = false;
        flameBreathEffect.enabled = true;
        flameBreathEffect.Stop();
        flameBreathEffect.Reinit();
    }

    public override void Update()
    {
        base.Update();

        if (headAimConstraint.weight != headAimTargetWeight)
        {
            headAimConstraint.weight = Mathf.Lerp(headAimConstraint.weight, headAimTargetWeight, HEAD_AIM_LERP_SPEED * Time.deltaTime);
        }

        if (isServer && breathingFire && (Time.time - lastFlameProjectileTime) > 0.1F)
        {
            Projectile.Create(flameHitProjectile, flameBreathEffect.transform, gameObject, flameBreathEffect.transform.TransformVector(new Vector3(0, 15.0F, 0)));
            lastFlameProjectileTime = Time.time;
        }
    }

    protected override void Animate()
    {
        base.Animate();

        if (movementMode == MovementMode.Rigidbody)
        {
            animator.SetFloat("yVelocity", GetRigidBody().linearVelocity.y);
        }
        else
        {
            animator.SetFloat("yVelocity", localVelocity.y);
        }
    }

    float lastFlameProjectileTime = 0.0F;

    protected override State GetPreferredState()
    {
        Entity nearestEnemy = GetPreferredEnemy(aggroRadius);
        if (stateMachine.GetState() is EmberpawFireBreathingState fireBreathingState)
        {
            // If breathing fire, check if done
            if (fireBreathingState.ReadyForExit())
            {
                return base.GetPreferredState();
            }
            else
            {
                return null;
            }
        }
        else if (stateMachine.GetState() is PounceState pounceState)
        {
            if (!pounceState.ReadyForExit())
            {
                return null;
            }
        }
        else
        {
            if (ShouldPounce(nearestEnemy))
            {
                return new PounceState(this, nearestEnemy);
            }
            else if (CanBreathFire(nearestEnemy))
            {
                return new EmberpawFireBreathingState(this, nearestEnemy);
            }
        }

        // Otherwise base behavior is acceptable
        return base.GetPreferredState();
    }

    public override void Attack(Entity target = null)
    {
        SetAnimatorTrigger("Swipe");
        SetAttacking();
    }

    [ServerCallback]
    public void Swipe1Hit()
    {
        swipe1Hitbox.HitOnce(SWIPE1_DAMAGE, Element.Physical, this);
    }

    bool CanBreathFire(Entity target)
    {
        return target != null 
            && (Time.time - lastFireBreath) > FIRE_BREATH_COOLDOWN 
            && Vector3.Distance(transform.position, target.transform.position) < MAX_FIRE_BREATH_RANGE;
    }

    bool ShouldPounce(Entity target)
    {
        return target != null
            && (Time.time - lastPounce) > POUNCE_COOLDOWN 
            && Vector3.Distance(transform.position, target.transform.position) < maxPounceRange
            && Vector3.Distance(transform.position, target.transform.position) > minPounceRange;
    }

    void StartBreathingFire()
    {
        breathingFire = true;
        RpcPlayFlames();
    }

    [ClientRpc]
    void RpcPlayFlames()
    {
        flameBreathEffect.Play();
    }

    void StopBreathingFire()
    {
        breathingFire = false;
        RpcStopFlames();
    }

    [ClientRpc]
    void RpcStopFlames()
    {
        flameBreathEffect.Stop();
    }

    
    protected override void OnStaggered()
    {
        if (stateMachine.GetState() is EmberpawFireBreathingState fireBreathingState)
        {
            fireBreathingState.Interrupt();
        }
    }

    private class PounceState : State
    {
        readonly Emberpaw emberpaw;
        readonly Entity target;
        bool done = false;


        bool jumping = false;
        float startTime;
        float jumpStartTime;

        float totalTime = 0;

        public PounceState(Emberpaw emberpaw, Entity target)
        {
            this.emberpaw = emberpaw;
            this.target = target;
        }

        public override void OnEnter()
        {
            emberpaw.SetAnimatorTrigger("PouncePrepare");
            startTime = Time.time;
        }

        void StartJump()
        {
            jumping = true;
            jumpStartTime = Time.time;

            Vector3 targetPoint = target.transform.position + target.GetVelocity() * 0.33F;
            Vector3 startPoint = emberpaw.transform.position;
            
            Vector3 direction = new(targetPoint.x - startPoint.x, 0, targetPoint.z - startPoint.z);
            float distance = direction.magnitude;
            direction.Normalize();

            float heightDifference = targetPoint.y - startPoint.y;

            float verticalVelocity = Mathf.Sqrt(-2 * (Physics.gravity.y * 3) * emberpaw.pounceHeight);

            float timeToPeak = verticalVelocity / -(Physics.gravity.y * 3);

            float timeToDescend = Mathf.Sqrt(2 * (emberpaw.pounceHeight - heightDifference) / -(Physics.gravity.y * 3));

            totalTime = timeToPeak + Mathf.Max(timeToDescend, 0);

            Vector3 horizontalVelocity = direction * (distance / totalTime);


            Vector3 velocity = new (horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);

            emberpaw.SwitchMovementToRigidbody();
            emberpaw.GetRigidBody().linearDamping = 0;
            emberpaw.body.linearVelocity = velocity;


            emberpaw.SetAnimatorTrigger("Pounce");
        }

        public override void Update()
        {
            if ((Time.time - startTime) <= emberpaw.pouncePreparationTime)
            {
                emberpaw.TurnTowards(target.gameObject, emberpaw.rotationSpeed * Time.deltaTime);
                return;
            }
            else 
            {
                if (!jumping)
                {
                    StartJump();
                    return;
                }
            }

            if ((Time.time - jumpStartTime) >= (totalTime / 1.8))
            {
                if (Mathf.Abs(emberpaw.GetRigidBody().linearVelocity.y) < 0.05F)
                {
                    done = true;
                    emberpaw.SetAnimatorTrigger("PounceEnd");
                    emberpaw.SwitchMovementToNavMeshAgent();
                }
            }

            // Apply 3x gravity
            emberpaw.GetRigidBody().linearVelocity += new Vector3(0, Physics.gravity.y * 2 * Time.deltaTime, 0);
        }

        public override void OnExit()
        {
            emberpaw.lastPounce = Time.time;
        }

        public override bool ReadyForExit()
        {
            return done;
        }
    }

    private class EmberpawFireBreathingState : State
    {

        readonly Emberpaw emberpaw;
        readonly Entity target;

        float startTime;

        bool interrupted = false;

        public EmberpawFireBreathingState(Emberpaw emberpaw, Entity target)
        {
            this.emberpaw = emberpaw;
            this.target = target;
        }

        
        public override void OnEnter()
        {
            emberpaw.lastFireBreath = Time.time;
            emberpaw.StartBreathingFire();

            startTime = Time.time;
        }

        public override void Update()
        {
            float distanceToTarget = Vector3.Distance(emberpaw.transform.position, target.transform.position);
            if (distanceToTarget >= MAX_FIRE_BREATH_RANGE)
            {
                emberpaw.SetDestination(target.transform.position);
            }
            else if (distanceToTarget < MIN_FIRE_BREATH_RANGE)
            {
                Vector3 direction = emberpaw.transform.position - target.transform.position;
                Vector3 destination = target.transform.position + (direction.normalized * MIN_FIRE_BREATH_RANGE);
                
                emberpaw.SetDestination(destination, BACKPEDAL_SPEED_MULT * emberpaw.originalNavMeshAgentSpeed);
                emberpaw.TurnTowards(target.gameObject, emberpaw.rotationSpeed);
            }
            else
            {
                emberpaw.ClearDestination();
                if (Mathf.Abs(emberpaw.GetRelativeBearingAngle(target.gameObject)) > (0.8F * MAX_HEAD_AIM_HEADING))
                {
                    emberpaw.TurnTowards(target.gameObject, emberpaw.rotationSpeed);
                }
            }

            if (target != null)
            {
                if (Mathf.Abs(emberpaw.GetRelativeBearingAngle(target.gameObject)) < 60.0F)
                {
                    emberpaw.headAimTransform.position = target.GetComponent<Collider>().bounds.center - new Vector3(0, 0.2F, 0);
                    emberpaw.headAimTargetWeight = 1;
                }
                else
                {
                    emberpaw.headAimTargetWeight = 0;
                }
            }
        }

        public void Interrupt()
        {
            interrupted = true;
        }

        public override void OnExit()
        {
            emberpaw.StopBreathingFire();
            emberpaw.headAimTargetWeight = 0;
        }

        public override bool ReadyForExit()
        {
            return interrupted || (Time.time - startTime) > emberpaw.fireBreathDuration || target == null || target.IsDead();
        }

    }

}

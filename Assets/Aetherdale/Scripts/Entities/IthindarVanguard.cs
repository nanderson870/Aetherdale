using System.Collections;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class IthindarVanguard : StatefulCombatEntity
{
    [Header("Basic Attacks")]
    public int attackDamage = 10;
    public int attackImpact = 50;
    public Hitbox attackHitbox;
    public float knockbackMagnitude = 6.0F;
    public VisualEffect attack1VFX;
    public GameObject hammer;
    public Transform hand;


    [Header("Shield Barge Attack")]
    public int shieldBargeDamage = 15;
    public float shieldBargeKnockbackMagnitude = 8.0F;
    public float minShieldBargeDistance = 3.0F;
    public float maxShieldBargeDistance = 15.0F;
    public float shieldBargeCooldown = 8.0F;
    public Hitbox shieldHitbox;



    [Header("Hammer Throw Attack")]
    public int hammerThrowDamage = 20;
    public float hammerThrowVelocity = 35.0F;
    public float hammerRecallVelocity = 50.0F;
    public float hammerRecallAcceleration = 30.0F;
    public float minHammerThrowDistance = 10.0F;
    public float maxHammerThrowDistance = 20.0F;
    public float hammerThrowCooldown = 8.0F;
    public Projectile thrownHammerPrefab;


    [Header("Charge Attack")]
    public int chargeDamage = 25;
    public float chargeVelocity = 45.0F;
    public float chargeKnockbackMagnitude = 12.0F;
    public float minChargeDistance = 15.0F;
    public float maxChargeDistance = 50.0F;
    public float chargeCooldown = 8.0F;
    public float chargeOvershoot = 5.0F;
    [SerializeField] EventReference chargeRoar;
    [SerializeField] VisualEffect chargeVFX;

    [Header("Abilities")]
    public float globalCooldown = 3.0F;


    /* RUNTIME */
    bool blocking = false;
    bool barging = false;
    [SyncVar] bool charging = false;
    float lastAbility = 0.0F;
    float lastShieldBarge = 0.0F;
    float lastCharge = 0.0F;
    float lastHammerThrow = 0.0F;

    Entity hammerThrowTarget;

    protected override State GetPreferredState()
    {
        State currentState = stateMachine.GetState();
        if (!currentState.ReadyForExit())
        {
            return null;
        }

        Entity preferredEnemy = GetPreferredEnemy();
        //If can charge, charge
        if (CanCharge(preferredEnemy))
        {
            return new VanguardChargeState(this, preferredEnemy);
        }
        else if (preferredEnemy != null && currentState is not VanguardEngageState)
        {
            // Engage state handles shield barge and hammer throw
            return new VanguardEngageState(this, preferredEnemy);
        }

        return base.GetPreferredState();
    }

    
    protected override void Animate()
    {
        base.Animate();

        Vector2 horizontalVelocity = new(localVelocity.x, localVelocity.z);

        if (horizontalVelocity.magnitude > 0.5F && !airborne)
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 1.0F);
        }
        else
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 0.0F);
        }

        animator.SetBool("blocking", blocking);
    }

    public override bool CanMove()
    {
        return !barging && !stunned && !animator.GetBool("staggered") && Time.time - lastStagger > UNIVERSAL_STAGGER_DURATION && navMeshAgent.enabled;
    }

    public override bool CanTurn()
    {
        return base.CanTurn() && !barging;
    }

    bool CanCharge(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

        return IsOvershootGrounded(target, chargeOvershoot)
            && (Time.time - lastCharge) > chargeCooldown 
            && (Time.time - lastAbility) > globalCooldown
            && distanceToTarget >= minChargeDistance
            && distanceToTarget <= maxChargeDistance;
    }


    #region Attacks
    bool CanBarge(Entity target)
    {
        if (target == null)
        {
            return false;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

        float heading = GetRelativeBearingAngle(target.gameObject);

        return (Time.time - lastShieldBarge) > shieldBargeCooldown 
            && (Time.time - lastAbility) > globalCooldown
            && distanceToTarget >= minShieldBargeDistance
            && distanceToTarget <= maxShieldBargeDistance;
    }

    bool CanHammerThrow(Entity target)
    {
        if (target == null)
        {
            return false;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

        return (Time.time - lastHammerThrow) > hammerThrowCooldown 
            && (Time.time - lastAbility) > globalCooldown
            && distanceToTarget >= minHammerThrowDistance
            && distanceToTarget <= maxHammerThrowDistance;
    }

    public override bool CanAttack(Entity target = null)
    {
        if (target == null || !hammer.activeSelf)
        {
            return false;
        }

        /*
        Charge handled by states
        */

        if (CanBarge(target))
        {
            return true;
        }

        if (CanHammerThrow(target))
        {
            return true;
        }
        
        return base.CanAttack(target) && !barging && !charging;
    }

    public override void Attack(Entity target = null)
    {
        if (CanBarge(target))
        {
            Barge(target);
        }
        else 
        if (CanHammerThrow(target))
        {
            HammerThrow(target);
        }
        else
        {
            lastAttack = Time.time;
            SetAttacking();

            PlayAnimation("WarhammerAttack1", 0.1F);
        }
    }

    void Barge(Entity target)
    {
        ClearDestination();
        transform.LookAt(target.transform.position + target.GetVelocity() * 0.85F);
        PlayAnimation("ShieldBarge", 0.05F);

        lastShieldBarge = Time.time;
        lastAbility = Time.time;
    }

    void HammerThrow(Entity target)
    {
        PlayAnimation("HammerThrow", 0.05F);

        lastHammerThrow = Time.time;
        lastAbility = Time.time;

        hammerThrowTarget = target;
    }


    #endregion

    void ApplyAttackKnockback(HitboxHitData hitData)
    {
        Entity target = hitData.hitEntity;

        target.Stagger();

        Vector3 direction = (target.transform.position - transform.position + new Vector3(0, 0.5F, 0)).normalized;
        target.Push(direction * knockbackMagnitude, forceMode:ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.2F, 0.3F, target.transform.position, frequency:3);
    }

    void ApplyBargeKnockback(HitboxHitData hitData)
    {
        Entity target = hitData.hitEntity;

        Vector3 direction = (target.transform.position - transform.position + new Vector3(0, 0.5F, 0) + transform.forward).normalized;

        target.Push(direction * shieldBargeKnockbackMagnitude, forceMode:ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.5F, 0.75F, target.transform.position, frequency:3);
    }

    void ApplyChargeKnockback(HitboxHitData hitData)
    {
        Entity target = hitData.hitEntity;

        Vector3 direction = (target.transform.position - transform.position + new Vector3(0, 0.5F, 0) + transform.forward).normalized;
        target.Push(direction * chargeKnockbackMagnitude, forceMode:ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.6F, 1F, target.transform.position, frequency:3);
    }

    void ApplyHammerThrowKnockback(Projectile projectile, HitInfo hitInfo)
    {
        Entity target = hitInfo.entityHit;

        Vector3 direction = (target.transform.position - projectile.transform.position + new Vector3(0, 1F, 0) + transform.forward).normalized;
        target.Push(direction * chargeKnockbackMagnitude, forceMode:ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.6F, 1F, target.transform.position, frequency:3);
    }


    
    void RecallHammer(Projectile projectile)
    {
        GameObject projectileObject = projectile.gameObject;

        StartCoroutine(RecallHammerCoroutine(projectileObject));
    }

    IEnumerator RecallHammerCoroutine(GameObject projectileObj)
    {
        Debug.Log("RECALL");
        projectileObj.GetComponent<Projectile>().enabled = false;
        projectileObj.GetComponent<Rigidbody>().isKinematic = true;

        float startTime = Time.time;
        float velocity = 0;
        while (Vector3.Distance(projectileObj.transform.position, hand.position) >= 0.2F)
        {
            if (Time.time - startTime > 10.0F)
            {
                break;    
            }

            velocity = Mathf.Clamp(velocity + (hammerRecallAcceleration * Time.deltaTime), 0, hammerRecallVelocity);

            Vector3 direction = hand.position - projectileObj.transform.position;
            projectileObj.transform.position += direction.normalized * velocity * Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        NetworkServer.UnSpawn(projectileObj);
        Destroy(projectileObj);

        hammer.SetActive(true);
        Debug.Log("DONE RECALL");
    }


    #region Animator Callbacks
    public void WeaponAttackHit()
    {
        if (isServer)
        {
            attackHitbox.HitOnce(attackDamage, Element.Physical, this, impact:attackImpact, onHitAction:ApplyAttackKnockback);
            AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
            attack1VFX.SendEvent("Play");
        }
    }

    public void ShieldBargeStart()
    {
        if (isServer)
        {
        Debug.Log("Barge start");
            barging = true;
            navMeshAgent.enabled = false;
            shieldHitbox.StartHit(shieldBargeDamage, Element.Physical, HitType.Ability, this, impact:100, onHitAction:ApplyBargeKnockback);
        }
    }

    public void ShieldBargeEnd()
    {
        if (isServer)
        {
        Debug.Log("Barge end");
            barging = false;
            navMeshAgent.enabled = true;
            shieldHitbox.EndHit();
        }
    }

    public void ShieldChargeEnter()
    {
        if (isServer)
        {
            charging = true;
            channeling = false;
            shieldHitbox.StartHit(chargeDamage, Element.Physical, HitType.Ability, this, impact:100, onHitAction:ApplyChargeKnockback);
        
            Vector3 targetPos = currentTarget.transform.position + (0.2F * currentTarget.GetVelocity());
            Vector3 overshoot = (targetPos - transform.position).normalized * 3.0F;

            SlideToPositionWithSpeed(targetPos + overshoot, chargeVelocity, ShieldChargeExit);
        }

        chargeVFX.SendEvent("Start");
    }

    public void ShieldChargeExit()
    {
        if (isServer)
        {
            charging = false;
            RpcShieldChargeExit();
        }
    }

    [ClientRpc]
    void RpcShieldChargeExit()
    {
        chargeVFX.SendEvent("Stop");
    }

    public void HammerThrowRelease()
    {
        hammer.SetActive(false);

        Projectile projectile = Projectile.FireAtEntityWithPrediction(this, hammerThrowTarget, thrownHammerPrefab, hammer.transform.position, hammerThrowVelocity);

        projectile.OnFlightEnd += RecallHammer;

        projectile.SetDamage(hammerThrowDamage);
        projectile.OnHit += ApplyHammerThrowKnockback;

        hammerThrowTarget = null;
    }

    #endregion


    #region Behaviors
    
    /*
    Engage a target with standard features from a PursuitState

    Hammer throw and barge are treated as attacks and are therefore handled
    by this state.
    */
    protected class VanguardEngageState : State
    {
        public IthindarVanguard vanguard;
        public Entity target;

        public float lastTimeTargetSeen;
        public Vector3 lastSeenPosition;

        public VanguardEngageState(IthindarVanguard vanguard, Entity target)
        {
            this.vanguard = vanguard;
            this.target = target;

            lastTimeTargetSeen = Time.time;
        }

        public override void OnEnter()
        {
            target.OnTransformed += ChangeTarget;
            vanguard.currentTarget = target;
        }

        public override void Update()
        {
            if (target == null)
            {
                return;
            }

            if (!vanguard.SeesEntity(target))
            {
                // Don't see target, try to go where we last saw them
                vanguard.SetDestination(lastSeenPosition);
                return;
            }

            
            lastTimeTargetSeen = Time.time;
            lastSeenPosition = target.transform.position;

            // Attempt to move to target
            if (vanguard.CanMove() && !vanguard.InAttackRange(target))
            {
                vanguard.SetDestination(target.transform.position);
            }
            else
            {
                vanguard.ClearDestination();
            }
            
            // Attempt to turn towards target
            if (vanguard.CanTurn())
            {
                vanguard.TurnTowards(target.gameObject, vanguard.rotationSpeed);
            }
        
            // Attempt to attack
            if (vanguard.CanAttack(target) && !vanguard.IsAttacking())
            {
                vanguard.Attack(target);
                vanguard.lastAttack = Time.time;

                if (vanguard.CanTurn())
                {
                    vanguard.TurnTowards(target.gameObject, vanguard.navMeshAgent.angularSpeed / 30); 
                }
            }
            else
            {
                vanguard.blocking = true;
            }


            vanguard.RecalculateAvoidanceRadiusForPursuit(target);
        }

        public override void OnExit()
        {
            if (target != null)
            {
                target.OnTransformed -= ChangeTarget;
            }
            
            vanguard.ClearDestination();
        }

        public void ChangeTarget(Entity oldEntity, Entity newTarget)
        {
            target.OnTransformed -= ChangeTarget;
            target = newTarget;
            target.OnTransformed += ChangeTarget;
        }

        public override bool ReadyForExit()
        {
            return true; // Pursuit state is low priority, always ready for exit
        }
    }
    

    public class VanguardChargeState : State
    {
        public IthindarVanguard vanguard;
        public Entity target;

        bool done = false;
        float startTime;
        float originalNMASpeed;
        float originalNMAAccel;
        float originalNMAAngSpeed;
        const float MANEUVERABILITY_HEADING = 10.0F;
        const float ACCELERATION = 50;
        const float ANGULAR_SPEED = 90;

        const float BREAK_DISTANCE = 20.0F; // Break out if beyond this distance, and heading to target is > 60 deg
        const float MAX_DURATION=20.0F; //seconds

        public VanguardChargeState(IthindarVanguard vanguard, Entity target)
        {
            this.vanguard = vanguard;
            this.target = target;

            originalNMASpeed = vanguard.navMeshAgent.speed;
            originalNMAAccel = vanguard.navMeshAgent.acceleration;
            originalNMAAngSpeed = vanguard.navMeshAgent.angularSpeed;
        }

        public override void OnEnter()
        {
            vanguard.currentTarget = target;
            vanguard.blocking = false;
            vanguard.PlayAnimation("ShieldChargeEnter", 0.05F);
            vanguard.RpcSetAnimatorBool("shieldCharging", true);
            vanguard.channeling = true;
            AudioManager.Singleton.PlayOneShot(vanguard.chargeRoar, vanguard.GetWorldPosCenter());

            startTime = Time.time;
            vanguard.lastCharge = Time.time;
            vanguard.lastAbility = Time.time;

            // vanguard.navMeshAgent.speed = vanguard.chargeVelocity;
            // vanguard.navMeshAgent.acceleration = ACCELERATION;
            // vanguard.navMeshAgent.angularSpeed = ANGULAR_SPEED;

            vanguard.transform.LookAt(target.transform);

            // vanguard.ClearDestination();
            // vanguard.navMeshAgent.velocity = new();

        }


        public override void Update()
        {
            vanguard.lastCharge = Time.time;
            vanguard.lastAbility = Time.time;

            if (!vanguard.charging)
            {
                vanguard.transform.LookAt(target.transform);
            }

            // Vector3 destination = vanguard.transform.TransformVector(0, 0, vanguard.chargeVelocity);
            // float targetHeading = vanguard.GetRelativeBearingAngle(target.gameObject);
            // float distanceToTarget = Vector3.Distance(vanguard.transform.position, target.transform.position);
            // if (Mathf.Abs(targetHeading) >= 60 && distanceToTarget >= BREAK_DISTANCE)
            // {
            //     done = true;
            //     return;
            // }
            // else if (Mathf.Abs(targetHeading) <= MANEUVERABILITY_HEADING)
            // {
            //     Vector3 vectorToTarget = target.transform.position - vanguard.transform.position;
            //     destination = vanguard.transform.position + vectorToTarget.normalized * vanguard.chargeVelocity;
            // }

            // vanguard.SetDestination(destination, vanguard.chargeVelocity);

        }
        public override void OnExit()
        {
            Debug.Log("CHARGE EXIT");
            vanguard.RpcSetAnimatorBool("shieldCharging", false);
            
            // vanguard.navMeshAgent.speed = originalNMASpeed;
            // vanguard.navMeshAgent.acceleration = originalNMAAccel;
            // vanguard.navMeshAgent.angularSpeed = originalNMAAngSpeed;

            vanguard.ClearDestination();
            // vanguard.navMeshAgent.velocity = new();

            vanguard.shieldHitbox.EndHit();
            vanguard.charging = false;
            vanguard.currentTarget = null;
        }


        public override bool ReadyForExit()
        {
            return !vanguard.channeling && !vanguard.charging;
        }

    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;

public class RunewaspQueen : Boss
{
    [SerializeField] Collider thoraxCollider;

    [SerializeField] Hitbox stingerHitbox;
    
    const float SPIT_PROJECTILE_SPEED = 15.0F;
    [SerializeField] Projectile spitProjectile;
    [SerializeField] Transform spitProjectileOrigin;
    readonly float spitRange = 20.0F;
    readonly float spitCooldown = 10.0F;


    [SerializeField] Hitbox rushHitbox;
    readonly int rushDamage = 45;
    readonly float rushMaxRange = 60.0F;
    readonly float rushMinRange = 25.0F;
    readonly float rushCooldown = 20.0F;
    readonly float rushSpeed = 35.0F;

    float spitCooldownRemaining = 0.0F;
    Entity spitTarget;

    float rushCooldownRemaining;
    bool flying = false;
    Vector3 rushDestination = new();
    List<Entity> entitiesHitThisRush = new();

    
    EventInstance idleSoundInstance;


    public override void Start()
    {
        base.Start();

        // TODO
        // for (int i = 0; i < runicAlphabetSymbols.Length; i++)
        // {
        //     runicAlphabetSymbols[i].SetIndex(Random.Range(0, 25));
        // }

        if (isClient)
        {
            idleSoundInstance = RuntimeManager.CreateInstance(idleSound);
            RuntimeManager.AttachInstanceToGameObject(idleSoundInstance, transform);
            idleSoundInstance.start();
        }
    }

    public override void Update()
    {
        base.Update();

        spitCooldownRemaining -= Time.deltaTime;
        if (spitCooldownRemaining < 0)
        {
            spitCooldownRemaining = 0;
        }

        rushCooldownRemaining -= Time.deltaTime;
        if (rushCooldownRemaining < 0)
        {
            rushCooldownRemaining = 0;
        }
    }

    protected override void Animate()
    {
    }

    protected override State GetPreferredState()
    {
        Entity nearestEnemy = GetPreferredEnemy();
        State currentState = stateMachine.GetState();

        if (currentState is RushState rushState)
        {
            if (rushState.ReadyForExit())
            {
                return base.GetPreferredState();
            }

            return null;
        }
        else if (currentState is PursuitState pursuitState && pursuitState.target != null)
        {
            if (ShouldRush(pursuitState.target))
            {
                rushCooldownRemaining = rushCooldown;
                return new RushState(this, pursuitState.target);
            }
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

    public override void Attack(Entity target = null)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (spitCooldownRemaining <= 0 && distanceToTarget <= spitRange)
        {
            SetAnimatorTrigger("Spit");
            spitTarget = target;
            
            lastAttack = Time.time;
            spitCooldownRemaining = spitCooldown;
            return;
        }

        base.Attack(target);
    }

    [ServerCallback]
    public void SpitEvent()
    {
        // Raycast check for ground to aim for. If no hit, aims at target directly
        Vector3 projectileTargetPoint;
        
        if (spitTarget != null) 
        {
            projectileTargetPoint = spitTarget.GetWorldPosCenter();
        }
        else
        {
            // TODO launch straight ahead
            spitTarget = null;
            return;
        }

        Projectile.FireAtEntityWithPrediction(this, spitTarget, spitProjectile, spitProjectileOrigin.position, SPIT_PROJECTILE_SPEED);
        spitTarget = null;
    }

    public override bool CanAttack(Entity target)
    {
        return base.CanAttack(target) && !flying;
    }

    public override bool CanTurn()
    {
        return base.CanTurn() && !flying && !IsAttacking();
    }

    public override bool InAttackRange(Entity target)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);


        if (spitCooldownRemaining <= 0)
        {
            return distance <= spitRange;
        }

        return distance <= maxAttackRange;
    }

    public bool ShouldRush(Entity target)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);

        return rushCooldownRemaining <= 0 
            && distanceToTarget >= rushMinRange 
            && distanceToTarget <= rushMaxRange
            && GetRelativeBearingAngle(target.gameObject) <= 1.0F
            && spitTarget == null;
    }
    

    [Server]
    public void FlyTo(Vector3 destination)
    {
        rushDestination = destination;
        SetAnimatorTrigger("EnterRush");
        animator.SetBool("rushing", true);
    }

    [ServerCallback]
    public void RushStartMovement()
    {
        rushHitbox.StartHit(rushDamage, Element.Physical, HitType.Attack, this, impact: 300);
        StartCoroutine(nameof(FlyToCoroutine), rushDestination);
    }


    IEnumerator FlyToCoroutine(Vector3 destination)
    {
        flying = true;

        navMeshAgent.enabled = false;

        body.isKinematic = false;
        body.useGravity = false;

        //body.angularVelocity = Vector3.zero;
        
        body.linearVelocity = Vector3.zero;

        Vector3 direction = destination - transform.position;
        float duration = direction.magnitude / rushSpeed;

        float startTime = Time.time;
        body.linearVelocity = direction.normalized * rushSpeed;

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        while ((Time.time - startTime) <= duration)
        {
            if (Physics.Raycast(GetWorldPosCenter(), transform.forward, 4.0F, LayerMask.GetMask("Default")))
            {
                // Something in front of us
                flying = false;

                body.linearVelocity = Vector3.zero;
                body.isKinematic = true;
                body.useGravity = false;

                navMeshAgent.Warp(transform.position);
                navMeshAgent.enabled = true;

                animator.SetBool("rushing", false);
                yield break;
            }

            yield return null;
        }
        
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        body.useGravity = false;

        navMeshAgent.Warp(transform.position);
        navMeshAgent.enabled = true;

        flying = false;
        animator.SetBool("rushing", false);
    }

    public override Vector3 GetWorldPosCenter()
    {
        return thoraxCollider.bounds.center;
    }


    [ServerCallback]
    public void StingerHit()
    {
        stingerHitbox.HitOnce(30, Element.Physical, this);
    }


    public override void OnDestroy()
    {
        base.OnDestroy();
        
        if (isClient)
        {
            idleSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    #region States
    class RushState : State
    {
        public readonly RunewaspQueen wasp;
        public Entity target;

        Vector3 destination;

        public RushState(RunewaspQueen wasp, Entity target)
        {
            this.wasp = wasp;
            this.target = target;

        }

        public override void OnEnter()
        {
            target.OnTransformed += ChangeTarget;

            Vector3 expectedPos = target.transform.position + target.GetVelocity() * 0.66F;

            Vector3 rush = expectedPos - wasp.transform.position;

            // Both of these adjustments serve to make sure we are aiming for the target, regardless of distance
            rush *= 1.25F; // scale distance out a bit
            rush += rush.normalized * 3.0F; // add flat 3m to distance

            if (rush.magnitude > wasp.rushMaxRange)
            {
                rush = rush.normalized * wasp.rushMaxRange;
            }
            else if (rush.magnitude < wasp.rushMinRange)
            {
                rush = rush.normalized * wasp.rushMinRange;
            }

            destination = wasp.transform.position + (rush.normalized * wasp.rushMaxRange);
            wasp.FlyTo(destination);
        }

        public override void Update()
        {
            wasp.TurnTowards(destination, wasp.rotationSpeed * Time.deltaTime);
        }

        public override void OnExit()
        {
            wasp.rushHitbox.EndHit();
        }

        public void ChangeTarget(Entity oldTarget, Entity newTarget)
        {
        }

        public override bool ReadyForExit()
        {
            return !wasp.flying;
        }
    }
    
    #endregion

}

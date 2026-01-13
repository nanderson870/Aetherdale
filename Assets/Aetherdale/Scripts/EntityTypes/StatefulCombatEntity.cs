using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;
using FMODUnity;

/*
A fundamental behavior for entities whose AI uses my state machine model
*/

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public abstract class StatefulCombatEntity : Entity
{
    static bool globalAIEnabled = true;
    public static void SetStatefulCombatEntityGlobalAI(bool enabled) {globalAIEnabled = enabled;}
    public static bool GetStatefulCombatEntityGlobalAIEnabled() {return globalAIEnabled;}

    [Header("Settings")]
    [SerializeField] protected float backpedalSpeedMult = 0.5F;
    [SerializeField] protected bool autoPlayIdleSounds = true;
    float minIdleSoundInterval = 10;
    float maxIdleSoundInterval = 20;
    
    [Header("Pursuit")]
    [SerializeField] protected float aggroRadius = 50.0F;
    [SerializeField] protected float dropAggroRadius = 75.0F;
    [SerializeField] float noLOSDropAggroTime = 5.0F;


    [Header("Wandering")]
    [SerializeField] protected float wanderRadius = 12.0F;
    [SerializeField] protected float maxWanderDuration = 10.0F;
    [SerializeField] float maxPauseDuration = 3.0F;
    [SerializeField] float minPauseDuration = 1.0F;
    [SerializeField] protected float wanderSpeed = 3.0F;


    [Header("Attacks")]
    [SerializeField] protected float maxAttackRange = 3.5F;
    [SerializeField] protected float minAttackRange = 0.0F;
    [SerializeField] protected float attackCooldown = 4.0F;


    [SerializeField] float relativeHeadingRangeForAttack = 15.0F;
    [SerializeField] public float rotationSpeed = 12.0F;
    [SerializeField] protected EventReference attackSound;


    public bool aiEnabled = true;

    [SyncVar] protected Vector3 localVelocity = new();

    protected bool spawned = false;



    protected float originalNavMeshAgentSpeed;
    protected float originalNavMeshAcceleration;
    protected float originalNavMeshAngularSpeed;
    protected float originalAvoidanceRadius;

    float idleSoundTimeout = 0;

    public override void Awake()
    {
        base.Awake();
        animator.Update(0);
    }

    public override void Start()
    {
        base.Start();

        movementMode = MovementMode.NavMeshAgent;
        defaultMovementMode = MovementMode.NavMeshAgent;

        navMeshAgent = GetComponent<NavMeshAgent>();
        originalNavMeshAgentSpeed = navMeshAgent.speed;
        originalNavMeshAcceleration = navMeshAgent.acceleration;
        originalNavMeshAngularSpeed = navMeshAgent.angularSpeed;
        originalAvoidanceRadius = navMeshAgent.radius;

        navMeshAgent.radius = 0;
        StartCoroutine(nameof(SpoolUpAvoidanceRadius));

        body.isKinematic = true;
        body.useGravity = false;

        body.constraints = RigidbodyConstraints.FreezeRotation;


        // Play spawn animation if present
        bool spawnAnim = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Spawn")
            {
                spawnAnim = true;
                // SetAnimatorTrigger("Spawn");
            }
        }

        if (NetworkServer.active)
        {
            if (spawnAnim)
            {
                stateMachine.ChangeState(new DormantState(this));
            }
            else
            {
                if (!stateMachine.HasState())
                {
                    // No spawn anim, get right into it I guess
                    spawned = true;
                    stateMachine.ChangeState(CreateWanderState());
                }
            }

            navMeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }
    }

    public override void Update()
    {
        base.Update();

        if (isServer)
        {
            localVelocity = transform.InverseTransformVector(navMeshAgent.velocity);

            // Debug.Log("STATE IS " + stateMachine.GetState());
            if (stateMachine.GetState() is DeadState)
            {
                return;
            }
            else if (!aiEnabled || !globalAIEnabled)
            {
                if (stateMachine.GetState() is not DormantState)
                {
                    stateMachine.ChangeState(new DormantState(this));
                }
            }
            else if (Time.timeScale > 0 && spawned)
            {
                State preferredState = GetPreferredState();

                if (preferredState != null)
                {
                    stateMachine.ChangeState(preferredState);
                }

                stateMachine.Update();

                // Perform cleanup
                if (!CanMove())
                {
                    // Shouldn't move if we can't
                    ClearDestination();
                }
            }


            if (!idleSound.IsNull && autoPlayIdleSounds)
            {
                if (idleSoundTimeout <= 0)
                {
                    AudioManager.Singleton.PlayOneShot(idleSound, GetWorldPosCenter());
                    idleSoundTimeout = UnityEngine.Random.Range(minIdleSoundInterval, maxIdleSoundInterval);
                }
                else
                {
                    idleSoundTimeout -= Time.deltaTime;
                }
            }
        }
    }

    public void SpawnAnimationComplete()
    {
        if (isServer)
        {
            stateMachine.ChangeState(CreateWanderState());
            spawned = true;
        }
    }

    IEnumerator SpoolUpAvoidanceRadius()
    {
        while (navMeshAgent.radius != originalAvoidanceRadius)
        {
            navMeshAgent.radius = Mathf.Lerp(navMeshAgent.radius, originalAvoidanceRadius, 0.5F * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Returns the state this entity wishes to be in. Returns null if its current state is the preferred state
    /// 
    /// The base version of this function is all most entities will need - it handles basic aggro and waiting. It
    /// can be overridden for entities that have more specialized state logic, and used as a fallback.
    /// </summary>
    protected virtual State GetPreferredState()
    {
        State currentState = stateMachine.GetState();

        AreaPortal nextAreaPortal = AreaManager.CurrentAreaManager.GetRandomChargingPortal();

        float adjustedAggroRadius = aggroRadius;
        if (nextAreaPortal != null && nextAreaPortal.rebuilding)
        {
            adjustedAggroRadius = aggroRadius * 1.3F;
        }

        Entity preferredEnemy = GetPreferredEnemy(aggroRadius);

        // Priority case - dead
        if (currentState is DeadState deadState || !gameObject.activeSelf)
        {
            return null;
        }

        float distanceToPortal = Mathf.Infinity;
        if (nextAreaPortal != null)
        {
            distanceToPortal = Vector3.Distance(nextAreaPortal.transform.position, transform.position);
        }


        // Priority case - needs to travel to portal
        if (currentState is not TravelState && preferredEnemy == null && nextAreaPortal != null && nextAreaPortal.rebuilding && distanceToPortal > adjustedAggroRadius)
        {
            // We should make our way to the portal, if it's rebuilding and we aren't already occupied
            return new TravelState(this, nextAreaPortal.transform.position, 600, wanderSpeed * 2, UnityEngine.Random.Range(10.0F, 20.0F));
        }
        
        if (currentState is DormantState dormantState)
        {
            // Dormant state waits for aggro-able target and transfers into pursuit state if one exists
            if (preferredEnemy != null && SeesEntity(preferredEnemy))
            {
                return CreatePursuitState(preferredEnemy);
            }
        }
        else if (currentState is PursuitState pursuitState)
        {
            if (pursuitState.target == null)
            {
                return CreateWanderState(); 
            }

            if (preferredEnemy != null && preferredEnemy != pursuitState.target)
            {
                return CreatePursuitState(preferredEnemy);
            }

            //Debug.Log("pursuit");
            // Pursuit state changes to wait if the target is missing
            if (!IsAttacking() && !ShouldKeepAggro(pursuitState.target) || (Time.time - pursuitState.lastTimeTargetSeen) >= noLOSDropAggroTime)
            {
                return CreateWanderState();
            }
        }
        else if (currentState is WanderState wanderState || currentState is TravelState travelState)
        {
            // In wander state, we transition to pursuit if there's a nearby enemy, otherwise transition to wait if we've reached the destination
            if (preferredEnemy != null && Vector3.Distance(transform.position, preferredEnemy.transform.position) <= adjustedAggroRadius)
            {
                return CreatePursuitState(preferredEnemy);
            }
            else if (currentState.ReadyForExit())
            {
                return new WaitState(this);
            }
        }
        else if (currentState is WaitState waitState)
        {
            if (preferredEnemy != null)
            {
                return CreatePursuitState(preferredEnemy);
            }
            else if (waitState.ReadyForExit()) // Otherwise default to wander
            {
                return CreateWanderState();
            }

        }
        else if (currentState is FleeState fleeState)
        {
            if (fleeState.fleeTarget == null)
            {
                return CreateWanderState();
            }
        }
        else
        {
            // No idea what state we're in right now. Aggro if there's a nearby enemy
            if (preferredEnemy != null)
            {
                return CreatePursuitState(preferredEnemy);
            }
            else // Otherwise default to wander
            {
                return CreateWanderState();
            }

        }

        return null;
    }

    public virtual State CreateWanderState()
    {
        return new WanderState(this, maxWanderDuration, wanderSpeed, wanderRadius);
    }

    public virtual State CreatePursuitState(Entity target)
    {
        return new PursuitState(this, target);
    }

    protected override void Animate()
    {
        animator.speed = globalAIEnabled || isDead ? 1 : 0;

        animator.SetFloat("zVelocity", localVelocity.z);
    }

    public override void Attack(Entity target = null)
    {
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        lastAttack = Time.time;
        SetAnimatorTrigger("Attack");
        SetAttacking();
    }

    private bool ShouldKeepAggro(Entity target)
    {
        if (Vector3.Distance(transform.position, target.transform.position) >= dropAggroRadius)
        {
            return false;
        }

        return true;
    }

    public override void AdjustStatsForLevel()
    {
        base.AdjustStatsForLevel();

        SetStat(Stats.GlobalDamageMultiplier, Equation.ENEMY_GLOBAL_DAMAGE_MULTIPLIER.Calculate(entityLevel));
    }

    public override Vector3 GetVelocity()
    {
        return navMeshAgent.velocity;
    }

    public override bool CanMove()
    {
        return base.CanMove() && navMeshAgent.enabled && movementMode == MovementMode.NavMeshAgent && !IsAttacking();
    }

    public virtual bool InAttackRange(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= maxAttackRange;
    }

    public virtual bool TooCloseToTarget(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) < minAttackRange;
    }

    public override bool IsAttacking()
    {
        return attacking;
    }

    protected virtual bool PreventedFromAttacking()
    {
        return IsAttacking()
            || beingPushed
            || IsStunned();
    }

    public virtual bool CanAttack(Entity target)
    {
        return !PreventedFromAttacking()
            && (Time.time - lastAttack) >= attackCooldown
            && SeesEntity(target)
            && InAttackRange(target)
            && (Mathf.Abs(GetRelativeBearingAngle(target.gameObject)) <= relativeHeadingRangeForAttack || relativeHeadingRangeForAttack == 0);
    }

    public override void Stun(float duration)
    {
        base.Stun(duration);

        ClearDestination(abruptStop:true);
    }

    public virtual void SetDestination(Vector3 destination, float speed = 0.0F, float acceleration = 0.0F, float angularSpeed = 0.0F, bool sampleClosest = false)
    {
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        NavMeshQueryFilter filter = new();
        filter.agentTypeID = navMeshAgent.agentTypeID;
        filter.areaMask = NavMesh.AllAreas;

        if (sampleClosest)
        {
            NavMeshHit hit;
            NavMesh.SamplePosition(destination, out hit, Mathf.Infinity, filter);
            destination = hit.position;
        }

        navMeshAgent.SetDestination(destination);

        if (Mathf.Approximately(speed, 0.0F))
        {
            navMeshAgent.speed = originalNavMeshAgentSpeed * GetStat(Stats.MovementSpeedMult);
        }
        else
        {
            navMeshAgent.speed = speed * GetStat(Stats.MovementSpeedMult);
        }
        
        if (Mathf.Approximately(acceleration, 0.0F))
        {
            navMeshAgent.acceleration = originalNavMeshAcceleration;
        }
        else
        {
            navMeshAgent.acceleration = acceleration * GetStat(Stats.MovementSpeedMult);
        }
        
        if (Mathf.Approximately(angularSpeed, 0.0F))
        {
            navMeshAgent.angularSpeed = originalNavMeshAngularSpeed;
        }
        else
        {
            navMeshAgent.angularSpeed = angularSpeed * GetStat(Stats.MovementSpeedMult);
        }
        
    }

    public virtual void ClearDestination(bool abruptStop = false)
    {
        if (navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
        }

        if (abruptStop) navMeshAgent.velocity = new();

        navMeshAgent.speed = originalNavMeshAgentSpeed;
        navMeshAgent.acceleration = originalNavMeshAcceleration;
        navMeshAgent.angularSpeed = originalNavMeshAngularSpeed;
    }

    public override void Move(Vector3 magnitude)
    {
        //transform.position += magnitude;
        //navMeshAgent.nextPosition = transform.position;
        //navMeshAgent.Warp(transform.position + magnitude);
    }

    public void RecalculateAvoidanceRadiusForPursuit(Entity target)
    { 
        // Experimental avoidance radius calculation, to make radius smaller when near target
        navMeshAgent.radius = Mathf.Clamp(Mathf.Sqrt(Vector3.Distance(transform.position, target.transform.position)), 0.2F, originalAvoidanceRadius);    
    }

    public Rigidbody GetRigidBody()
    {
        return body;
    }

    
    public virtual bool HasAppropriateAttackAngle(Entity target)
    {
        float relativeHeadingToTarget = GetRelativeBearingAngle(target.gameObject);
        return Mathf.Abs(relativeHeadingToTarget) >= 0.5F * relativeHeadingRangeForAttack;
    }

    public virtual Vector3 GetPreferredPositionFromTarget(Entity target, out float speed)
    {
        if (TooCloseToTarget(target))
        {
            Vector3 direction = transform.position - target.transform.position;

            speed = originalNavMeshAgentSpeed * backpedalSpeedMult;

            return target.transform.position + 2 * minAttackRange * direction.normalized;
        }
        else
        {
            speed = 0;
            return target.transform.position;
        }

    }

    // public virtual bool IsAttackPositionValid(Vector3 position, Entity target)
    // {
    //     return 
    // }



    #region STATES
    protected class DormantState : State
    {
        public Entity entity;

        public DormantState(Entity entity)
        {
            this.entity = entity;
        }

        public override void OnEnter()
        {
            if (entity.TryGetComponent(out StatefulCombatEntity sce))
            {
                sce.ClearDestination();
            }
        }

        [ServerCallback]
        public override void Update()
        {
        }

        public override bool ReadyForExit()
        {
            return false; // Make no assumptions. We will force the entity out of dormant state when needed.
        }
    }

    protected class PursuitState : State
    {
        protected const int DESTINATION_RECALCULATION_FRAME_INTERVAL = 10;

        public StatefulCombatEntity entity;
        public Entity target;

        public float lastTimeTargetSeen;
        public Vector3 lastSeenPosition;

        protected int destinationRecalculationInterval = 0;

        public PursuitState(StatefulCombatEntity entity, Entity target)
        {
            this.entity = entity;
            this.target = target;

            lastTimeTargetSeen = Time.time;
        }

        public override void OnEnter()
        {
            entity.currentTarget = target;
            target.OnTransformed += ChangeTarget;
        }

        public override void Update()
        {
            if (target == null || !entity.IsValidEnemy(target))
            {
                return;
            }

            if (!entity.SeesEntity(target) && entity.CanMove())
            {
                // Don't see target, try to go where we last saw them
                entity.SetDestination(lastSeenPosition);
                return;
            }

            lastTimeTargetSeen = Time.time;
            lastSeenPosition = target.transform.position;

            // Attempt to move to target
            if (!entity.CanMove())
            {
                // Can't move
                entity.ClearDestination();
            }

            destinationRecalculationInterval--;
            if (destinationRecalculationInterval <= 0)
            {
                if (!entity.InAttackRange(target))
                {
                    float speed = 0;
                    Vector3 desiredPosition = entity.GetPreferredPositionFromTarget(target, out speed);

                    entity.SetDestination(desiredPosition, speed);
                }
                else
                {
                    entity.ClearDestination();
                }

                destinationRecalculationInterval = DESTINATION_RECALCULATION_FRAME_INTERVAL;
            }


            // Attempt to turn towards target
            if (entity.CanTurn())
            {
                entity.TurnTowards(target.gameObject, entity.rotationSpeed);
            }
        
            // Attempt to attack
            if (entity.CanAttack(target) && !entity.IsAttacking())
            {
                entity.Attack(target);
                //entity.lastAttack = Time.time;
            }


            entity.RecalculateAvoidanceRadiusForPursuit(target);
        }

        public override void OnExit()
        {
            if (target != null)
            {
                target.OnTransformed -= ChangeTarget;
            }

            entity.currentTarget = null;
            entity.ClearDestination();
        }

        public void ChangeTarget(Entity oldEntity, Entity newTarget)
        {
            target.OnTransformed -= ChangeTarget;
            target = newTarget;
            entity.currentTarget = newTarget;
            target.OnTransformed += ChangeTarget;
        }

        public override bool ReadyForExit()
        {
            return true; // Pursuit state is low priority, always ready for exit
        }
    }


    protected class WanderState : State
    {
        StatefulCombatEntity entity;
        protected Vector3 destination;
        float wanderSpeed;

        float wanderDurationLeft;


        public WanderState(StatefulCombatEntity entity, float maxWanderDuration, float wanderSpeed, float wanderRadius)
        {
            this.updateInterval = 3.0F;

            this.entity = entity;
            wanderDurationLeft = maxWanderDuration;
            this.wanderSpeed = wanderSpeed;

            destination = GetDestination(wanderRadius);
        }

        public virtual Vector3 GetDestination(float radius)
        {
            Vector2 destinationOffset = UnityEngine.Random.insideUnitCircle * radius;

            if (destinationOffset.magnitude < 0.5F * radius)
            {
                destinationOffset = destinationOffset.normalized * 0.5F * radius;
            }

            Vector3 pos = entity.transform.position + new Vector3(destinationOffset.x, 0.0F, destinationOffset.y);

            // Check if near navmesh and flip X if not
            if (!NavMesh.SamplePosition(pos, out NavMeshHit _, 15.0F, NavMesh.AllAreas))
            {
                pos.x *= -1;
            }

            // Check if near navmesh and flip Z if not
            if (!NavMesh.SamplePosition(pos, out NavMeshHit _, 15.0F, NavMesh.AllAreas))
            {
                pos.z *= -1;
            }
        
            // If it still isn't on the NavMesh at this point, let NavMesh figure it out and return it anyway

            return pos;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            entity.SetDestination(destination, wanderSpeed);
        }

        [ServerCallback]
        public override void Update()
        {
            wanderDurationLeft -= Time.deltaTime;

            if (entity.CanMove())
            {
                entity.SetDestination(destination);
            }
            else
            {
                Debug.Log(entity.CanMove());
                Debug.Log(entity.navMeshAgent);
                Debug.Log("CLEAR");
                entity.ClearDestination();
            }
            
        }

        [Server]
        public override bool ReadyForExit()
        {
            return entity.navMeshAgent.remainingDistance <= 2.0F /* TODO remove magic number, how close we need to be */
                || wanderDurationLeft <= 0;
        }
    }

    protected class TravelState : State
    {
        Vector3 destination;
        
        StatefulCombatEntity entity;
        float maxDurationLeft;
        float radiusRequired;


        public TravelState(StatefulCombatEntity entity, Vector3 destination, float maxTravelDuration, float travelSpeed, float radiusRequired)
        {
            this.updateInterval = 3.0F;
            
            this.entity = entity;
            maxDurationLeft = maxTravelDuration;
            entity.navMeshAgent.speed = travelSpeed;
            
            this.destination = destination;
            this.radiusRequired = radiusRequired;
        }

        [ServerCallback]
        public override void Update()
        {
            maxDurationLeft -= Time.deltaTime;

            if (entity.CanMove())
            {
                entity.SetDestination(destination);
            }
            else
            {
                entity.ClearDestination();
            }
            
        }

        [Server]
        public override bool ReadyForExit()
        {
            //Debug.Log((Vector3.Distance(entity.transform.position, destination) <= radiusRequired) + " & " + maxDurationLeft);
            return Vector3.Distance(entity.transform.position, destination) <= radiusRequired
                || maxDurationLeft <= 0;
        }
    }
    
    protected class WaitState : State
    {
        StatefulCombatEntity entity;
        float waitTime;

        public WaitState(StatefulCombatEntity spawnedEnemy)
        {
            this.entity = spawnedEnemy;
            this.waitTime = UnityEngine.Random.Range(entity.minPauseDuration, entity.maxPauseDuration);
        }

        [ServerCallback]
        public override void Update() 
        {
            waitTime -= Time.deltaTime;
        }

        public override bool ReadyForExit()
        {
            return waitTime <= 0;
        }
    }

    protected class FleeState : State
    {
        public StatefulCombatEntity sce;
        public Entity fleeTarget;
        readonly float fleeSpeed;
        readonly float fleeDuration;

        
        readonly float enemyFarEnough = 16.0F; // how far the entity wants to be from an enemy, ideally

        float timeRemaining;

        public FleeState(StatefulCombatEntity sce, Entity fleeTarget, float fleeSpeed, float fleeDuration)
        {
            this.sce = sce;
            this.fleeTarget = fleeTarget;
            this.fleeSpeed = fleeSpeed;
            this.fleeDuration = fleeDuration;

            timeRemaining = fleeDuration;
        }

        public float GetTimeRemaining()
        {
            return timeRemaining;
        }

        public override void OnEnter()
        {
        }

        public override void Update()
        {
            if (fleeTarget == null)
            {
                sce.ClearDestination();
                return;
            }

            Vector3 awayFromTarget = (sce.transform.position - fleeTarget.transform.position).normalized;
            Vector3 desiredPostion = sce.transform.position + (fleeDuration * fleeSpeed * awayFromTarget);
            
            sce.SetDestination(desiredPostion, fleeSpeed);

            timeRemaining -= Time.deltaTime;
        }

        public override void OnExit()
        {
            sce.ClearDestination();
        }

        public override bool ReadyForExit()
        {
            return fleeTarget == null 
                || Vector3.Distance(fleeTarget.transform.position, sce.transform.position) >= enemyFarEnough 
                || timeRemaining <= 0;
        }

    }

    protected class KeepDistanceState : State
    {
        public StatefulCombatEntity sce;
        public Entity target;
        public float minDistance;
        public float maxDistance;
        

        public KeepDistanceState(StatefulCombatEntity sce, Entity target, float minDistance, float maxDistance) : base()
        {
            this.sce = sce;
            this.target = target;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
        }

        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void Update()
        {
            base.Update();

            if (target == null)
            {
                sce.stateMachine.ChangeState(new WaitState(sce));
            }

            float distance = Vector3.Distance(target.transform.position, sce.transform.position);
            if (distance > maxDistance)
            {
                sce.navMeshAgent.SetDestination(target.transform.position);
            }
            else if (distance <= minDistance)
            {
                Vector3 awayFromTarget = (sce.transform.position - target.transform.position).normalized;
                Vector3 desiredPostion = sce.transform.position + awayFromTarget * 5.0F;
                sce.SetDestination(desiredPostion);
            }
            else
            {
                sce.ClearDestination();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override bool ReadyForExit()
        {
            return target == null;
        }
    }
    
    #endregion
}

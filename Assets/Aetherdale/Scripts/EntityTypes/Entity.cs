using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.VFX;
using FMODUnity;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

// Contains basic universal stats for all combat-respecting living things


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkIdentity))]
public abstract class Entity : NetworkBehaviour, Damageable
{
    public const float MELEE_HEALTH_ORBS_ON_KILL_MULT = 2.5F;

    public const float UNIVERSAL_STAGGER_DURATION = 0.4F;

    const float ARMOR_50_PERCENT_DR = 200; // this much armor provides 50% damage reduction


    const float BASE_HEALTH_REGEN = 0.25F;
    
    public const float BASE_CRIT_MULT = 2.0F;
    public const float BASE_CRIT_CHANCE = 0;

    public const float BASE_STATUS_CHANCE = 12.5F;

    public const float THREAT_DECAY_PER_SECOND = 1.0F;

    public const float CORPSE_LIFESPAN = 60.0F;

    
    public const float AIM_MOVESPEED_MULT = 0.35F;

    
    // Data about this entity
    [SerializeField] string entityName;
    [SerializeField] string entityTitle;
    [SerializeField] protected int baseHealth;
    [SerializeField] int baseArmor;
    [SerializeField] float baseEnergy = 100;
    #if UNITY_EDITOR
    public int effectiveHealth = 0;
    #endif
    public Transform floatingHealthBarTransform;
    public EventReference idleSound;
    [SerializeField] Faction faction;
    [SerializeField] DropTable dropTable;
    [SerializeField] int dropTableRolls = 1;
    [SerializeField] List<EntityType> types; 
    [SerializeField] bool staggerable = true;
    [SerializeField] int impactResistance = 50;
    [SerializeField] bool canBePushed = true;
    [SerializeField] bool countedBySpawners = true;
    
    [SerializeField] protected CameraContext cameraContext;
    [SerializeField] Transform sightPoint;
    public Transform ephemeraParentTransform;


    [SerializeField] int damagedSoundThreshold = 5;
    public EventReference damagedSound;
    [SerializeField] EventReference footstepsSound;

    public readonly SyncDictionary<string, float> stats = new();
    List<StatChange> postTraitStatChanges = new();


    float energyRegenPerSecond = 2.0F;

    [SyncVar] public bool grounded;

    bool animatorApplyRootMotion = false;
    protected Animator animator;
    protected Rigidbody body;
    ParticleSystem stunParticles;

    LayerMask ignoreInSightCalculation;

    public readonly StateMachine stateMachine = new();
    protected List<EffectInstance> activeEffectInstances = new();

    public readonly SyncDictionary<string, int> activeEffectIDs = new();


    public TraitList traitList = new();

    [SyncVar] protected int currentImpactResistance;

    readonly float visionRadius = 90.0F;

    // currently set difficulty multiplier
    public int entityLevel = 1;

    [SyncVar] public bool attacking = false;
    protected float lastAttack = -10.0F;
    protected float lastDodge = -10.0F;
    protected float lastDodgeAttack = -10.0F;
    [SyncVar] protected bool beingPushed = false;

    Dictionary<Entity, int> damagers = new();
    protected Dictionary<Entity, float> threats = new();

    Dictionary<Status, VisualEffect> statusVisuals = new();

    [SyncVar (hook = nameof(OwningPlayerChanged))] protected Player owningPlayer;
    [SyncVar] public bool airborne = false;
    [SyncVar] public bool isDead = false;
    [SyncVar] public bool resurrecting = false;
    [SyncVar] protected bool stunned = false;
    [SyncVar] float stunEndTime;
    [SyncVar] protected bool invulnerable;
    [SyncVar] protected bool invisible;
    [SyncVar] protected bool aiming;

    [SyncVar] protected bool channeling = false;

    [SyncVar] public Entity currentTarget;

    

    Vector3 currentPushVector;
    float currentPushSpeed;

    public List<GameObject> velocitySources = new();

    Dictionary<string, List<VisualEffect>> appliedEffectVisuals = new();

    protected float lastStagger = 0;

    [SyncVar] public bool createsCorpse = true;


    [SyncVar] public bool inJumpAttack = false;

    protected float lastAttackModeActionTime = 0.0F;
    [ClientRpc] public void RpcSetAttackMode() { lastAttackModeActionTime = Time.time; }

    Vector3 originalScale;

    public enum MovementMode
    {
        NavMeshAgent,
        Rigidbody,
        PlayerControlled,
        None,
    }

    protected MovementMode movementMode;

    protected MovementMode defaultMovementMode;


    protected NavMeshAgent navMeshAgent;

    protected VisualEffect footstepVFX;

    protected float lastDamaged = 0;



    [SyncVar] Entity killer;



    public Action OnDeathAnimationComplete;

    public Action<HitInfo> OnKillEntity;
    public Action<HitInfo> OnHitEntity;
    public Action<HitInfo> OnDamaged;
    public Action<Entity, Entity> OnDeath;
    public static Action<Entity, Entity> OnEntityDied; // Invoked when ANY entity dies

    public Action<Entity, Entity> OnTransformed;
    public Action OnNextGrounded;
    public Action<string, float> OnStatChanged;
    public Action OnEffectsChanged;
    public Action<EffectInstance> OnEffectAdded;
    public Action<EffectInstance> OnEffectRemoved;


    [SerializeField] public List<RendererMaterialColor> defaultMaterialsColors;

    #if UNITY_EDITOR
    [SerializeField] bool clickToRefreshDMC = false;
    protected override void OnValidate()
    {
        base.OnValidate();

        float baseArmorMult = ARMOR_50_PERCENT_DR / (ARMOR_50_PERCENT_DR + baseArmor);

        effectiveHealth = (int) (baseHealth / baseArmorMult);

        if (impactResistance == 0)
        {
            Debug.LogError("Entity prefab " + GetName() + " has zero impact resistance");
        }

        if (clickToRefreshDMC)
        {
            SaveDefaultMaterialsColors();
            clickToRefreshDMC = false;
        }     
    }
    #endif

    public virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            if (isOwned)
            {
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                body.interpolation = RigidbodyInterpolation.None;
            }
        }

        animator = GetComponent<Animator>();
        animatorApplyRootMotion = animator.applyRootMotion;
    }

    // Call base.Start() in all overrides!
    public virtual void Start()
    {
        Collider coll = GetComponent<Collider>();

        stunParticles = Instantiate(VisualEffectIndex.GetDefaultEffectIndex().stunnedParticles, transform);
        stunParticles.transform.position = coll == null ? transform.position + new Vector3(0, 1, 0) : GetComponent<Collider>().bounds.center + new Vector3(0, GetComponent<Collider>().bounds.extents.y, 0);
        if (!stunParticles.isStopped)
        {
            stunParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        GameObject newEphemeraParent = new GameObject("Ephemera Parent Transform");
        newEphemeraParent.transform.SetParent(ephemeraParentTransform);
        newEphemeraParent.transform.SetLocalPositionAndRotation(new(), new());

        ephemeraParentTransform = newEphemeraParent.transform;

        SceneManager.activeSceneChanged += OnSceneChanged;

        originalScale = transform.localScale;

        // Don't want entities in collisions when doing rigidbody movement
        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.excludeLayers = LayerMask.GetMask("Entities");
        }

        footstepVFX=Instantiate(AetherdaleData.GetAetherdaleData().vfxData.footstepsVFXPrefab, transform);
        footstepVFX.transform.position += Vector3.up * 0.1F;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (faction == null)
        {
            faction = WorldManager.GetWorldManager().GetDefaultFaction();
        }
        
        if (GetStat(Stats.MaxHealth, 0) == 0) // max health may have been set by difficulty, before Start was called
        {
            SetStat(Stats.MaxHealth, baseHealth);
            SetStat(Stats.CurrentHealth, GetStat(Stats.MaxHealth));
        }

        SetDefaultStats();

        currentImpactResistance = impactResistance;
        ignoreInSightCalculation = LayerMask.GetMask("Entities", "Loot", "Hitboxes", "Hurtboxes", "Flight Navmesh");

        WorldManager.RegisterEntity(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Set up stat SyncDict hooks
        stats.OnChange += StatSyncDictCallback;

        activeEffectIDs.OnChange += ActiveEffectIDsDictCallback;
    }

    public virtual void OnEnable()
    {
        if (stunParticles != null && !stunParticles.isStopped)
        {
            stunParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // Call base.Update() in all overrides!
    public virtual void Update()
    {
        if (isServer)
        {
            SetStat(Stats.CurrentHealth, GetStat(Stats.CurrentHealth) + GetStat(Stats.HealthRegen) * Time.deltaTime);
            if (GetStat(Stats.CurrentHealth) >= GetMaxHealth())
            {
                SetStat(Stats.CurrentHealth, GetMaxHealth());
            }
            else if (GetStat(Stats.CurrentHealth) <= 0)
            {
                Die();
            }

            foreach (Entity threatSource in threats.Keys.ToList())
            {
                threats[threatSource] -= THREAT_DECAY_PER_SECOND * Time.deltaTime;
            }
            
            bool previouslyGrounded = grounded;
            grounded = IsGrounded();
            if (grounded)
            {
                if (!previouslyGrounded)
                {
                    Grounded();
                }
                inJumpAttack = false;
            }

        }

        // Process hit scale manipulation
        if (transform.localScale != originalScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, 12.0F * Time.deltaTime);
        }

    }

    public virtual void FixedUpdate()
    {
        if (isServer)
        {
            // Process stuns
            if (stunned)
            {
                if (Time.time >= stunEndTime)
                {
                    ClearStun();
                }
            }

            // iterate backwards - effects may end while running through this loop
            for (int i = activeEffectInstances.Count - 1; i >= 0; i--)
            {
                activeEffectInstances[i].Process();
            }

            if (currentPushVector != null && currentPushVector.magnitude > 0)
            {
                Vector3 frameMove = Vector3.Lerp(Vector3.zero, currentPushVector, currentPushSpeed * Time.deltaTime);

                if (currentPushVector.magnitude < frameMove.magnitude)
                {
                    currentPushVector = Vector3.zero;
                }
                else
                {
                    currentPushVector -= frameMove;
                }

                Move(currentPushVector);
            }
        }
    }

    public virtual void LateUpdate()
    {
        if (Time.timeScale > 0)
        {
            Animate();
        }
    }

    public virtual void OnDestroy()
    {
        CancelInvoke();

        if (cameraContext != null)
        {
            Destroy(cameraContext.gameObject);
        }
        
        SceneManager.activeSceneChanged -= OnSceneChanged;
        WorldManager.UnregisterEntity(this);

        if (isServer)
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.TryGetComponent(out Projectile projectile))
                {
                    NetworkServer.UnSpawn(projectile.gameObject);
                    Destroy(projectile.gameObject);
                }
            } 
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
    }


    private void StatSyncDictCallback(SyncIDictionary<string, float>.Operation op, string statName, float value)
    {
        if (op is not SyncIDictionary<string, float>.Operation.OP_REMOVE && statName != null)
        {
            OnStatChanged?.Invoke(statName, value);
        }
    }

    
    protected abstract void Animate();

    void Grounded()
    {
        OnNextGrounded?.Invoke();

        OnNextGrounded = null;
    }


    public virtual void ProcessPeriodics()
    {
        if (isServer)
        {
            ProcessHealthRegen();
        }
    }

    public void ProcessHealthRegen()
    {
        if (gameObject.activeSelf)
        {
            // regular regen
            SetStat(Stats.CurrentHealth, GetStat(Stats.CurrentHealth) + GetStat(Stats.HealthRegen)* Time.deltaTime);
        }
        else
        { 
            // inactive regen
            SetStat(Stats.CurrentHealth, GetStat(Stats.CurrentHealth) + GetStat(Stats.InactiveFormHealthRegen, GetStat(Stats.HealthRegen)) * Time.deltaTime);
        }
    }
    

#region Stats And Traits
    public virtual void SetDefaultStats()
    {
        float healthRatio = GetHealthRatio();

        float energyRatio = GetEnergyRatio();

        stats.Clear();
        
        AdjustMaxHealth(baseHealth, healthRatio);

        AdjustMaxEnergy((int) baseEnergy, energyRatio);

        if (GetStat(Stats.MaxArmor, 0) == 0)
        {
            SetStat(Stats.MaxArmor, baseArmor);
            SetStat(Stats.CurrentArmor, baseArmor);
        }

        SetStat(Stats.EnergyRegen, energyRegenPerSecond);

        SetStat(Stats.AttackDamageMultiplier, 1);
        SetStat(Stats.AbilityStrength, 1);
        SetStat(Stats.AbilityCooldownRatio, 1);
        SetStat(Stats.AbsorbChance, 0);
        SetStat(Stats.AOERadiusMultiplier, 1);
        SetStat(Stats.AttackSpeed, 1);
        SetStat(Stats.CriticalChance, BASE_CRIT_CHANCE);
        SetStat(Stats.GlobalDamageMultiplier, 1);
        SetStat(Stats.DamageTakenMultiplier, 1);
        SetStat(Stats.DroppedLootMult, 1.0F);
        SetStat(Stats.ExperienceRewardedMultiplier, 1.0F);
        SetStat(Stats.CriticalDamageMultiplier, BASE_CRIT_MULT);
        SetStat(Stats.HealthRegen, BASE_HEALTH_REGEN);
        SetStat(Stats.MovementSpeedMult, 1.0F);
        SetStat(Stats.ElementalStatusChance, BASE_STATUS_CHANCE);

        AdjustStatsForLevel();
    }
    
    public float GetStat(string name, float notFoundValue = 0.0F)
    {
        if (!stats.ContainsKey(name))
        {
            return notFoundValue;
        }

        return stats[name];
    }

    public void SetStat(string name, float value)
    {
        if (name == null || name == "")
        {
            return;
        }
        
        OnStatChanged?.Invoke(name, value);

        //Debug.Log("Setting stat " + name + " to " + value);
        if (!stats.ContainsKey(name))
        {
            stats.Add(name, value);
        }
        else
        {
            stats[name] = value;
        }
    }

    public void ModStat(string name, float change, float notFoundValue = 0)
    {
        //Debug.Log("modding stat " + name + " to " + value);
        if (!stats.ContainsKey(name))
        {
            stats.Add(name, notFoundValue);
        }
            
        stats[name] = stats[name] + change;

        OnStatChanged?.Invoke(name, stats[name]);
    }


    [Server]
    public void EvaluateStats()
    {
        // Reset stats to original value to ensure accuracy
        SetDefaultStats();
        
        foreach (StatChange statChange in traitStatChanges)
        {
            ApplyStatChange(statChange);
        }

        foreach (StatChange statChange in postTraitStatChanges)
        {
            ApplyStatChange(statChange);
        }

    }

    [Server]
    void ApplyStatChange(StatChange statChange)
    {
        switch (statChange.stat)
        {
            case Stats.MaxHealth:
                float portionOfHealth = GetHealthRatio();
                
                SetStat(Stats.MaxHealth, GetStat(Stats.MaxHealth) + (int) statChange.GetFinalChangeAmount(GetStat(Stats.MaxHealth)));

                SetStat(Stats.CurrentHealth, portionOfHealth * GetStat(Stats.MaxHealth));

                break;

            case Stats.MaxArmor:
                float portionOfArmor = GetPercentArmor();
                
                SetStat(Stats.MaxArmor, GetStat(Stats.MaxArmor) + (int) statChange.GetFinalChangeAmount(GetStat(Stats.MaxArmor)));

                SetStat(Stats.CurrentArmor, portionOfArmor * GetStat(Stats.MaxArmor));
                break;

            default:
                float originalStatValue = GetStat(statChange.stat);
                float change = statChange.GetFinalChangeAmount(originalStatValue);

                SetStat(statChange.stat, originalStatValue + change);

                break;
        }
    }

    
    [Server]
    protected void AdjustMaxHealth (int newValue, float customRatio = -1)
    {
        float portion;
        if (customRatio < -1)
        {
            portion = GetHealthRatio();
        }
        else
        {
            portion = customRatio;
        }

        SetStat(Stats.MaxHealth, newValue);

        SetStat(Stats.CurrentHealth, portion * GetStat(Stats.MaxHealth));
    }

    [Server]
    protected void AdjustMaxEnergy (int newValue, float customNewRatio = -1)
    {
        float portion;
        if (customNewRatio < -1)
        {
            portion = GetEnergyRatio();
        }
        else
        {
            portion = customNewRatio;
        }

        SetStat(Stats.MaxEnergy, newValue);

        SetStat(Stats.CurrentEnergy, portion * GetStat(Stats.MaxEnergy));
    }

    [Server]
    public virtual void AdjustStatsForLevel()
    {
        // Preserve ratios to scale out our current alongside max
        float healthPerc = GetHealthRatio();
        float healthMult = Equation.ENTITY_HEALTH_SCALING.Calculate(this.entityLevel);
        if (this is Boss)
        {
            healthMult = Equation.BOSS_HEALTH_SCALING.Calculate(this.entityLevel);
        }
        
        SetStat(Stats.MaxHealth, (int) (baseHealth * healthMult)); //maxHealth = (int) (baseHealth * healthMult);
        SetStat(Stats.CurrentHealth, (int) (GetStat(Stats.MaxHealth) * healthPerc)); //currentHealth = (int) (maxHealth * healthPerc);

        float armorPerc = GetPercentArmor();
        SetStat(Stats.MaxArmor, baseArmor);
        SetStat(Stats.CurrentArmor, (int) (GetStat(Stats.MaxArmor) * armorPerc));
    }

    
    [Server]
    public void SetHealthRatio(float ratio)
    {
        SetStat(Stats.CurrentHealth, ratio * GetStat(Stats.MaxHealth));
    }


    protected List<StatChange> traitStatChanges = new();

    public virtual TraitList GetTraits()
    {
        if (GetOwningPlayer() != null)
        {
            return GetOwningPlayer().GetTraits();
        }
        return traitList;
    }

    [Server]
    public void AddPostTraitStatChange(StatChange statChange)
    {
        postTraitStatChanges.Add(statChange);
        
        EvaluateStats();
    }

    [Server]
    public void ApplyTraits()
    {
        traitStatChanges.Clear();

        OnKillEntity = null;
        OnHitEntity = null;

        foreach (Trait trait in GetTraits())
        {
            ApplyTrait(trait, false);
        }

        EvaluateStats();
    }

    void ApplyTrait(Trait trait, bool evaluate = true)
    {
        //OnHitEntity += trait.OnHit; // OnHit managed manually for traits now
        //OnKillEntity += trait.OnKill; // OnKill managed manually for traits now
        
        foreach (StatChange statChange in trait.GetStatChanges())
        {
            traitStatChanges.Add(statChange);
        }
    }

    [Server]
    public void RemovePostTraitStatChange(StatChange statChange)
    {
        postTraitStatChanges.Remove(statChange);
        
        EvaluateStats();
    }

     public int GetMaxHealth()
    {
        return (int) GetStat(Stats.MaxHealth);
    }

    public int GetCurrentHealth()
    {
        return (int) GetStat(Stats.CurrentHealth);
    }

    public int GetCurrentArmor()
    {
        return (int) GetStat(Stats.CurrentArmor);
    }

    // returns proportion of max health remaining, e.g. 0.72 = 72%
    public float GetHealthRatio()
    {
        if (GetStat(Stats.CurrentHealth) == 0)
        {
            return GetStat(Stats.MaxHealth) == 0 ? 1.0F : 0.0F;
        }
        
        return GetCurrentHealth() / (float) GetMaxHealth();
    }

    public float GetEnergyRatio()
    {
        if (GetStat(Stats.CurrentEnergy) == 0)
        {
            return GetStat(Stats.MaxEnergy) == 0 ? 1.0F : 0.0F;
        }
        
        return GetStat(Stats.CurrentEnergy) / (float) GetStat(Stats.MaxEnergy);
    }

    // returns proportion of max armor remaining, e.g. 1.00 = 100%
    public float GetPercentArmor()
    {
        if (GetStat(Stats.CurrentArmor) <= 0)
        {
            return GetStat(Stats.MaxArmor) == 0 ? 1.0F : 0.0F;
        }

        return GetCurrentArmor() / GetStat(Stats.MaxArmor);
    }

    // returns the effective maximum health of this entity (total pre-armor damage required to kill it)
    public float GetEffectiveMaxHealth()
    {
        return GetStat(Stats.MaxHealth) / GetArmorMultiplier();
    }

    public float ActualToEffectiveHealth(float actual)
    {
        return actual / GetArmorMultiplier();
    }

    public virtual float GetGlobalDamageMultiplier()
    {
        return Equation.ENEMY_GLOBAL_DAMAGE_MULTIPLIER.Calculate(entityLevel);
    }

    public float GetDamageTakenMultiplier()
    {
        float mult = 1.0F;
        mult *= GetStat(Stats.DamageTakenMultiplier);
        return mult;
    }

    public float GetArmorMultiplier()
    {
        return ARMOR_50_PERCENT_DR / (ARMOR_50_PERCENT_DR + GetStat(Stats.CurrentArmor));
    }

#endregion


#region Effects
    public bool HasEffectOfType(Type type)
    {
        foreach(EffectInstance instance in activeEffectInstances)
        {
            Debug.Log(instance);
            if (instance.effect.GetType().IsAssignableFrom(type))
            {
                return true;
            }
        }

        return false;
    }

    [Server]
    public EffectInstance AddEffect(Effect effect, Entity origin, int numStacks = 1)
    {
        EffectInstance existingInstance = activeEffectInstances.Find(x => x.effect == effect);

        if (existingInstance != null)
        {
            if (existingInstance.GetNumberOfStacks() >= effect.GetMaxStacks())
            {
                return existingInstance;
            }

            existingInstance.AddStacks(numStacks);

            EffectInstancesUpated(existingInstance);

            return existingInstance;
        }
        else
        {
            // Brand new effect
            EffectInstance newInstance = effect.CreateEffectInstance(origin);
            newInstance.Attach(this);
            newInstance.EffectStart();

            newInstance.SetStacks(numStacks);

            newInstance.OnStackChange += EffectInstancesUpated;

            activeEffectInstances.Add(newInstance);

            OnEffectAdded?.Invoke(newInstance);

            EffectInstancesUpated(newInstance);

            RpcEffectStart(effect.effectID);

            return newInstance;
        }
    }

    void EffectInstancesUpated(EffectInstance instance)
    {
        activeEffectIDs[instance.effect.effectID] = instance.GetNumberOfStacks();
    }

    [ClientRpc]
    void RpcEffectStart(string effectID)
    {
        Effect effect = Effect.GetEffect(effectID);

        VisualEffect visualEffectPrefab = effect.GetVisualEffectApplied();
        if (visualEffectPrefab != null)
        {
            if (!appliedEffectVisuals.ContainsKey(effectID))
            {
                appliedEffectVisuals.Add(effectID, new());
            }

            List<VisualEffect> createdInstances = AttachVisualEffect(visualEffectPrefab);

            foreach (VisualEffect visualEffect in createdInstances)
            {
                appliedEffectVisuals[effectID].Add(visualEffect);
            }
        }
    }

    public List<VisualEffect> AttachVisualEffect(VisualEffect vfxPrefab)
    {
        List<VisualEffect> createdInstances = new();
        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>();

        // Set the rate of each visual effect according to the total number so we don't go nuts when an entity has a ton of SMRs
        float rate = vfxPrefab.GetFloat("Rate");
        rate /= smrs.Count();

        foreach (SkinnedMeshRenderer smr in smrs)
        {
            VisualEffect visEffInst = GameObject.Instantiate(vfxPrefab);
            visEffInst.transform.parent = transform;
            visEffInst.transform.SetLocalPositionAndRotation(new(), new());

            visEffInst.SetFloat("Rate", rate);

            visEffInst.SetSkinnedMeshRenderer("SkinnedMeshRenderer", smr);

            createdInstances.Add(visEffInst);
        }

        return createdInstances;
    }

    [Server]
    public void RemoveEffect(Effect effect)
    {
        EffectInstance instance = GetEffectInstance(effect);

        if (instance != null)
        {
            activeEffectInstances.Remove(instance);
            activeEffectIDs.Remove(instance.effect.effectID);

            instance.EffectEnd();

            instance.OnStackChange -= EffectInstancesUpated;

            OnEffectRemoved?.Invoke(instance);
        }


        RpcEffectEnd(effect.effectID);
    }

    [Server]
    public void RemoveStackOfEffect(Effect effect)
    {
        EffectInstance instance = GetEffectInstance(effect);

        if (instance != null)
        {
            instance.RemoveStack();
        }
    }

    [ClientRpc]
    void RpcEffectEnd(string effectID)
    {
        if (appliedEffectVisuals.ContainsKey(effectID))
        {
            foreach (VisualEffect visualEffect in appliedEffectVisuals[effectID])
            {
                // TODO stop gracefully
                GameObject.Destroy(visualEffect.gameObject);
            }

            appliedEffectVisuals.Remove(effectID);
        }
    }

    [Server]
    public EffectInstance GetEffectInstance(Effect effect)
    {
        foreach(EffectInstance effectInstance in activeEffectInstances)
        {
            if(effectInstance.effect == effect)
            {
                return effectInstance;
            }
        }

        return null;
    }

    public List<EffectInstance> GetActiveEffects()
    {
        return activeEffectInstances;
    }


    [Server]
    public void TransferEffects(Entity newTarget)
    {
        for(int i = activeEffectInstances.Count() - 1; i >= 0; i--)
        {
            //newTarget.activeEffectInstances.Add(activeEffectInstances[i]);
            newTarget.AddEffect(activeEffectInstances[i].effect, activeEffectInstances[i].origin, activeEffectInstances[i].GetNumberOfStacks());

            RemoveEffect(activeEffectInstances[i].effect);
        }
    }

    private void ActiveEffectIDsDictCallback(SyncIDictionary<string, int>.Operation operation, string effectID, int stacks)
    {
        OnEffectsChanged?.Invoke();
    }

#endregion
    
#region Movement

    public void SetMovementMode(MovementMode newMovementMode)
    {
        movementMode = newMovementMode;

        
        switch (movementMode)
        {
            case MovementMode.NavMeshAgent:
                SwitchMovementToNavMeshAgent();
                break;
            case MovementMode.PlayerControlled:
                SwitchMovementToPlayerControlled();
                break;
            case MovementMode.Rigidbody:
                SwitchMovementToRigidbody();
                break;
            case MovementMode.None:
                SwitchMovementToNone();
                break;
        }
    }

    public void SwitchMovementToNone()
    {
        movementMode = MovementMode.None;

        if (navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.enabled = false;
        }

        body.angularVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;

        body.isKinematic = true;
        body.useGravity = false;

        animator.applyRootMotion = false;
    }

    public void SwitchMovementToRigidbody(Vector3 initialVelocity = new())
    {
        movementMode = MovementMode.Rigidbody;
        if (navMeshAgent != null)
        {
            navMeshAgent.ResetPath();
            navMeshAgent.enabled = false;
        }

        body.isKinematic = false;
        body.useGravity = true;

        body.angularVelocity = Vector3.zero;
        body.linearVelocity = initialVelocity;

        animator.applyRootMotion = false;
    }

    public void SwitchMovementToRigidBodyUntilGrounded(Vector3 initialVelocity = new(), float timeout = 10)
    {
        StartCoroutine(SwitchMovementToRigidBodyUntilGroundedCoroutine(initialVelocity, timeout));
    }

    IEnumerator SwitchMovementToRigidBodyUntilGroundedCoroutine(Vector3 initialVelocity, float timeout)
    {
        SwitchMovementToRigidbody(initialVelocity);

        float timeoutRemaining = timeout;
        while (!grounded && timeoutRemaining > 0)
        {
            timeoutRemaining -= Time.deltaTime;
            yield return null;
        }

        SwitchMovementToDefault();
    }

    public void SwitchMovementToNavMeshAgent()
    {
        movementMode = MovementMode.NavMeshAgent;
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        navMeshAgent.Warp(transform.position);
        navMeshAgent.enabled = true;

        animator.applyRootMotion = animatorApplyRootMotion;
    }

    public void SwitchMovementToPlayerControlled()
    {
        movementMode = MovementMode.PlayerControlled;
        if (navMeshAgent != null)
        {
            navMeshAgent.enabled = false;
        }

        body.isKinematic = false;
        body.useGravity = false;

        body.angularVelocity = Vector3.zero;
        body.linearVelocity = Vector3.zero;
    }

    public void SwitchMovementToDefault()
    {
        SetMovementMode(defaultMovementMode);
    }


    [TargetRpc]
    public void TargetMove(Vector3 magnitude)
    {
        Move(magnitude);
    }

    public abstract void Move(Vector3 magnitude);

    public abstract Vector3 GetVelocity();

    public virtual void AddVelocity(Vector3 velocity)
    {
        
    }

    protected bool ChangePosition(Vector3 newPosition)
    {
        transform.position = newPosition;

        RpcChangedPosition(newPosition);

        return true;
    }

    [ClientRpc]
    void RpcChangedPosition(Vector3 newPosition)
    {
        transform.position = newPosition;

        if (navMeshAgent != null)
        {
            navMeshAgent.nextPosition = newPosition;
            navMeshAgent.Warp(newPosition);
        }
    }

    public virtual bool TeleportToRandomNavMeshPosition(float minDistance, float maxDistance)
    {
        float radiusDifference = maxDistance - minDistance;
        Vector2 position = UnityEngine.Random.insideUnitCircle * radiusDifference;

        // Scale it out by adding min radius to its offset
        Vector2 offset = position.normalized * minDistance;

        Vector2 finalXY = position + offset;

        Vector3 destination;
        if (NavMesh.SamplePosition(transform.position + new Vector3(finalXY.x, 0, finalXY.y), out NavMeshHit hit, 100.0F, NavMesh.AllAreas))
        {
            destination = hit.position;
        }
        else
        {
            return false;
        }

        ChangePosition(destination);
        return true;
    }

#endregion


    public virtual void ActivateRig(string name)
    {

    }

    public virtual void DeactivateRig(string name)
    {
        
    }
    
    public virtual void OwningPlayerChanged(Player oldPlayer, Player newPlayer)
    {

    }

    [Server]
    public void SetFaction(Faction faction)
    {
        this.faction = faction;
    }

    [Server]
    public void SetLevel(int entityLevel)
    {
        this.entityLevel = entityLevel;

        // Update health
        AdjustStatsForLevel();

    }

    public int GetLevel()
    {
        return entityLevel;
    }

    public virtual bool HasSecondaryResource()
    {
        return false;
    }
    
    public virtual Color GetSecondaryResourceColor()
    {
        return Color.grey;
    }

    /// <summary>
    /// Angle around Y to face the target
    /// </summary>
    /// <param name="otherObject"></param>
    /// <returns>Positive angle if to "right", negative angle if to "left"</returns>
    public float GetRelativeBearingAngle(GameObject otherObject, bool relativeToLookingAngle = false) => GetRelativeBearingAngle(otherObject.transform.position, relativeToLookingAngle);
    public float GetRelativeBearingAngle(Vector3 position, bool relativeToLookingAngle = false)
    {
        if (relativeToLookingAngle)
        {
            return GetCameraContext().gameObject.GetRelativeBearingAngle(position);
        }

        return gameObject.GetRelativeBearingAngle(position);
    }

    public float GetRelativePitchAngle(GameObject otherObject, bool relativeToLookingAngle = false) => GetRelativePitchAngle(otherObject.transform.position, relativeToLookingAngle);
    public float GetRelativePitchAngle(Vector3 position, bool relativeToLookingAngle = false)
    {
        if (relativeToLookingAngle)
        {
            return GetCameraContext().gameObject.GetRelativePitchAngle(position);
        }

        return gameObject.GetRelativePitchAngle(position);
    }

    // Returns amount rotated around Y
    public float TurnTowards(GameObject otherObject, float rotationSpeed, float predictiveOvershootStrength = 0F)
    {
        Vector3 overshoot = Vector3.zero;
        if (otherObject.TryGetComponent(out Entity target))
        {
            overshoot = target.GetVelocity() * predictiveOvershootStrength;
        }

        return TurnTowards(otherObject.transform.position + overshoot, rotationSpeed);
    }

    // Returns amount rotated around Y
    public virtual  float TurnTowards(Vector3 position, float rotationSpeed)
    {
        Vector3 lookDirection = (position - transform.position).normalized;
        lookDirection.y = 0;

        if (lookDirection == Vector3.zero)
        {
            return 0.0F;
        }

        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        Quaternion newRotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        float difference = (newRotation * Quaternion.Inverse(transform.rotation)).y;

        transform.rotation = newRotation;
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

        return difference;
    }


    /// <summary>
    /// Push from an external source - respects whether this entity can be pushed
    /// </summary>
    /// <param name="push"></param>
    /// <param name="endAction"></param>
    public void Push(Vector3 push, Action endAction = null, ForceMode forceMode = ForceMode.Force)
    {
        if (gameObject.activeSelf && canBePushed && !isDead)
        {
            StartCoroutine(ApplyPush(push, endAction, forceMode));
        }
    }

    /// <summary>
    /// Push from self - not blocked by canBePushed flag
    /// </summary>
    /// <param name="force"></param>
    /// <param name="endAction"></param>
    protected void PushSelf(Vector3 force, Action endAction = null, ForceMode forceMode = ForceMode.Force)
    {
        if (gameObject.activeSelf && !isDead)
        {
            StartCoroutine(ApplyPush(force, endAction, forceMode));
        }
    }

    IEnumerator ApplyPush(Vector3 force, Action endAction = null, ForceMode forceMode = ForceMode.Force)
    {
        Debug.Log("Start pushing " + this + " (" + forceMode + ")");
        beingPushed = true;

        SwitchMovementToRigidbody();

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // while (true)
        // {
        //     body.linearVelocity = force;
        //     Debug.Log(body.linearVelocity);
        //     yield return null;
        // }
        body.AddForce(force, forceMode);

        yield return new WaitForFixedUpdate();

        float startTime = Time.time;
        float timeAtZeroVelocity = 0;

        float groundedScore = 0; // time grounded / sqrt of velocity while grounded, totalled over multiple frames
        float GROUNDED_SCORE_REQUIRED = 0.5F;
        while ((body.linearVelocity.magnitude >= 0.1F && timeAtZeroVelocity < 0.1F) || groundedScore >= GROUNDED_SCORE_REQUIRED)
        {
            // Break if grounded with low enough velocity
            if (grounded)
            {
                if (body.linearVelocity.magnitude <= 0.5F * force.magnitude )
                {
                    break; 
                }

                groundedScore += Time.deltaTime / Mathf.Sqrt(body.linearVelocity.magnitude);
            }

            if (body.linearVelocity.magnitude <= 0.1F)
            {
                timeAtZeroVelocity += Time.deltaTime;
            }
            else
            {
                timeAtZeroVelocity = 0;
            }

            yield return null;
        }
        
        SwitchMovementToDefault();

        beingPushed = false;

        Debug.Log("Push end");

        endAction?.Invoke();
    }

    /// <summary>
    /// Whether <paramref name="distanceOvershot"/> past <paramref name="target"/> from me is over ground (can we charge+overshoot the target without a long drop)
    /// </summary>
    /// <param name="target"></param>
    /// <param name="distanceOvershot"></param>
    /// <returns></returns>
    public bool IsOvershootGrounded(Entity target, float distanceOvershot)
    {
        Vector3 offset = target.transform.position - transform.position;
        Vector3 overshotPosition = transform.position + offset + (offset.normalized * distanceOvershot);

        return Physics.Raycast(overshotPosition + Vector3.up * 1, Vector3.down, 10.0F, LayerMask.GetMask("Default"));
    }

    
    public void SlideToPositionWithSpeed(Vector3 targetPos, float speed, Action endAction = null)
    {
        float distance = Vector3.Distance(targetPos, transform.position);
        float duration = distance / speed;

        SlideToPosition(targetPos, duration, endAction);
    }
    public void SlideToPosition(Vector3 targetPos, float duration, Action endAction = null)
    {
        StartCoroutine(SlideToPositionCoroutine(targetPos, duration, endAction));
    }

    IEnumerator SlideToPositionCoroutine(Vector3 targetPos, float duration, Action endAction = null, float maxTime = 3.0F)
    {
        SwitchMovementToRigidbody();
        
        transform.LookAt(targetPos, Vector3.up);

        Debug.Log("Target pos is " + targetPos);

        body.useGravity = false;
        body.detectCollisions = false;

        Vector3 direction = (targetPos - transform.position).normalized;
        float durationRemaining = duration;
        float distance = Vector3.Distance(targetPos, transform.position);
        float speed = distance / duration;

        Vector3 initialTargetVelocity = direction * (distance / duration);

        float startTime = Time.time;

        // TODO this is overshooting our destination
        while (Vector3.Distance(targetPos, transform.position) > speed * Time.fixedDeltaTime * 2 && (Time.time - startTime) < maxTime)
        {
            Vector3 velocity = (targetPos - transform.position).normalized * speed;
            body.linearVelocity = velocity;

            yield return null;
        }

        body.linearVelocity = Vector3.zero;

        body.useGravity = true;
        body.detectCollisions = true;
        Debug.Log("Actual position at end is " + transform.position);

        SwitchMovementToDefault();

        endAction?.Invoke();
    }


    public virtual bool CanMove()
    {
        return !stunned && !animator.GetBool("staggered") && Time.time - lastStagger > UNIVERSAL_STAGGER_DURATION;
    }

    public virtual bool CanTurn()
    {
        return !stunned && !animator.GetBool("staggered");
    }

    
    List<RaycastHit> GetGroundHits()
    {
        List<RaycastHit> hits = new();

        Collider collider = GetComponent<Collider>();
        Vector3 bottom = collider.bounds.center - new Vector3(0, collider.bounds.extents.y, 0) + new Vector3(0, 0.1F, 0);

        // Check in a + pattern
        List<Vector3> groundCheckPoints = new()
        {
            bottom,
            bottom + new Vector3(collider.bounds.extents.x, 0, 0),
            bottom + new Vector3(-collider.bounds.extents.x, 0, 0),
            bottom + new Vector3(0, 0, collider.bounds.extents.z),
            bottom + new Vector3(0, 0, -collider.bounds.extents.z),
        };

        foreach (Vector3 point in groundCheckPoints)
        {
            if (Physics.Raycast(point, Vector3.down, out RaycastHit hit, 0.3F, LayerMask.GetMask("Default")))
            {
                hits.Add(hit);
            }
        }

        return hits;
    }

    bool IsGrounded()
    {
        Collider collider = GetComponent<Collider>();

        if (collider == null)
        {
            return false;
        }
        
        Vector3 bottom = collider.bounds.center - new Vector3(0, collider.bounds.extents.y, 0) + new Vector3(0, 0.1F, 0);

        return GetGroundHits().Count() > 0;
    }

    public Vector3 GetGroundNormal()
    {
        Vector3 total = new();

        foreach (RaycastHit hit in GetGroundHits())
        {
            total += hit.normal;
        }

        return total.normalized;
    }


    [Server]
    public virtual void Stun(float duration)
    {
        if (!stunned)
        {
            Stagger();
            RpcPlayStunParticles();
        }

        stunned = true;
        float endTime = Time.time + duration;
        if (endTime > stunEndTime)
        {
            stunEndTime = endTime;
        }
    }

    public Player GetOwningPlayer()
    {
        return owningPlayer;
    }

    [Server]
    public virtual bool SetOwningPlayer(Player player)
    {
        if (owningPlayer != null)
        {
            return false;
        }

        owningPlayer = player;

        gameObject.name = gameObject.name + " - " + player.GetPlayerId();

        return true;
    }

    [Server]
    public void ClearOwningPlayer()
    {
        owningPlayer = null;

        RpcSetOwningPlayer(null);
    }

    [ClientRpc]
    public void RpcSetOwningPlayer(Player player)
    {
        owningPlayer = player;
    }


    [ClientRpc]
    void RpcPlayStunParticles()
    {
        if (stunParticles != null)
        {
            stunParticles.Play();
        }
    }

    [Server]
    void ClearStun()
    {
        stunned = false;

        RpcStopStunParticles();
    }

    [ClientRpc]
    void RpcStopStunParticles()
    {
        if (stunParticles != null)
        {
            stunParticles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public bool IsStunned()
    {
        return stunned;
    }
    
    public bool IsInvulnerable()
    {
        return invulnerable;
    }

    public void SetInvulnerable(bool invulnerable)
    {
        this.invulnerable = invulnerable;
    }

    public void SetInvulnerableForSeconds(float seconds)
    {
        StartCoroutine(InvulnSecondsCoroutine(seconds));
    }

    IEnumerator InvulnSecondsCoroutine(float seconds)
    {
        float secondsRemaining = seconds;

        while (secondsRemaining > 0)
        {
            invulnerable = true;
            secondsRemaining -= Time.deltaTime;
            yield return null;
        }

        invulnerable = false;
    }

    public bool godmode = false;
    public void SetGodMode(bool godmode)
    {
        this.godmode = godmode;
    }


    public void SetInvisible(bool invisible)
    {
        this.invisible = invisible;
    }

    public bool IsInvisible()
    {
        return invisible;
    }

    public virtual void OnSeen(Entity seer)
    {

    }

    

    [Server]
    public virtual void JumpAttackDone()
    {
        StartCoroutine(JumpAttackDoneCoroutine());
    }

    IEnumerator JumpAttackDoneCoroutine()
    {
        yield return new WaitForSeconds(0.5F);

        inJumpAttack = false;

        attacking = false;
    }



#region Materials Colors Etc

    [ClientRpc]
    public void RpcSetMaterial(string materialName, MaterialChangeProperties properties)
    {
        Material material = Resources.Load($"Materials/{materialName}") as Material;
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            // Debug.Log($"Set renderer {rmc.renderer} to mat {material}");
            if (rmc.renderer == null)
            {
                continue;
            }
            
            Material[] mats = rmc.renderer.materials;
            MaterialColor[] matsColors = rmc.materialsColors;
            for (int i = 0; i < mats.Length; i++)
            {
                if (matsColors[i].eliteOverride == EliteOverrideMode.NotOverridden)
                {
                    continue;
                }

                mats[i] = material;
            }
            rmc.renderer.materials = mats;
        }

        
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            Renderer renderer = rmc.renderer;
            MaterialColor[] matsColors = rmc.materialsColors;

            if (renderer == null)
            {
                Debug.LogWarning($"Entity {gameObject} has a null renderer in its default material color mapping");
                continue;
            }

            for (int i = 0; i < renderer.materials.Length && i < matsColors.Length; i++)
            {
                if (matsColors[i].eliteOverride == EliteOverrideMode.NotOverridden)
                {
                    continue;
                }
                
                if (properties.preserveBaseMap && matsColors[i] != null)
                {
                    renderer.materials[i].SetTexture("_BaseMap", matsColors[i].material.GetTexture("_BaseMap"));
                    renderer.materials[i].SetTexture("_MainTex", matsColors[i].material.GetTexture("_MainTex"));
                }

                if (properties.preserveColor && renderer.materials[i].HasProperty("_Color") && matsColors[i] != null)
                {
                    renderer.materials[i].color = matsColors[i].color;
                    renderer.materials[i].SetColor("_Color", matsColors[i].color);
                }

                if (properties.preserveEmission && renderer.materials[i].HasColor("_EmissionColor"))
                {
                    renderer.materials[i].EnableKeyword("_EMISSION");
                    renderer.materials[i].SetColor("_EmissionColor", matsColors[i].material.GetColor("_EmissionColor"));
                }

            }
        }
    }

    protected void SaveDefaultMaterialsColors()
    {
        defaultMaterialsColors ??= new();

        // Save default appearance for use in material/color changes
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
        {
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer)
            {
                continue;
            }


            if (!defaultMaterialsColors.Any(x => x.renderer == renderer))
            {
                SaveDefaultsForRenderer(renderer);
            }

            renderer.renderingLayerMask = RenderingLayerMask.GetMask("Entities");
        }
    }

    protected IEnumerator RefreshDefaultMatsColors()
    {
        //defaultMaterialsColors = new();
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        SaveDefaultMaterialsColors();
    }

    void SaveDefaultsForRenderer(Renderer renderer)
    {
        MaterialColor[] defaults = new MaterialColor[renderer.sharedMaterials.Length];
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            if (renderer.sharedMaterials[i].HasProperty("_Color"))
            {
                defaults[i] = new(renderer.sharedMaterials[i], renderer.sharedMaterials[i].color);
            }
        }

        defaultMaterialsColors.Add(new(renderer, defaults));
    }
    
    public void SetColorForSeconds(Color color, float seconds)
    {
        //foreach (Renderer renderer in renderers)
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            if (rmc.renderer == null)
            {
                continue;
            }
            
            foreach (Material material in rmc.renderer.materials)
            {
                material.color = color;
            }
        }
         
        Invoke(nameof(ResetColors), seconds);
    }


    [ClientRpc]
    public void RpcResetColors()
    {
        ResetColors();
    }


    [System.Serializable]
    public struct MaterialChangeProperties
    {
        public bool preserveBaseMap;
        public bool preserveColor;
        public bool preserveEmission;

        public MaterialChangeProperties(bool preserveBaseMap, bool preserveColor, bool preserveEmission)
        {
            this.preserveBaseMap = preserveBaseMap;
            this.preserveColor = preserveColor;
            this.preserveEmission = preserveEmission;
        }

        public static MaterialChangeProperties All = new(true, true, true);
        public static MaterialChangeProperties ColorAndEmission = new(false, true, true);
        public static MaterialChangeProperties None = new(false, false, false);
    }

    public void ResetColors()
    {
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            Renderer defaultRenderer = rmc.renderer;
            MaterialColor[] defaultMatsColors = rmc.materialsColors;

            for (int i = 0; i < defaultRenderer.materials.Length && i < defaultMatsColors.Length; i++)
            {
                if (defaultMatsColors[i].material == null) continue;
                
                if (defaultMatsColors[i] != null && defaultRenderer.materials[i].HasTexture("_BaseMap"))
                {
                    defaultRenderer.materials[i].SetTexture("_BaseMap", defaultMatsColors[i].material.GetTexture("_BaseMap"));
                }

                if (defaultRenderer.materials[i].HasProperty("_Color") && defaultMatsColors[i] != null)
                {
                    defaultRenderer.materials[i].color = defaultMatsColors[i].color;
                }

                if (defaultRenderer.materials[i].HasColor("_EmissionColor"))
                {
                    defaultRenderer.materials[i].EnableKeyword("_EMISSION");
                    defaultRenderer.materials[i].SetColor("_EmissionColor", defaultMatsColors[i].material.GetColor("_EmissionColor"));
                }

            }
        }
    }

    public void ResetMaterials()
    {
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            Renderer renderer = rmc.renderer;
            if (renderer == null)
            {
                Debug.LogError($"Renderer on {this} is null - materials will look incorrect");
                return;    
            }

            MaterialColor[] matsColors = rmc.materialsColors;

            List<Material> materials = new();

            for (int i = 0; i < matsColors.Length; i++)
            {
                materials.Add(matsColors[i].material);
            }

            renderer.materials = materials.ToArray();
        }
    }
    [ClientRpc]
    public void RpcSetMaterialFloat(string name, float value)
    {
        foreach (RendererMaterialColor rmc in defaultMaterialsColors)
        {
            if (rmc.renderer == null)
            {
                continue;
            }
            
            foreach (Material material in rmc.renderer.sharedMaterials)
            {
                if (material.HasProperty(name))
                {
                    material.SetFloat(name, value);
                }
            }
        }
         
    }


    [ClientRpc]
    public void RpcResetMaterials()
    {
        ResetMaterials();
    }

    [ClientRpc]
    public void RpcResetMaterialsColors()
    {
        ResetMaterials();
        ResetColors();
    }

    [ClientRpc(includeOwner = false)]
    public void RpcAddEliteComponent(string eliteTypeName)
    {
        Type eliteType = Type.GetType(eliteTypeName);
        if (eliteType == null)
        {
            return;
        }

        Elite elite = (Elite) gameObject.AddComponent(eliteType);
        originalScale = transform.localScale;
    }
#endregion

    [Server]
    public void AddEnergy(int amount)
    {
        float currentEnergy = GetStat(Stats.CurrentEnergy, 0);
        SetStat(Stats.CurrentEnergy, Mathf.Clamp(currentEnergy + amount, 0, GetStat(Stats.MaxEnergy, 0)));
    }

    [ServerCallback]
    public void Heal(float healingAmount, Entity healer, bool reportToUI = true)
    {
        float health = GetStat(Stats.CurrentHealth);
        health = health + healingAmount;
        if (health > GetStat(Stats.MaxHealth))
        {
            health = GetStat(Stats.MaxHealth);
        }

        SetStat(Stats.CurrentHealth, health);

        if (reportToUI)
        {
            PlayerUI.ReportHealingInstance(this, (int) healingAmount, healer);
        }
    }

    [Server]
    private void DetermineHitDetails(HitInfo hitInfo, HitType hitType, Entity damageDealer, Element damageType, bool forceCritical, bool forceStatus)
    {
        hitInfo.hitType = hitType;

        hitInfo.criticalHit = forceCritical || (damageDealer != null && UnityEngine.Random.Range(0, 100) < damageDealer.GetStat(Stats.CriticalChance));

        hitInfo.statusProc = forceStatus || (damageDealer != null && UnityEngine.Random.Range(0, 100) < damageDealer.GetStat(Stats.ElementalStatusChance));

        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < GetStat(Stats.AbsorbChance))
        {
            hitInfo.hitResult = HitResult.Absorbed;
        }
        else
        {
            hitInfo.hitResult = HitResult.Hit;
        }
    }

    public virtual void Attack(Entity target = null) {}

    [Server]
    public void DamageInSeconds(float waitDuration, int premitigationDamage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        StartCoroutine(DamageInSecondsCoroutine(waitDuration, premitigationDamage, damageType, hitType, damageDealer, impact, forceCritical, forceStatus, originEffectInstanceId));
    }

    [Server]
    public IEnumerator DamageInSecondsCoroutine(float waitDuration, int premitigationDamage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0)
    {
        yield return new WaitForSeconds(waitDuration);

        Damage(premitigationDamage, damageType, hitType, damageDealer, impact, forceCritical, forceStatus, originEffectInstanceId);
    }

    /* Damage the entity for the specified amount. Returns whether or not the entity died from this hit. */
    [ServerCallback]
    public HitInfo Damage(int premitigationDamage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0, HitboxHitData hitboxHitData=null, bool allowHitSound = true, bool scaleTick = true)
    {
        HitInfo info = new();
        info.premitigationDamage = premitigationDamage;
        info.originEffectInstanceId = originEffectInstanceId;
        if (invulnerable || godmode || IsDead())
        {
            return info;
        }

        // ----- Determine information about the hit -----------------------
        DetermineHitDetails(info, hitType, damageDealer, damageType, forceCritical, forceStatus);

        float totalDamage = premitigationDamage;

        // Apply status proc - effects cannot apply status
        if (info.statusProc && hitType != HitType.Effect)
        {
            Effect elementStatus = EffectLibrary.GetElementStatusEffect(damageType);
            if (elementStatus != null)
            {
                AddEffect(elementStatus, damageDealer);
            }
        }

        // Apply critical hit
        if (info.criticalHit) totalDamage *= damageDealer.GetStat(Stats.CriticalDamageMultiplier);

        // Non-armor multipliers
        totalDamage *= GetDamageTakenMultiplier();


        if (damageType != Element.TrueDamage && TryGetComponent(out Elite elite))
        {
            totalDamage = elite.ModifyDamageWithEliteResistances(totalDamage, damageType);
        }

        if (damageDealer != null)
        {
            // Determine entity-specific damage - obliterate, bonuses vs type, etc
            totalDamage = damageDealer.ModifyDamageForTarget(this, totalDamage);
            totalDamage = ModifyDamageFromTarget(damageDealer, totalDamage);

            totalDamage *= damageDealer.GetStat(Stats.GlobalDamageMultiplier);

            if (hitType != HitType.None)
            {
                if (hitType == HitType.Attack)
                {
                    totalDamage *= damageDealer.GetStat(Stats.AttackDamageMultiplier, 1);
                }
                else
                {
                    totalDamage *= damageDealer.GetStat(Stats.AbilityStrength, 1);
                }
            }

        }


        // Determine target-specific damage to deal
        if (damageType != Element.TrueDamage)
        {
            totalDamage *= GetArmorMultiplier();
        }

        // ----- Apply the hit -----------------------
        if (info.hitResult == HitResult.Hit)
        {
            if (totalDamage > 0)
            {
                if (totalDamage < 1)
                {
                    totalDamage = 1;
                }

                // One-shot protection
                if (damageType != Element.TrueDamage && this is ControlledEntity && GetHealthRatio() >= 0.9F && totalDamage >= GetStat(Stats.CurrentHealth))
                {
                    Debug.Log("SAVED BY OSP");
                    totalDamage = GetStat(Stats.CurrentHealth) - (0.05F * GetStat(Stats.MaxHealth));
                }

                int actualDamageDealt;
                if (totalDamage >= GetStat(Stats.CurrentHealth))
                {
                    actualDamageDealt = (int)GetStat(Stats.CurrentHealth);
                    SetStat(Stats.CurrentHealth, 0);
                }
                else
                {
                    SetStat(Stats.CurrentHealth, GetStat(Stats.CurrentHealth) - (int)totalDamage);
                    actualDamageDealt = (int)totalDamage;
                }


                if (damageDealer != null)
                {
                    if (!damagers.ContainsKey(damageDealer)) info.firstHitOnTarget = true;

                    AddDamager(damageDealer, actualDamageDealt);

                    AddThreat(damageDealer, premitigationDamage / 2);
                }

            }

            if (GetStat(Stats.CurrentHealth) <= 0 && !isDead)
            {
                OnEntityDied?.Invoke(this, damageDealer);
                killer = damageDealer;
                Die();
                info.killedTarget = true;
            }


            info.staggeredTarget = false;
            // Impact
            if (!info.killedTarget && impact > 0)
            {
                currentImpactResistance -= impact;
                if (currentImpactResistance <= 0)
                {
                    currentImpactResistance = impactResistance;
                    Stagger();
                }
            }

            lastDamaged = Time.time;
        }

        info.entityHit = this;
        info.damageDealer = damageDealer;
        info.damageDealt = (int) totalDamage;
        info.damageType = damageType;

        if (hitboxHitData != null)
        {
            info.hitPosition = hitboxHitData.position;
        }
        else
        {
            info.hitPosition = GetWorldPosCenter();
        }

        if (damageDealer != null)
        {
            damageDealer.HitAnEntity(info);
            
            float lifesteal = damageDealer.GetStat(Stats.Lifesteal, 0);
            if (lifesteal > 0)
            {
                damageDealer.Heal(totalDamage * lifesteal, damageDealer);
            }
        }

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnDamaged(info);
        }

        OnDamaged?.Invoke(info);

        RpcDamaged((int) totalDamage, allowHitSound, scaleTick);

        PlayerUI.ReportDamageInstance(info);

        // Update last combat time if the (local) player was involved in this
        if (damageDealer == Player.GetLocalPlayer().GetControlledEntity()
            || this == Player.GetLocalPlayer().GetControlledEntity())
        {
            AudioManager.UpdateCombatTime();
        }

        return info;
    }

    [ClientRpc]
    void RpcDamaged(int totalDamage, bool allowHitSound, bool scaleTick)
    {
        if (scaleTick)
        {
            float magnitude = 1.05F;
            transform.localScale = originalScale * magnitude;
        }

        if (totalDamage > 0)
        {
            SetColorForSeconds(ColorPalette.GetDefaultPalette().entityHealthDamaged, 0.25F);

            if (allowHitSound)
            {
                AudioManager.Singleton.PlayOneShot(damagedSound, transform.position);
            }
        }     
    }

    
    [Server]
    public virtual void HitAnEntity(HitInfo hitResult)
    {
        OnHitEntity?.Invoke(hitResult);
        
        if (GetTraits() != null)
        {
            foreach (Trait trait in GetTraits().ToProcOrderList())
            {
                trait.OnHit(hitResult);
            }
        }

        if (owningPlayer != null)
        {
            owningPlayer.HitAnEntity(hitResult);
        }
    }

    
    [Server]
    public void KilledAnEntity(HitInfo hitResult)
    {
        OnKillEntity?.Invoke(hitResult);

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnKill(hitResult);
        }

        if (owningPlayer != null)
        {
            owningPlayer.KilledAnEntity(hitResult);
        }
    }


#region Threat Management
    private void AddThreat(Entity threatSource, int threatToAdd)
    {
        if (threats.ContainsKey(threatSource))
        {
            threats[threatSource] += threatToAdd;
        }
        else
        {
            threats.Add(threatSource, threatToAdd);
        }
    }

    public float GetThreatFromEntity(Entity entity)
    {
        if (threats.ContainsKey(entity))
        {
            return threats[entity];
        }
        else
        {
            return 0;
        }
    }

    private void AddDamager(Entity damageDealer, int actualDamageDealt)
    {
        if (damagers.ContainsKey(damageDealer))
        {
            damagers[damageDealer] += actualDamageDealt;
        }
        else
        {
            damagers.Add(damageDealer, actualDamageDealt);
        }
    }

    [Server]
    public bool HasBeenDamagedBy(Entity other)
    {
        return damagers.ContainsKey(other);
    }

    

    /// <summary>
    /// Get nearest enemy
    /// </summary>
    /// <param name="radius">Range to check, leave 0/don't pass if infinite</param>
    /// <param name="requiresSight">Whether sight on an entity is required to return it</param>
    /// <returns></returns>
    [Server]
    public virtual Entity GetPreferredEnemy(float radius = 50.0F)
    {

        List<Entity> inRangeEnemies = new();
        foreach (Entity entity in WorldManager.GetWorldManager().entities)
        {
            // Omit non-enemies
            if (IsValidEnemy(entity) && Vector3.Distance(transform.position, entity.transform.position) <= radius)
            {
                inRangeEnemies.Add(entity);
            }
        }

        if (inRangeEnemies.Count == 0)
        {
            return null;
        }

        Entity bestEnemy = inRangeEnemies[0];
        float bestEnemyScore = 0;

        foreach (Entity entity in inRangeEnemies)
        {   
            float distanceToEnemy = Vector3.Distance(transform.position, entity.transform.position);
            distanceToEnemy = Mathf.Clamp(distanceToEnemy, 5F, Mathf.Max(5, radius)); // Within a radius of about 5m everything should be counted the same distance-wise - threat will determine from there

            float distanceFactor = radius / distanceToEnemy; // how much distance affects threat for this algorithm (1 at max radius, huge at min radius)
            distanceFactor = Mathf.Clamp(distanceFactor, 0.3F, 10.0F);

            float thisEnemyScore = Mathf.Clamp(GetThreatFromEntity(entity), 1, Mathf.Infinity) * distanceFactor; // Clamp threat so we don't pick a random enemy because all were 0

            if (thisEnemyScore > bestEnemyScore)
            {
                bestEnemy = entity;
                bestEnemyScore = thisEnemyScore;
            }
        }

        return bestEnemy;
    }

    public Entity GetNearestEnemy(float radius = 50.0F, float bearingAngle=0, float pitchAngle=0, bool relativeToLookingDirection = false)
    {
        if (WorldManager.GetWorldManager() == null)
        {
            Debug.LogWarning("No world manager");
            return null;
        }

        List<Entity> inRangeEnemies = new();
        foreach (Entity entity in WorldManager.GetWorldManager().entities)
        {
            // Omit non-enemies
            if (IsValidEnemy(entity) && Vector3.Distance(transform.position, entity.transform.position) <= radius)
            {
                float rBearing = GetRelativeBearingAngle(entity.GetWorldPosCenter(), relativeToLookingDirection);
                if (bearingAngle != 0 && Mathf.Abs(rBearing) > bearingAngle)
                {
                    // Heading angle check
                    continue;
                }
                float rPitch = GetRelativePitchAngle(entity.GetWorldPosCenter(), relativeToLookingDirection);
                if (pitchAngle != 0 && Mathf.Abs(rPitch) > pitchAngle)
                {
                    // Pitch angle check
                    continue;
                }

                inRangeEnemies.Add(entity);
            }
        }

        if (inRangeEnemies.Count == 0)
        {
            return null;
        }


        inRangeEnemies.Sort((e1, e2) => { return Vector3.Distance(transform.position, e1.transform.position).CompareTo(Vector3.Distance(transform.position, e2.transform.position)); });
        return inRangeEnemies[0];
    }

    public List<Entity> GetNearbyAllies(float radius, bool includeNeutral = true)
    {
        List<Entity> inRangeAllies = new();
        foreach (Entity entity in WorldManager.GetWorldManager().entities)
        {
            if (entity == null || !entity.gameObject.activeSelf)
            {
                continue;
            }

            // Omit non-enemies
            if ((IsAlly(entity)
                    || (includeNeutral && !IsEnemy(entity)))
            
                && Vector3.Distance(transform.position, entity.transform.position) <= radius)
            {
                inRangeAllies.Add(entity);
            }
        }

        return inRangeAllies;
    }

    
    public List<Entity> GetNearbyEnemies(float radius, bool includeNeutral = true, Transform alternateOriginTransform = null)
    {
        Transform transformToUse = transform;
        if (alternateOriginTransform != null)
        {
            transformToUse = alternateOriginTransform;
        }

        List<Entity> inRangeEnemies = new();
        foreach (Entity entity in WorldManager.GetWorldManager().entities)
        {
            if (entity == null || !entity.gameObject.activeSelf)
            {
                continue;
            }

            // Omit non-enemies
            if ((IsEnemy(entity)
                    || (includeNeutral && !IsAlly(entity)))
            
                && Vector3.Distance(transformToUse.position, entity.transform.position) <= radius)
            {
                inRangeEnemies.Add(entity);
            }
        }

        return inRangeEnemies;
    }

    public bool IsValidEnemy(Entity entity)
    {
        return entity != null
            && entity.gameObject.activeSelf
            && entity is not NonPlayerCharacter
            && IsEnemy(entity)
            && SeesEntity(entity)
            && !entity.IsDead();
    }

    /// <summary>
    /// Whether this considers other to be an ally
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsAlly(Entity other)
    {
        return other == this || faction.IsAlliesWithFaction(other.GetFaction());
    }

    /// <summary>
    /// Whether this considers other to be an enemy
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsEnemy(Entity other)
    {
        return other != this && faction.IsEnemiesWithFaction(other.GetFaction());
    }


    public Faction GetFaction()
    {
        return faction;
    }
    
    
    public bool IsDead()
    {
        return isDead;
    }

#endregion


    [Server]
    public virtual void Stagger()
    {
        if (isDead || animator == null)
        {
            return;
        }

        bool hasStaggerAnim = false;
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Stagger")
            {
                hasStaggerAnim = true;
            }
        }

        if (staggerable && hasStaggerAnim)
        {
            attacking = false;
            SetAnimatorTrigger("Stagger");
        }

        lastStagger = Time.time;

        OnStaggered();
    }

    protected virtual void OnStaggered()
    {
        
    }
    
    public bool paused = false;
    public void PauseEntity()
    {
        paused = true;
        if (animator != null)
        {
            animator.speed = 0;
        }

        foreach (VisualEffect visualEffect in GetComponentsInChildren<VisualEffect>())
        {
            visualEffect.pause = true;
        }
    }

    public void UnpauseEntity()
    {
        paused = false;
        if (animator != null)
        {
            animator.speed = 1; 
        }

        foreach (VisualEffect visualEffect in GetComponentsInChildren<VisualEffect>())
        {
            visualEffect.pause = false;
        }
    }


    public Entity GetDamageableEntity()
    {
        return this;
    }

    public int GetDamageablePriority()
    {
        return 0;
    }

    

    public void SetAttacking(bool attacking = true)
    {
        this.attacking = attacking;
    }

    public void SetAttackingInSeconds(bool attacking, float seconds)
    {
        StartCoroutine(SetAttackingCoroutine(attacking, seconds));
    }

    IEnumerator SetAttackingCoroutine(bool attacking, float seconds)
    {
        bool previousAttackingState = this.attacking;
        yield return new WaitForSeconds(seconds);

        // Only change if something else hasn't since changed the attacking state
        if (this.attacking == previousAttackingState)
        {
            Debug.Log("Set not attack");
             this.attacking = attacking;
        }
    }

    public virtual bool IsAttacking()
    {
        return attacking;
    }

    [ServerCallback]
    public void AttackComplete()
    {
        attacking = false;
    }

    [Server]
    public void OnProjectileCreated(Projectile projectilePrefab, Projectile projectileInstance)
    {
        StartCoroutine(OnProjectileCreatedCoroutine(projectilePrefab, projectileInstance));
    }

    [Server]
    IEnumerator OnProjectileCreatedCoroutine(Projectile projectilePrefab, Projectile projectileInstance)
    {
        // Give one frame for it to spawn
        yield return null;

        ApplyTraitsToProjectile(projectilePrefab, projectileInstance);
    }

    [Server]
    public virtual void ApplyTraitsToProjectile(Projectile projectilePrefab, Projectile projectileInstance)
    {
        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnProjectileCreated(projectilePrefab, projectileInstance);
        }
    }


    public virtual string GetName()
    {
        return entityName;
    }

    public virtual string GetDisplayName()
    {
        if (TryGetComponent(out Elite elite))
        {
            return elite.GetElitePrefix() + " " + entityName;
        }

        return entityName;
    }
     
   
    /// <summary>
    /// Applies any necessary modifiers to damage against a specific target
    /// </summary>
    /// <param name="target"></param>
    /// <param name="damage"></param>
    /// <returns></returns>
    public virtual float ModifyDamageForTarget(Entity target, float damage)
    {
        if (GetOwningPlayer() != null)
        {
            foreach (Trait trait in GetTraits().ToProcOrderList())
            {
                damage = trait.ModifyDamageForTarget(this, target, damage);
            }
        }

        return damage;
    }

    /// <summary>
    /// Applies modifiers from this entity onto damage from a specific target
    /// </summary>
    /// <param name="target"></param>
    /// <param name="damage"></param>
    /// <returns></returns>
    public virtual float ModifyDamageFromTarget(Entity target, float damage)
    {
        return damage;
    }


    public bool IsEntityType(EntityType type)
    {
        return types.Contains(type);
    }

    // Whether this entity can see other
    [Server]
    public bool SeesEntity(Entity other)
    {
        Vector3 otherPosition = other.GetWorldPosCenter(); /* so not looking through ground */

        if (other.IsInvisible() || Vector3.Distance(GetWorldPosCenter(), otherPosition) > visionRadius)
        {
            // Non-LOS reasons it can't see the other
            return false;
        }
        
        Vector3 sightPos = sightPoint != null ? sightPoint.position : GetWorldPosCenter();

        return !Physics.Linecast(sightPos, otherPosition, out var hit, ~ignoreInSightCalculation, QueryTriggerInteraction.Ignore);
    }

    public virtual Vector3 GetWorldPosCenter()
    {
        Collider collider = GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.bounds.center;
        }

        return transform.position + new Vector3(0, 1, 0);
    }

    
    public virtual Vector3 GetLocalPosCenter()
    {
        return transform.InverseTransformPoint(GetWorldPosCenter());
    }


    public float GetHeight()
    {
        return GetComponent<Collider>().bounds.extents.y * 2;
    }

    public Entity GetKiller()
    {
        return killer;
    }

    public CameraContext GetCameraContext()
    {
        return cameraContext;
    }


    // Used to denote transformations, by derivations that can transform
    public void Transforming(Entity newForm)
    {
        beingPushed = false;
        RpcTransforming(newForm);
    }

    [ClientRpc]
    void RpcTransforming(Entity newForm)
    {
        OnTransformed?.Invoke(this, newForm);
    }


#region Animations
    [Server]
    public void PlayAnimation(string animationName, float normalizedTransitionDuration)
    {
        RpcPlayAnimation(animationName, normalizedTransitionDuration);
    }
    
    [ClientRpc]
    public void RpcPlayAnimation(string animationName, float normalizedTransitionDuration)
    {
        if (animator == null)
        {
            return;
        }
        
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(animationName)))
                animator.CrossFade(animationName, normalizedTransitionDuration, i);
        }
    }

    [Server]
    public void SetAnimatorTrigger(string triggerName)
    {
        RpcSetAnimatorTrigger(triggerName);
    }

    [ClientRpc]
    void RpcSetAnimatorTrigger(string triggerName)
    {
        try
        {
            animator.SetTrigger(triggerName);
        }
        catch (Exception e)
        {
            Debug.Log("Error trying to set animation trigger " + triggerName + " on " + GetName());
            Debug.Log(e);
        }
    }

    [ClientRpc]
    public void RpcSetAnimatorBool(string boolName, bool value)
    {
        try
        {
            animator.SetBool(boolName, value);
        }
        catch (Exception e)
        {
            Debug.Log("Error trying to set animation bool " + boolName + " on " + GetName());
            Debug.Log(e);
        }
    }

#endregion


#region Death And Teardown

    [Server]
    public virtual void Die()
    {
        isDead = true;

        stateMachine.ChangeState(new DeadState(this));

        DropLoot();

        foreach (KeyValuePair<Entity, int> kvp in damagers)
        {
            if (kvp.Key.GetOwningPlayer() is Player damagerPlayer)
            {
                int experienceForPlayer = (int)(Equation.EXP_AWARDED_PER_EFFECTIVE_HEALTH.Calculate(ActualToEffectiveHealth(kvp.Value)) * GetStat(Stats.ExperienceRewardedMultiplier, 1.0F));
                damagerPlayer.AddExperience(experienceForPlayer);
            }
        }

        OnDeath?.Invoke(this, killer);

        for (int i = activeEffectInstances.Count - 1; i >= 0; i--)
        {
            if (activeEffectInstances[i] != null)
            {
                activeEffectInstances[i].TargetEntityDeath();
            }
        }

        // Disable colliders now, no need after death
        // Only on things that won't immediately fall through the floor
        if (GetComponent<CharacterController>() != null || GetComponent<NavMeshAgent>() != null)
        {
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        // Handle animation/lack of
        bool hasTrigger = false;
        animator.speed = 1;
        
        // If no death animation, look for a Die trigger, this is sometimes problematic but usually fine
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == "Die")
            {
                hasTrigger = true;
                SetAnimatorTrigger("Die");
            }
        }

        // If none of the above worked, there is no animation. Just remove this from the screen
        if (!hasTrigger)
        {
            TeardownEntity();
        }
        else
        {
            // Failsafe in case death animation doesnt play
            Invoke(nameof(TeardownEntity), 3); 
        }
    }

    
    // Called by animator
    // When overriding this, call base.DeathComplete() at end
    public virtual void DeathComplete()
    {
        CreateCorpse(this);

        if (isServer)
        {
            TeardownEntity();
        }
    }


    public GameObject corpseObject; // only exists in last few seconds of entity's existence, used for overrides of CreateCorpse
    public virtual void CreateCorpse(Entity entity)
    {
        ResetColors();

        if (!createsCorpse)
        {
            return;
        }

        Transform parent = null;
        if (GameObject.Find("Corpses") == null)
        {
            parent = new GameObject("Corpses").transform;
        }

        corpseObject = new(entity.GetDisplayName() + " Corpse");
        corpseObject.transform.SetPositionAndRotation(entity.transform.position, entity.transform.rotation);
        corpseObject.transform.SetParent(parent);

        SkinnedMeshRenderer[] renderers = entity.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].gameObject.layer == LayerMask.NameToLayer("Entities") || (renderers[i].gameObject.layer == LayerMask.NameToLayer("Default")))
            {
                GameObject rendererObj = entity.CreateMeshImage(renderers[i]);
                rendererObj.transform.parent = corpseObject.transform;
            }
        }

        Rigidbody rb = corpseObject.AddComponent<Rigidbody>();
        RigidbodyConstraints rbc = ~RigidbodyConstraints.FreezePositionY;
        rb.constraints = rbc;

        BoxCollider coll = corpseObject.AddComponent<BoxCollider>();
        coll.center = new(0, 0.5F);
        coll.size = new(0.5F, 0.5F, 0.5F);

        corpseObject.AddComponent<AutoDestroy>().lifespan = CORPSE_LIFESPAN;

        corpseObject.AddComponent<Corpse>().entityType = entity.GetType();

        corpseObject.layer = LayerMask.NameToLayer("Entities");
    }

    public virtual void TeardownEntity()
    {
        // if (GetOwningPlayer() != null)
        // {
        //     GetOwningPlayer().SetWraithForm(null);
        //     GetOwningPlayer().SetControlledEntity(null);

        // }
        WorldManager.UnregisterEntity(this);

        OnDeathAnimationComplete?.Invoke();

        NetworkServer.UnSpawn(gameObject);
        Destroy(gameObject);
    }
    
    // For behavior state machines
    public class DeadState : State
    {
        Entity entity;

        public DeadState(Entity entity)
        {
            this.entity = entity;
            this.entity.invulnerable = true;
        }

        public override void Update()
        {
            if (entity.body != null && !entity.body.isKinematic)
            {
                entity.body.linearVelocity = new Vector3(0.0F, entity.body.linearVelocity.y, 0.0F);
            }
        }

        public override bool ReadyForExit()
        {
            return false; // Never exiting, most likely
        }
    }

#endregion
    
    GameObject CreateMeshImage(SkinnedMeshRenderer renderer)
    {
        GameObject newObj = new();

        MeshRenderer mr = newObj.AddComponent<MeshRenderer>();
        mr.materials = renderer.materials;

        MeshFilter mf = newObj.AddComponent<MeshFilter>();
        Mesh mesh = new();
        renderer.BakeMesh(mesh);
        mf.mesh = mesh;

        newObj.transform.position = renderer.transform.position;
        newObj.transform.rotation = renderer.transform.rotation;

        return newObj;
    }


    public void SetCountedBySpawners(bool counted)
    {
        countedBySpawners = counted;
    }

    public bool IsCountedBySpawners()
    {
        return countedBySpawners;
    }

    [Server]
    void DropLoot()
    {
        float droppedLootMult = GetStat(Stats.DroppedLootMult, 1);
        if (droppedLootMult <= 0)
        {
            // Don't bother processing if we arent dropping loot
            return;
        }
        
        if (dropTable != null)
        {
            List<DropInstance> drops = dropTable.GetDrops(dropTableRolls, Mathf.Pow(Equation.ENTITY_HEALTH_SCALING.Calculate(entityLevel), 1.1F));

            if (killer != null)
            {
                killer.ModifyDropsFromKilledEnmy(drops);
            }

            foreach (DropInstance instance in drops)
            {
                instance.quantity = (int) (instance.quantity * droppedLootMult);
            }

            dropTable.DropLoot(drops, GetWorldPosCenter());

            int healthOrbsDropped = GetNumberOfHealthOrbsDropped();
            if (killer != null)
            {
                healthOrbsDropped = (int) (healthOrbsDropped * killer.GetHealthOrbsOnKillMult());
            }
            
            for (int i = 0; i < healthOrbsDropped; i++)
            {
                DropHealthOrb();
            }
        }
    }

    public virtual float GetHealthOrbsOnKillMult()
    {
        return 1.0F;
    }

    void ModifyDropsFromKilledEnmy(List<DropInstance> drops)
    {
        if (GetTraits() != null)
        {
            foreach (Trait trait in GetTraits().ToProcOrderList())
            {
                trait.ModifyEnemyItemDrops(this, drops);
            }
        }
    }

    public void DropHealthOrb()
    {
        Vector2 horizOffset = UnityEngine.Random.insideUnitCircle * 3.0F;
        Vector3 pos = transform.position + new Vector3(horizOffset.x, 0, horizOffset.y);

        AetherdaleData.GetAetherdaleData().healingOrbPickup.Drop(pos, shared:AreaSequencer.GetAreaSequencer().IsSequenceRunning());
    }

    public virtual int GetNumberOfHealthOrbsDropped()
    {
        return  UnityEngine.Random.Range(0, 2) + (int) Mathf.Log10(GetEffectiveMaxHealth());
    }

    public Transform GetFloatingHealthBarTransform()
    {
        if (floatingHealthBarTransform != null)
        {
            return floatingHealthBarTransform;
        }

        return gameObject.transform;
    }

    float lastFootstep = 0;
    public void Footstep()
    {
        // Animator layers need to be synced w/ time
        if (Time.time - lastFootstep < 5 * Time.fixedDeltaTime || !grounded)
        {
            return;
        }

        lastFootstep = Time.time;

        footstepVFX.SendEvent("Footstep");

        FootstepSound();
    }

    public virtual void FootstepSound()
    {
        if (footstepsSound.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.footsteps.dirtLight, transform.position);  
        }
        else
        {
            AudioManager.Singleton.PlayOneShot(footstepsSound, transform.position);
        }
    }


    public float GetSize()
    {
        Collider entityCollider = gameObject.GetComponent<Collider>();
        return  entityCollider.bounds.extents.z;
    }

    public bool DamageableColliderDistanceMatters()
    {
        return false;
    }

    public bool HasEphemera()
    {
        return ephemeraParentTransform.childCount != 0;
    }

    [Server]
    public void AddEliteEphemera(Element element)
    {
        RpcAddEliteEphemera(element);
    }

    [ClientRpc]
    void RpcAddEliteEphemera(Element element)
    {
        GameObject ephemera = VisualEffectIndex.GetDefaultEffectIndex().GetElementalEliteVisualEffect(element);

        Instantiate(ephemera, ephemeraParentTransform);
    }

    public virtual Vector3 GetAimedPosition()
    {
        return transform.position + (transform.forward * 10.0F);
    }


    public virtual void TargetEnterAimMode(bool includeEntities)
    {
        aiming = true;
    }

    public virtual void TargetExitAimMode()
    {
        aiming = false;
    }

    public virtual float GetMovespeedMult()
    {
        float aimMult = aiming ? AIM_MOVESPEED_MULT : 1.0F;
        
        return GetStat(Stats.MovementSpeedMult, 1.0F) * aimMult;
    }
    
}

using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class AreaOfEffect : NetworkBehaviour
{

    [System.Serializable]
    public class AOEProperties
    {
        public int damage;
        public Element damageType;
        public bool hitsOwner = false;
        public float duration;
        public float hitDuration = 0.2F;
        public float hitResetInterval = 0;
        public float hitDelay = 0;
        public Effect[] appliedEffects;

        public ClusterAOEProperties clusterAOEProperties = new();

        [System.Serializable]
        public class ClusterAOEProperties
        {
            public bool releasesProjectiles = false;
            public float releasedProjectileDelay = 1.0F;
            public Projectile releasedProjectile;
            public float releasedProjectileMinSpeed = 6.0F;
            public float releasedProjectileMaxSpeed = 10.0F;
            public int minReleasedProjectiles = 3;
            public int maxReleasedProjectiles = 5;
            public ProjectileReleaseMode projectileReleaseMode = ProjectileReleaseMode.Random;

        }

        public AOEProperties Copy()
        {
            return (AOEProperties) MemberwiseClone();
        }
    }

    public AOEProperties properties = new();



    [SerializeField] AreaOfEffectTelegrapher telegrapher;
    [SerializeField] EventReference initialSound;


    public enum ProjectileReleaseMode
    {
        Random = 0,
        Radial = 1
    }

    protected Entity damageDealer;
    protected HitType hitType;
    protected int impact;
    ParticleSystem effectParticleSystem;
    Hitbox hitbox;
    float durationRemaining;
    bool regularHitboxStruck = false;

    bool hitStarted = false;
    float hitDelayRemaining = 0;
    float lastHitReset = -9000;


    [SyncVar] Vector3 velocity = new();


    [Server]
    public static AOEProperties Create(AreaOfEffect aoe, Vector3 position, Entity damageDealer, HitType hitType = HitType.None, int impact = 0, Transform parentTransform = null, bool useParentRotation = false, bool skipTelegraph = false)
    {
        if (aoe.telegrapher != null && !skipTelegraph)
        {
            return AreaOfEffectTelegrapher.Create(aoe.telegrapher, aoe, position, damageDealer, hitType, impact, parentTransform, useParentRotation);
        }

        return AreaOfEffect.CreateNoTelegraph(aoe, position, damageDealer, hitType, impact, parentTransform, useParentRotation).properties;
    }

    [Server]
    public static AreaOfEffect CreateNoTelegraph(AreaOfEffect aoe, Vector3 position, Entity damageDealer, HitType hitType = HitType.None, int impact = 0, Transform parentTransform = null, bool useParentRotation = false)
    {
        AreaOfEffect aoeInstance;
        if (parentTransform != null)
        {
            aoeInstance = Instantiate(aoe, position, useParentRotation ? parentTransform.rotation : Quaternion.identity, parentTransform);
        }
        else
        {
            aoeInstance = Instantiate(aoe, position, Quaternion.identity);
        }

        float sizeMultiplier = damageDealer.GetStat(Stats.AOERadiusMultiplier, 1.0F);
        aoeInstance.transform.localScale *= sizeMultiplier;

        foreach (ParticleSystem ps in aoeInstance.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius *= sizeMultiplier;
        }

        aoeInstance.hitType = hitType;
        aoeInstance.impact = impact;
        aoeInstance.damageDealer = damageDealer;

        NetworkServer.Spawn(aoeInstance.gameObject);

        return aoeInstance;
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {
        hitDelayRemaining = properties.hitDelay;
        durationRemaining = properties.duration;
        hitbox = GetComponentInChildren<Hitbox>();
        if (hitbox != null)
            hitbox.hitsOwner = properties.hitsOwner;

        if (isServer && properties.clusterAOEProperties.releasesProjectiles)
        {
            Invoke(nameof(ReleaseProjectiles), properties.clusterAOEProperties.releasedProjectileDelay);
        }

        if (GetComponentInChildren<VisualEffect>() is VisualEffect visualEffect)
        {
            // Visual effect
            // No work needed generally
        }
        else
        {
            // Assume particle system
            effectParticleSystem = GetComponentInChildren<ParticleSystem>();
            if (effectParticleSystem == null)
            {
                effectParticleSystem = GetComponentInParent<ParticleSystem>();
            }

            effectParticleSystem.Play();
        }

        if (isClient && !initialSound.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(initialSound, transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += velocity * Time.deltaTime;
        if (!isServer)
        {
            return;
        }


        if (!hitStarted)
        {
            if (hitDelayRemaining <= 0)
            {
                if (hitbox != null)
                {
                    hitbox.StartHit(properties.damage, properties.damageType, hitType, damageDealer, impact);
                    hitStarted = true;
                    lastHitReset = Time.time;

                    if (properties.hitDuration != 0)
                    {
                        Invoke(nameof(EndHit), properties.hitDuration);
                    }
                }
            }
            else
            {
                hitDelayRemaining -= Time.deltaTime;
            }
        }
        else
        {
            // Check for hit reset - multi-hit AOEs
            if (properties.hitResetInterval != 0 && (Time.time - lastHitReset) > properties.hitResetInterval)
            {
                hitbox.ResetHits();
                lastHitReset = Time.time;
            }
        }

        durationRemaining -= Time.deltaTime;
        if (durationRemaining <= 0)
        {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        EndHit();
    }

    void EndHit()
    {
        if (hitbox != null)
        {
            hitbox.EndHit();
        }
    }

    protected virtual void UpdateAOE()
    {
        if (!regularHitboxStruck && hitbox != null)
        {
            regularHitboxStruck = true;
            List<HitInfo> results = hitbox.HitOnce(properties.damage, properties.damageType, damageDealer, hitType: hitType);

            foreach (HitInfo result in results)
            {
                if (result.hitResult == HitResult.Hit && !result.killedTarget)
                {
                    foreach (Effect effect in properties.appliedEffects)
                    {
                        result.entityHit.AddEffect(effect, damageDealer);
                    }
                }
            }
        }
    }

    public float GetRadius(Entity user)
    {
        Collider coll = GetComponentInChildren<Collider>();

        if (coll == null)
        {
            throw new System.Exception($"No collider found on AoE {gameObject.name} - must have SphereCollider");
        }


        float radius;
        if (coll is SphereCollider sphereCollider)
        {
            radius = sphereCollider.radius;
        }
        else
        {
            throw new System.Exception($"AoE {gameObject.name} does not have a SphereCollider");
        }


        if (user != null)
        {
            radius *= user.GetStat(Stats.AOERadiusMultiplier, 1.0F);
        }

        return radius;
    }

    protected virtual void OnHit(Entity target)
    {
        // Override this if special on-hit effects are desired
    }

    #region Projectile Release
    protected virtual void ReleaseProjectiles()
    {
        if (properties.clusterAOEProperties.projectileReleaseMode == ProjectileReleaseMode.Random)
        {
            int numProjectiles = Random.Range(properties.clusterAOEProperties.minReleasedProjectiles, properties.clusterAOEProperties.maxReleasedProjectiles);
            for (int i = 0; i < numProjectiles; i++)
            {
                float speed = Random.Range(properties.clusterAOEProperties.releasedProjectileMinSpeed, properties.clusterAOEProperties.releasedProjectileMaxSpeed);
                Vector3 direction = Random.insideUnitSphere.normalized;

                Projectile.Create(properties.clusterAOEProperties.releasedProjectile, GetComponentInChildren<Collider>().bounds.center, transform.rotation, damageDealer.gameObject, direction * speed);
            }
        }
    }
    #endregion


    public void SetDamage(int damage)
    {
        properties.damage = damage;
    }

    public void DelayHit(float hitDelay)
    {
        properties.hitDelay = hitDelay;
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.velocity = velocity;
    }

    public void SetDuration(float duration)
    {
        properties.duration = duration;
        this.durationRemaining = duration;
    }





    // protected override void OnValidate()
    // {
    //     base.OnValidate();

        // properties.damage = damage;
        // properties.damageType = damageType;
        // properties.hitsOwner = hitsOwner;
        // properties.duration = duration;
        // properties.hitDuration = hitDuration;
        // properties.hitResetInterval = hitResetInterval;
        // properties.appliedEffects = appliedEffects;

        // properties.clusterAOEProperties.releasesProjectiles = releasesProjectiles;
        // properties.clusterAOEProperties.releasedProjectileDelay = releasedProjectileDelay;
        // properties.clusterAOEProperties.releasedProjectile = releasedProjectile;
        // properties.clusterAOEProperties.releasedProjectileMinSpeed = releasedProjectileMinSpeed;
        // properties.clusterAOEProperties.releasedProjectileMaxSpeed = releasedProjectileMaxSpeed;
        // properties.clusterAOEProperties.minReleasedProjectiles = minReleasedProjectiles;
        // properties.clusterAOEProperties.maxReleasedProjectiles = maxReleasedProjectiles;
        // properties.clusterAOEProperties.projectileReleaseMode = projectileReleaseMode;

        // EditorUtility.SetDirty(this);
    // }
    
    // // OBE
    // [SerializeField] int damage;
    // [SerializeField] Element damageType;
    // [SerializeField] bool hitsOwner = false;
    // [SerializeField] float duration;
    // [SerializeField] float hitDuration = 0.2F;
    // [SerializeField] float hitResetInterval = 0;
    // [SerializeField] Effect[] appliedEffects;


    // [Header("Cluster AOE Settings")]
    // [SerializeField] bool releasesProjectiles = false;
    // [SerializeField] float releasedProjectileDelay = 1.0F;
    // [SerializeField] Projectile releasedProjectile;
    // [SerializeField] float releasedProjectileMinSpeed = 6.0F;
    // [SerializeField] float releasedProjectileMaxSpeed = 10.0F;
    // [SerializeField] int minReleasedProjectiles = 3;
    // [SerializeField] int maxReleasedProjectiles = 5;
    // [SerializeField] ProjectileReleaseMode projectileReleaseMode = ProjectileReleaseMode.Random;
}



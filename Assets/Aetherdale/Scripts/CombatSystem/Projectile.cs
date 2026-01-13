using System;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

// A projectile
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : NetworkBehaviour
{
    public int damageOnHit;
    public int impact;
    public Element damageType;
    public bool useGravity;
    public HitType hitType = HitType.Attack;
    public bool hasLifespan;
    public float lifespanSeconds;
    public bool randomLifespan;
    public float randomLifespanMax;
    public bool passThroughAllies = true;
    public bool persistAfterHit;
    public bool destroyOnFlightEnd = true;
    public bool sticky = false;
    public float stickyTimeToStop = 0.1F;
    public float stickyAdditionalLifespan;

    public float inaccuracy = 0;
    public Vector2 inaccuracyMult = new(1, 1);
    public AreaOfEffect explosion;

    [SerializeField] EventReference initialSound;
    [SerializeField] EventReference hitSound;
    [SerializeField] EventReference collisionSound;
    [SerializeField] EventReference flightSound;

    public Action<Projectile> OnFlightEnd;
    public Action<Projectile, HitInfo> OnHit;
    public Action OnCollide;
    
    public Action<Entity> OnStick;
    public UnityEvent OnStickUnityEvent;

    public Action<AreaOfEffect.AOEProperties> OnExplosionCreated;
    
    protected Rigidbody body;

    public GameObject progenitor;
    public Vector3 velocity = Vector3.zero;

    protected bool active;

    [SyncVar] bool stuck = false;
    //Transform stuckTransform;
    //Vector3 stuckOffset;
    //Quaternion stuckRotation;

    [SyncVar] protected float projectileStartTime = 0.0F;
    List<Entity> hitEntities = new();

    protected GameObject target;

    EventInstance flightEventInstance;

    float lifespanRemaining = 0;

    bool aoeDamageOverridden = false;
    int aoeDamage = 0;


    [Server]
    public static T Create<T>(T projectile, Transform spawnTransform, GameObject progenitor, Vector3 velocity, bool triggerCreationEvent = true) where T : Projectile
    {
        T shotProjectile = Instantiate(projectile, spawnTransform);
        shotProjectile.transform.SetParent(null);
        
        NetworkServer.Spawn(shotProjectile.gameObject);

        shotProjectile.Initialize(progenitor, velocity);

        if (triggerCreationEvent && progenitor.TryGetComponent(out Entity entity))
        {
            entity.OnProjectileCreated(projectile, shotProjectile);
        }

        shotProjectile.active = true;

        return shotProjectile;
    }

    [Server]
    public static T Create<T>(T projectile, Vector3 spawnPosition, Quaternion spawnRotation, GameObject progenitor, Vector3 velocity, bool triggerCreationEvent = true) where T : Projectile
    {
        T shotProjectile = Instantiate(projectile);
        shotProjectile.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        NetworkServer.Spawn(shotProjectile.gameObject);

        shotProjectile.Initialize(progenitor, velocity);

        if (triggerCreationEvent && progenitor.TryGetComponent(out Entity entity))
        {
            entity.OnProjectileCreated(projectile, shotProjectile);
        }
        
        shotProjectile.active = true;

        return shotProjectile;
    }

    [Server]
    public static Projectile FireAtEntityWithPrediction(Entity shooter, Entity target, Projectile projectile, Vector3 origin, float projectileSpeed, float shotLeadStrength = 0.65F)
    {
        Vector3 targetedPosition = Vector3.negativeInfinity;
        if (shotLeadStrength > 0)
        {
            Vector3 relativePosition = target.GetWorldPosCenter() - origin;

            float a = Vector3.Dot(target.GetVelocity(), target.GetVelocity()) - projectileSpeed * projectileSpeed; // Relative velocity over time?
            float b = 2 * Vector3.Dot(relativePosition, target.GetVelocity()); // Relative velocity
            float c = Vector3.Dot(relativePosition, relativePosition); // Constant start offset

            // Get b^2 - 4ac of quadratic formula - the "discriminant"
            float discriminant = b * b - (4 * a * c);
            if (discriminant >= 0)
            {
                float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

                float time = Mathf.Max(t1, t2);

                if (time >= 0)
                {
                    targetedPosition = target.GetWorldPosCenter() + target.GetVelocity() * shotLeadStrength * time;
                }
            }
        }

        if (targetedPosition == Vector3.negativeInfinity)
        {
            targetedPosition = target.GetWorldPosCenter();
        }

        // Prediction or not, we now have the "straight line" velocity to hit the target
            // If there's no gravity, this will be sufficient
        Vector3 projectileVelocity = (targetedPosition - origin).normalized * projectileSpeed;

        // If it does use gravity...
        if (projectile.useGravity && origin.y <= targetedPosition.y)
        {
            //projectileVelocity = (target.transform.position - origin).normalized * projectileSpeed;
            // ...we have more to do. The mathematics of this are frankly obnoxious so I'll simplify it.
            // We're just going to add vertical velocity to compensate for the effects of gravity.

            // ! NOTE - this only works with properly scaled projectiles - if the scale is not (1, 1, 1), do not use gravity on it

            // We need travel time to calculate this. Should be the same with and without prediction
            float travelTime = (targetedPosition - origin).magnitude / projectileSpeed;

            // Then use standard projectile gravity calculation to determine how much gravity will be added over flight
            float gravityTotal = 0.5F * Physics.gravity.y * (travelTime * travelTime);

            // gravityTotal = Mathf.Clamp(gravityTotal, 0, projectileVelocity.magnitude * travelTime * 2);
            projectileVelocity.y -= gravityTotal;
        }

        return Projectile.Create(projectile, origin, shooter.transform.rotation, shooter.gameObject, projectileVelocity);
    }


    public static void CopyProperties(Projectile from, Projectile to)
    {
        to.progenitor = from.progenitor;
        to.transform.position = from.transform.position;
        to.transform.rotation = from.transform.rotation;
        to.velocity = from.velocity;
        to.target = from.target;

        to.GetComponent<Rigidbody>().isKinematic = from.body.isKinematic;
        to.GetComponent<Rigidbody>().useGravity = from.body.useGravity;
    }

    public virtual void Initialize(GameObject progenitor, Vector3 velocity)
    {
        this.progenitor = progenitor;
        this.velocity = velocity;

        transform.LookAt(transform.position + velocity.normalized);
    }

    public void SetDamage(int damage)
    {
        damageOnHit = damage;
    }

    public void SetAOEDamage(int damage)
    {
        aoeDamageOverridden = true;

        aoeDamage = damage;
    }

    public void SetImpact(int impact)
    {
        this.impact = impact;
    }

    public bool Stuck()
    {
        return stuck;
    }


    public void SetInaccuracy(float inaccuracy)
    {
        Vector3 localizedVelocity = transform.InverseTransformVector(velocity);
        Vector2 randomization = 0.5F * inaccuracy * velocity.magnitude * UnityEngine.Random.insideUnitCircle;
        randomization.Scale(inaccuracyMult);

        localizedVelocity += (Vector3)randomization;

        SetVelocity(transform.TransformVector(localizedVelocity));
    }


    // override to perform more setup specific to a projectile
    // call base.Start() when overriding
    public virtual void Start()
    {
        body = GetComponent<Rigidbody>();
        if (isServer)
        {
            body.useGravity = useGravity;

            if (velocity.IsNaN())
            {
                Debug.LogError("NaN velocity given to projectile");
                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
            }
            
            body.linearVelocity = velocity;
            body.linearDamping = 0;
            body.angularDamping = 0;

            projectileStartTime = Time.time;
            lifespanRemaining = lifespanSeconds;
            if (randomLifespan)
            {
                lifespanRemaining = UnityEngine.Random.Range(lifespanSeconds, randomLifespanMax);
            }

            SetInaccuracy(inaccuracy);
        }
        else
        {
            body.isKinematic = true;
        }

        if (isClient)
        {
            AudioManager.Singleton.PlayOneShot(initialSound, transform.position);

            if (!flightSound.IsNull)
            {
                flightEventInstance = RuntimeManager.CreateInstance(flightSound);
                flightEventInstance.start();

                RuntimeManager.AttachInstanceToGameObject(flightEventInstance, transform);
            }
        }
    }

    public virtual void Update()
    {
        if (isServer)
        {
            lifespanRemaining -= Time.deltaTime;
            if (active && hasLifespan && lifespanRemaining <= 0)
            {
                EndFlight();
            } 


            Physics.SyncTransforms();
            CheckRaycastHits();
        }
    }

    public virtual void FixedUpdate() 
    {
        if (isServer)
        {
            transform.LookAt(transform.position + body.linearVelocity.normalized);

            CheckRaycastHits();
        }
    }

    void CheckRaycastHits()
    {
        if (Physics.Raycast(transform.position, body.linearVelocity, out RaycastHit hitInfo, body.linearVelocity.magnitude * Time.deltaTime * 1.5F))
        {
            if (CanHitCollider(hitInfo.collider))
            {
                HitCollider(hitInfo.collider);
            }
        }
    }

    public void SetTarget(GameObject target)
    {
        this.target = target;
        Collider targetCollider = target.GetComponent<Collider>();
        Vector3 targetPosition = targetCollider == null ? target.transform.position : targetCollider.bounds.center;

        transform.LookAt(targetPosition);
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (isServer)
        {
            if (!CanHitCollider(collider))
            {
                return;
            }

            OnCollide?.Invoke();
            RpcCollide();

            HitCollider(collider);
        }

    }

    [ClientRpc]
    void RpcCollide()
    {
        AudioManager.Singleton.PlayOneShot(collisionSound, transform.position);
    }

    bool CanHitCollider(Collider collider)
    {
        return isServer
            && active
            && collider.gameObject != progenitor
            && Misc.IsInLayerMask(collider.gameObject, LayerMask.GetMask("Entities", "Hurtboxes", "Default")); 
    }

    public virtual void HitCollider(Collider collider)
    {
        Damageable potentialDamageable = collider.GetComponentInParent<Damageable>();

        if (potentialDamageable != null)
        {
            Entity progenAsEntity = null;
            if (progenitor != null) 
            {
                progenAsEntity = progenitor.GetComponentInParent<Entity>(); // could be null
            }

            Entity hitEntity = potentialDamageable.GetDamageableEntity();
            if (hitEntity != null)
            {
                // Processing-stopping factors
                if (hitEntities.Contains(hitEntity)
                    || progenAsEntity == null
                    || (progenAsEntity.IsAlly(hitEntity) && passThroughAllies)
                    || hitEntity.IsInvulnerable())
                {
                    return;
                }
            }

            bool forceCritical = false;
            if (potentialDamageable is Hurtbox hurtbox && hurtbox.projectileAutoCritical)
            {
                forceCritical = true;
            }

            if (damageOnHit > 0 &&
                    (progenAsEntity == null || hitEntity == null || !progenAsEntity.IsAlly(hitEntity)))
            {
                HitInfo hitInfo = potentialDamageable.Damage(damageOnHit, damageType, hitType, progenAsEntity, forceCritical:forceCritical, impact:impact);

                if (hitEntity != null)
                {
                    hitEntities.Add(hitEntity);
                }

                OnHit?.Invoke(this, hitInfo);
            }
        }

        if (sticky && !stuck)
        {
            StartCoroutine(Stick(collider.gameObject));
        }
        else if (!persistAfterHit)
        {
            EndFlight();
        }
    }

    IEnumerator Stick(GameObject stuckObject)
    {
        stuck = true;
        
        if (stickyTimeToStop > 0)
        {
            yield return new WaitForSeconds(stickyTimeToStop);
        }

        if (stuckObject == null)
        {
            EndFlight();
            yield return null;
        }

        Entity collidedEntity = stuckObject.GetComponentInParent<Entity>();


        if (NetworkServer.active)
        {
            ParentConstraint constraint = gameObject.AddComponent<ParentConstraint>();

            ConstraintSource source = new();
            source.sourceTransform = stuckObject.transform;
            constraint.AddSource(source);

            constraint.constraintActive = true;
            //transform.parent = stuckObject.transform;
        }
        //stuckTransform = gameObject.transform;
        //stuckOffset = stuckTransform.InverseTransformPoint(transform.position);
        //stuckRotation = Quaternion.Inverse(stuckTransform.rotation) * 

        active = false;

        body.useGravity = false;

        body.linearVelocity = new();
        velocity = new();

        OnStick?.Invoke(collidedEntity);
        OnStickUnityEvent?.Invoke();

        RpcStick();

        if (stickyAdditionalLifespan > 0)
        {
            yield return new WaitForSeconds(stickyAdditionalLifespan);

            EndFlight();
        }
        else
        {
            // Must be cleaned up manually, bit signal that the flight has ended
            CreateAOE();

            OnFlightEnd?.Invoke(this);
        
            active = false;
        }
    }

    [ClientRpc]
    void RpcStick()
    {
        if (!flightSound.IsNull)
        {
            flightEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    void CreateAOE()
    {
        if (explosion != null)
        {
            AreaOfEffect.AOEProperties properties = AreaOfEffect.Create(explosion, transform.position, progenitor.GetComponent<Entity>());

            if (aoeDamageOverridden)
            {
                properties.damage = aoeDamage;
            }

            OnExplosionCreated?.Invoke(properties);
        }
    }

    [ClientRpc]
    void RpcReparent(Transform newParent)
    {
        transform.SetParent(newParent);
    }

    public void EndFlight()
    {
        OnFlightEnd?.Invoke(this);

        
        active = false;

        CreateAOE();
        
        RpcEndFlight();


        if (destroyOnFlightEnd)
        {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }

    }

    [ClientRpc]
    void RpcEndFlight()
    {
        if (!hitSound.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(hitSound, transform.position);
        }

        if (!flightSound.IsNull)
        {
            flightEventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    void OnDestroy()
    {
        if (!flightSound.IsNull && flightEventInstance.isValid())
        {
            flightEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    public float GetLifespan()
    {
        return lifespanSeconds;
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.velocity = velocity;
    }


}

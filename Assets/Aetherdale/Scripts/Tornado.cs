using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Tornado : NetworkBehaviour, IVelocitySource
{
    [SerializeField] float lifespan = 20.0F;
    [SerializeField] EventReference soundLoop;

    [Header("Entity Pull")]
    [SerializeField] float pullRadius = 10.0F;
    [SerializeField] float maxPullStrength = 15.0F;
    [SerializeField] AnimationCurve pullStrengthCurve;

    [Header("Movement")]
    public float desiredVelocity = 8;
    public float yRotationPerSecond = 20.0F;


    public Entity origin;
    public float velocity = 0;

    const float VELOCITY_LERP = 0.8F;

    Rigidbody body;
    float startTime;

    EventInstance noiseInstance;

    public void Start()
    {
        body = GetComponent<Rigidbody>();
        if (isServer)
        {
            SphereCollider coll = gameObject.AddComponent<SphereCollider>();
            coll.radius = pullRadius;
            coll.isTrigger = true;
            coll.gameObject.layer = LayerMask.NameToLayer("Hitboxes");

            startTime = Time.time;
        }

        if (isClient)
        {
            noiseInstance = RuntimeManager.CreateInstance(soundLoop);
            RuntimeManager.AttachInstanceToGameObject(noiseInstance, transform);
            noiseInstance.start();
        }
    }

    public void Update()
    {
        if (isServer)
        {
            velocity = Mathf.Lerp(velocity, desiredVelocity, VELOCITY_LERP * Time.deltaTime);

            transform.Rotate(new(0, yRotationPerSecond * Time.deltaTime, 0));
            
            body.linearVelocity = transform.forward * velocity;

            if ((Time.time - startTime) > lifespan)
            {
                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
            }
        }
    }

    void OnDestroy()
    {
        noiseInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        noiseInstance.release();
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Entity entity) && entity.TryGetComponent(out Rigidbody rigidbody))
        {
            // if (origin.IsEnemy(entity))
            // {
                entity.velocitySources.Add(gameObject);
            // }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Entity entity) && entity.TryGetComponent(out Rigidbody rigidbody))
        {
            // if (origin.IsEnemy(entity))
            // {
                entity.velocitySources.Remove(gameObject);
            // }
        }
    }

    public Vector3 GetVelocityApplied(Entity entity)
    {
        Vector3 pullDirection = entity.transform.position - transform.position;
        float strength = Mathf.Clamp(maxPullStrength * pullStrengthCurve.Evaluate(Mathf.Clamp01(pullDirection.magnitude / pullRadius)), 0, maxPullStrength);
        return -pullDirection.normalized * strength;
    }


#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }

#endif
}

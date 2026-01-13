using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class Laser : NetworkBehaviour
{
    [SerializeField] int numberOfPositions = 2;
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] VisualEffect originVFX;
    [SerializeField] VisualEffect endpointVFX;
    [SerializeField] EventReference startSound;
    [SerializeField] EventReference fireSound;

    [SyncVar] Vector3 originPoint = new();
    [SyncVar] Vector3 endPoint = new();

    // Set by entity
    Entity owningEntity;
    public float maxLength = 16.0F;
    public int damagePerHit = 0;
    public int impactPerHit = 0;
    public Element damageType = Element.Physical;
    public HitType hitType = HitType.Ability;
    public float hitInterval = 0.25F;
    public float lifespan = 0;


    // runtime
    float lastHit = -100;
    float startTime = 0;


    [Server]
    public static Laser Create(Laser laserPrefab, Vector3 origin, Vector3 endPos, Quaternion rotation, Entity owningEntity, int damagePerHit, Element damageType, float hitInterval, float maxLength, int impactPerSecond = 0, float lifespan = 0)
    {
        Laser laserInstance = Instantiate(laserPrefab, origin, rotation);
        laserInstance.SetHitData(owningEntity, damagePerHit, damageType, hitInterval, maxLength, impactPerSecond);
        NetworkServer.Spawn(laserInstance.gameObject);
        laserInstance.SetPositions(origin, endPos);

        laserInstance.lifespan = lifespan;

        return laserInstance;
    }


    public void SetHitData(Entity owningEntity, int damagePerTick, Element damageType, float hitInterval, float maxLength = 0.0F, int impactPerSecond = 0)
    {
        this.owningEntity = owningEntity;
        this.damagePerHit = damagePerTick;
        this.damageType = damageType;
        this.hitInterval = hitInterval;
        this.maxLength = maxLength;
        this.impactPerHit = impactPerSecond;
    }

    public virtual void Start()
    {
        startTime = Time.time;
        AudioManager.Singleton.PlayOneShot(startSound, originPoint);
    }


    public virtual void FixedUpdate()
    {
        UpdatePositions();

        if (!NetworkServer.active)
        {
            return;
        }
  
        if (lifespan > 0)
        {
            if (Time.time - startTime > lifespan)
            {
                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
                return;
            }
        }

        if (Time.time - lastHit >= hitInterval)
        {
            Fire();
        }
    }

    public void Fire()
    {
        Vector3 vector = endPoint - originPoint;
        Vector3 direction = vector.normalized;
        lastHit = Time.time;

        foreach (VisualEffect vfx in GetComponentsInChildren<VisualEffect>())
        {
            vfx.SendEvent("Fire"); 
        }

        if (Physics.Raycast(originPoint, direction, out RaycastHit hit, vector.magnitude + 0.5F, LayerMask.GetMask("Entities", "Default")))
        {
            if (hit.collider.TryGetComponent(out Entity entity))
            {
                if (owningEntity != null && owningEntity.IsAlly(entity))
                {
                    return;
                }

                if (damagePerHit > 0)
                {
                    if (damagePerHit < 1)
                    {
                        damagePerHit = 1;
                    }

                    entity.Damage(damagePerHit, damageType, hitType, owningEntity, impactPerHit);
                }
            }
        }

        RpcFire();

    }

    [ClientRpc]
    void RpcFire()
    {
        AudioManager.Singleton.PlayOneShot(fireSound, originPoint + ((endPoint - originPoint) / 2));
    }

    [Server]
    public void SetPositions(Vector3 origin, Vector3 end)
    {
        originPoint = origin;
        endPoint = end;
    }

    public virtual void UpdatePositions()
    {
        Vector3 intendedVector = endPoint - originPoint;

        float length = Mathf.Infinity;
        if (maxLength >=0)
        {
            length = maxLength;
        }
        //Debug.Log(originPoint + " to " + endPoint);

        if (Physics.Raycast(originPoint, intendedVector, out RaycastHit hit, length, LayerMask.GetMask("Entities", "Default")))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = originPoint + (intendedVector.normalized * length);
        }
        

        List<Vector3> positions = new ();
        positions.Add(originPoint);
        {
            int intermediaryPositionsNeeded = numberOfPositions - 2;
            if (intermediaryPositionsNeeded > 0)
            {
                for (int i = 0; i < intermediaryPositionsNeeded; i++)
                {
                    positions.Add(Vector3.Lerp(originPoint, endPoint, (i + 1) / (numberOfPositions - 1.0F)));
                }
            }  
        }
        positions.Add(endPoint);

        
        // Handle LineRenderer lines
        if (lineRenderer != null)
        {
            lineRenderer.SetPositions(positions.ToArray());
        }

        // Handle child transform-based lines
        int index = 1;
        while(transform.Find($"Pos{index}") is Transform childPos && childPos != null)
        {
            childPos.position = positions[index - 1];

            index++;
        }

        // Set VFX of endpoints to correct positions
        if (originVFX != null) originVFX.transform.position = originPoint;
        if (endpointVFX != null) endpointVFX.transform.position = endPoint;
    }
}

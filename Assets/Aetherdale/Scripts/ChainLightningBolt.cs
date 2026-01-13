using System;
using FMODUnity;
using Mirror;
using UnityEngine;

public class ChainLightningBolt : NetworkBehaviour
{
    public EventReference soundEffect;

    [SyncVar] Entity origin;

    [SyncVar] Entity destination;

    float lifespanRemaining;

    LineRenderer lineRenderer;

    public Action OnLifespanRanOut;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        if (origin == null || destination == null)
        {
            if (isServer)
            {
                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
            }

            return;
        }
        
        Vector3 center = Vector3.Lerp(origin.transform.position, destination.transform.position, 0.5F);
        AudioManager.Singleton.PlayOneShot(soundEffect, center);
    }

    [Server]
    public void SetData(float lifespan, Entity origin, Entity destination)
    {
        lifespanRemaining = lifespan;
        
        this.origin = origin;
        this.destination = destination;
    }

    // Update is called once per frame
    void Update()
    {
        if (origin != null && destination != null)
        {
            Vector3 middleFirstPosition = Vector3.Lerp(origin.GetWorldPosCenter(), destination.GetWorldPosCenter(), 0.33F);
            Vector3 middleSecondPosition = Vector3.Lerp(origin.GetWorldPosCenter(), destination.GetWorldPosCenter(), 0.66F);
            Vector3[] positions = new Vector3[4];

            float variance1 = 2.5F - (Mathf.PerlinNoise(932759101, Time.time * 12) * 3F);
            float variance2 = 2.5F - (Mathf.PerlinNoise(452012935, Time.time * 12) * 3F);

            positions[0] = origin.GetWorldPosCenter();
            positions[1] = new(middleFirstPosition.x, middleFirstPosition.y + variance1, middleFirstPosition.z);
            positions[2] = new(middleSecondPosition.x, middleSecondPosition.y + variance2, middleSecondPosition.z);
            positions[3] = destination.GetWorldPosCenter();

            lineRenderer.SetPositions(positions);

        }

        if (NetworkServer.active)
        {
            lifespanRemaining -= Time.deltaTime;
            //Debug.Log((lifespanRemaining <= 0).ToString() + " and " + (origin == null).ToString()  + " and " +  (destination == null).ToString());
            if (lifespanRemaining <= 0 || origin == null || destination == null)
            {
                OnLifespanRanOut?.Invoke();
                
                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
            }
        }
    }

}

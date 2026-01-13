using Mirror;
using UnityEngine;

public class MovingTerrain : NetworkBehaviour, IVelocitySource
{
    public bool movementEnabled = true;

    [SerializeField] protected Vector3 terrainVelocity;


    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity) && entity.TryGetComponent(out Rigidbody rigidbody))
        {
            //entity.TargetAddExternalVelocity(terrainVelocity * rigidbody.mass);
            entity.velocitySources.Add(gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Entity entity) && entity.TryGetComponent(out Rigidbody rigidbody))
        {
            //entity.TargetAddExternalVelocity(-(terrainVelocity * rigidbody.mass));
            entity.velocitySources.Remove(gameObject);
        }
    }

    public Vector3 GetVelocityApplied(Entity entity)
    {
        return terrainVelocity;
    }
}

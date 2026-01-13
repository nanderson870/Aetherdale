using UnityEngine;

public interface IVelocitySource
{
    GameObject gameObject { get; }
    public Vector3 GetVelocityApplied(Entity entity);
}

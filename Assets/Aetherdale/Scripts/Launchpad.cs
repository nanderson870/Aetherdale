using System.Collections.Generic;
using UnityEngine;

public class Launchpad : MonoBehaviour
{
    public Vector3 velocity;

    [SerializeField] int displayNumberOfSegments = 10;
    [SerializeField] float segmentLengthSeconds = 0.25F;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out ControlledEntity controlledEntity))
        {
            controlledEntity.Push(velocity, forceMode:ForceMode.VelocityChange);
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = (Color.red + Color.yellow) / 2;

        List<Vector3> points = new();

        ProjectileArc arc = new(transform.position, velocity);

        for (int i = 0; i < displayNumberOfSegments; i++)
        {
            points.Add(arc.Calculate(i * segmentLengthSeconds)); // 1 segment, 1 second
        }

        for (int j = 0; j < points.Count - 1; j++)
        {
            Gizmos.DrawLine(points[j], points[j + 1]);
        }
    }
}

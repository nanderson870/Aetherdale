using UnityEngine;

public class Butterfly : MonoBehaviour
{
    Rigidbody body;
    readonly float speed = 2F;
    readonly int numPathsConsidered = 3;
    readonly float maxDistanceConsidered = 10;
    readonly float nearGroundDistance = 6.0F;

    Vector3 currentDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateDirection();

        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        body.linearVelocity = Vector3.Lerp(body.linearVelocity, currentDirection * speed, 0.5F * Time.deltaTime);
        transform.forward = body.linearVelocity.normalized;
    }

    
    void OnCollisionEnter(Collision collision)
    {
        CancelInvoke();
        UpdateDirection();
    }

    void UpdateDirection()
    {
        currentDirection = GetNewDirection();
        Invoke(nameof(UpdateDirection), Random.Range(3, 10));
    }

    Vector3 GetNewDirection()
    {
        bool nearGround = Physics.Raycast(transform.position, Vector3.down, nearGroundDistance, LayerMask.GetMask("Default"));

        // 1. Raycast random different directions and choose either: A) any distance over 10m or B) the longest
        RaycastHit[] hits = new RaycastHit[numPathsConsidered];
        for (int i = 0; i < numPathsConsidered; i++)
        {
            Vector3 direction = Random.insideUnitSphere;

            if (!nearGround && direction.y > 0)
            {
                direction.y *= -1;
            }

            Physics.Raycast(transform.position, direction, out RaycastHit hit, 100, LayerMask.GetMask("Default"));

            hits[i] = hit;
        }

        bool allWithinMaxConsidered = true;
        for (int i = 0; i < numPathsConsidered; i++)
        {
            if (hits[i].distance > maxDistanceConsidered)
            {
                allWithinMaxConsidered = false;
                break;
            }
        }

        if (allWithinMaxConsidered)
        {
            return (hits[Random.Range(0, numPathsConsidered)].point - transform.position).normalized;
        }

        // Otherwise we will determine which is longest and return that

        int furthestHitIndex = 0;
        for (int i = 1; i < numPathsConsidered; i++)
        {
            if (hits[i].distance > hits[furthestHitIndex].distance)
            {
                furthestHitIndex = i;
            }
        }

        return (hits[furthestHitIndex].point - transform.position).normalized;
    }

}

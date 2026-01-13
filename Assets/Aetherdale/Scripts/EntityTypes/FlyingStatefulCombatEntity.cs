
// Class specifically for flying SCEs. We may eventually just merge this into SCE

using Mirror;
using UnityEngine;

public class FlyingStatefulCombatEntity : StatefulCombatEntity
{
    float preferredHeight;

    Vector3 velocity = new();

    public override void Start()
    {
        base.Start();

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;

        body.isKinematic = false;
        body.useGravity = false;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        Vector3 targetPos = navMeshAgent.nextPosition;

        Vector3 offsetFromTarget = targetPos - transform.position;

        velocity = body.linearVelocity;

        // if (offsetFromTarget.magnitude > 0.3F)
        // {
        //     Vector3 direction = offsetFromTarget.normalized;

        //     Vector3 horizontal = direction * navMeshAgent.speed;
        //     velocity.x = horizontal.x;
        //     velocity.z = horizontal.z;

        //     velocity.y = GetVerticalVelocity();
        // }
        // else
        // {
        //     velocity = Vector3.zero;
        // }

        body.linearVelocity = Vector3.Lerp(body.linearVelocity, velocity, navMeshAgent.acceleration);
    }

    public override void LateUpdate()
    {
        base.LateUpdate();


        if (body.linearVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(body.linearVelocity.normalized);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, 3.0F * Time.deltaTime);
        }
    }


    float GetVerticalVelocity()
    {
        float heightOffset = preferredHeight - transform.position.y;
        if (Mathf.Abs(heightOffset) > 0.3F)
        {

            float yDir = Mathf.Clamp(heightOffset, -1, 1);

            float yVel = Mathf.Sqrt(navMeshAgent.speed) * yDir;

            yVel *= (Mathf.Abs(heightOffset) / Mathf.Clamp(navMeshAgent.remainingDistance, 1, 20));

            return yVel;
        }
        else
        {
            return 0;
        }
    }


    public override float TurnTowards(Vector3 position, float rotationSpeed)
    {
        Vector3 targetLookDirection = (position - transform.position).normalized;
        Vector3 currentLookDirection = transform.forward;

        Vector3 actualLookDirection = Vector3.Lerp(currentLookDirection, targetLookDirection, rotationSpeed * Time.deltaTime);

        transform.LookAt(transform.position + actualLookDirection);

        return 0;
    }
    
    public void SetPreferredHeight(float offset)
    {
        preferredHeight = offset;
    }

    public override Vector3 GetPreferredPositionFromTarget(Entity target, out float speed)
    {
        speed = 0; //default speed

        Vector3 directionToSelf = transform.position - target.transform.position;

        Vector3 position = target.GetWorldPosCenter() + directionToSelf.normalized * minAttackRange;

        return position;
    }
}
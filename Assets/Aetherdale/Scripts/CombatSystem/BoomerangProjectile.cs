

using UnityEngine;

public class BoomerangProjectile : Projectile
{
    [SerializeField] float outwardSeconds=3.0F;
    [SerializeField] AnimationCurve velocityTaperCurve;
    [SerializeField] float returnAcceleration = 2.0F;


    bool returning = false;
    Vector3 initialVelocity = Vector3.zero;

    public override void Initialize(GameObject progenitor, Vector3 velocity)
    {
        base.Initialize(progenitor, velocity);

        initialVelocity = velocity;

        OnCollide += Reverse;
    }

    void Reverse()
    {
        returning = true;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!returning) // still going outward
        {
            float outwardAge = (Time.time - projectileStartTime) / outwardSeconds;
            body.linearVelocity = initialVelocity * velocityTaperCurve.Evaluate(Mathf.Clamp01(outwardAge));

            if (outwardAge >= 1.0F)
            {
                returning = true;
            }
        }
        else
        {
            float magnitude = body.linearVelocity.magnitude;
            magnitude += returnAcceleration * Time.deltaTime;

            Vector3 destination = progenitor.transform.position + new Vector3(0, 1.5F, 0);

            Vector3 direction = -initialVelocity;
            if (progenitor != null)
            {
                direction = destination - transform.position;
            }
            body.linearVelocity = direction.normalized * magnitude;

            if (Vector3.Distance(transform.position, destination) < magnitude * Time.deltaTime * 2)
            {
                EndFlight();
            }
        }
    }


}
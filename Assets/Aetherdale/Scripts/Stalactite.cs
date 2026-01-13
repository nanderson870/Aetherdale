using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;


[RequireComponent(typeof(Rigidbody))]
public class Stalactite : NetworkBehaviour
{
    [SerializeField] int baseDamage = 10;
    [SerializeField] int baseImpact = 100;
    [SerializeField] Element element = Element.Physical;
    [SerializeField] GameObject[] ignoreCollisionWith;
    [SerializeField] VisualEffect collisionVFX;

    Vector3 originalPosition;
    Rigidbody body;
    Hitbox hitbox;

    public bool Falling {internal set; get;} = false;

    public static void LooseStalactites(int quantity = 0)
    {
        Stalactite[] stalactites = FindObjectsByType<Stalactite>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (quantity == 0)
        {
            quantity = stalactites.Count();
        }

        for (int i = 0; i < quantity; i++)
        {
            Stalactite stalactiteToLoose = stalactites[Random.Range(0, stalactites.Length)];
            stalactiteToLoose.Fall();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = transform.position;    
        body = GetComponent<Rigidbody>();
        hitbox = GetComponentInChildren<Hitbox>();

    }

    void Update()
    {
        if (Falling && GetDistanceTravelled() > 150.0F)
        {
            Reset();
        }
    }

    float GetDistanceTravelled()
    {
        return originalPosition.y - transform.position.y;
    }

    public void Fall()
    {
        body.isKinematic = false;
        body.useGravity = true;

        Falling = true;
        
        hitbox.StartHit(baseDamage, element, HitType.Environment, null, baseImpact);
    }


    void Reset()
    {   
        body.isKinematic = true;
        body.useGravity = false;

        hitbox.EndHit();

        transform.position = originalPosition;
        Falling = false;
    }

}

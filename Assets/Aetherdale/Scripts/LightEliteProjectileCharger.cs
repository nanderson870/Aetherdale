using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class LightEliteProjectileCharger : NetworkBehaviour
{
    const float TARGET_RANGE = 25.0F; // targets all enemies within this range

    public float chargeTime = 4;

    public float chargeTimeRemaining = 0;

    public float initialVelocity = 50.0F;

    public SeekingProjectile projectilePrefab;

    public Entity owner;

    [SerializeField] EventReference chargeNoise;

    VisualEffect visualEffectObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        chargeTimeRemaining = chargeTime;
        visualEffectObject = GetComponent<VisualEffect>();
    }

    void Start()
    {
        AudioManager.Singleton.PlayOneShot(chargeNoise, transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        chargeTimeRemaining -= Time.deltaTime;

        if (owner == null)
        {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }

        if (chargeTimeRemaining > 0)
            {
                visualEffectObject.SetFloat("Charge", Mathf.Clamp01((chargeTime - chargeTimeRemaining) / chargeTime));
            }
            else
            {
                List<Entity> targets = owner.GetNearbyEnemies(TARGET_RANGE, alternateOriginTransform: transform);
                foreach (Entity target in targets)
                {
                    Fire(target);
                }

                NetworkServer.UnSpawn(gameObject);
                Destroy(gameObject);
            }
    }


    void Fire(Entity target)
    {
        SeekingProjectile.Create(projectilePrefab, transform.position, owner.gameObject, target, initialVelocity);
    }
}

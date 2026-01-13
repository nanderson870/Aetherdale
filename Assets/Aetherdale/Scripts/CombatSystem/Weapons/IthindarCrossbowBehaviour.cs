

using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class IthindarCrossbowBehaviour : CrossbowBehaviour
{
    public float minCharge = 0F;
    public float maxCharge = 1.5F;
    public float laserLength = 600.0F;

    public EventReference chargeEventReference;

    public Laser maxChargeLaser;

    public VisualEffect chargeVisualEffect;

    bool charging = false;
    bool chargeSoundPlayed = false;
    float currentChargeTime = 0;

    
    EventInstance chargeEventInstance;

    public override void Start()
    {
        base.Start();

        chargeEventInstance = RuntimeManager.CreateInstance(chargeEventReference);
    }

    public override void Update()
    {
        base.Update();

        //Debug.Log(charging + " " + currentChargeTime);
        if (charging)
        {
            currentChargeTime += Time.deltaTime;
        }
        else
        {
            currentChargeTime = 0;
        }
    }


    [Server]
    public override bool PerformAttack1()
    {
        float interval = GetAttackInterval() / wielder.GetStat(Stats.AttackSpeed);
        // Check if we're currently timed out of making another attack
        float timeSinceLastAttack = Time.time - lastAttack;
        if (timeSinceLastAttack < interval * 0.75F)
        {
            return false;
        }
        lastAttack = Time.time;

        StartCharging();
        return true;
    }

    [Server]
    public override bool ReleaseAttack1()
    {
        if (currentChargeTime > maxCharge)
        {
            Vector3 targetPos = wielder.GetAimedPosition();
            FinishCharging();
            FireLaser(targetPos);
        }
        else if (currentChargeTime > minCharge)
        {
            FinishCharging();
            Fire();
        }
        else
        {
            CancelCharging();
        }

        return true;
    }

    [Server]
    public void FireLaser(Vector3 targetPos)
    {
        Transform projectileSpawnTransform = transform.Find("AmmoHolder");
        Laser.Create(
            maxChargeLaser,
            projectileSpawnTransform.position,
            targetPos,
            projectileSpawnTransform.rotation,
            wielder.gameObject.GetComponent<Entity>(),
            wielder.GetAttackDamage() * 3,
            Element.Dark,
            hitInterval:1F,
            laserLength,
            impactPerSecond:0,
            lifespan:0.25F
        );
    }

    /// <summary>
    /// Start charging up
    /// </summary>
    [Server]
    public void StartCharging()
    {
        charging = true;
        RpcSendVisualEffectEvent("StartCharge");

        if (!chargeSoundPlayed)
        {
            RuntimeManager.AttachInstanceToGameObject(chargeEventInstance, transform);
            chargeEventInstance.start();
            chargeSoundPlayed = true;
        }

        chargeEventInstance.getDescription(out EventDescription description);
        description.getLength(out int lengthMilliseconds);

        //Debug.Log(lengthMilliseconds);
    }

    /// <summary>
    /// Mark charging cancelled - i.e. no shot fired, spool down
    /// </summary>
    [Server]
    public void CancelCharging()
    {
        charging = false;
        currentChargeTime = 0;
        RpcSendVisualEffectEvent("AbortCharge");

        chargeEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        chargeSoundPlayed = false;
    }

    /// <summary>
    /// Mark charging completed - i.e. we're firing
    /// </summary>
    [Server]
    public void FinishCharging()
    {
        charging = false;
        currentChargeTime = 0;
        RpcSendVisualEffectEvent("Fire");

        chargeEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        chargeSoundPlayed = false;
    }

    [ClientRpc]
    void RpcSendVisualEffectEvent(string eventName)
    {
        chargeVisualEffect.SendEvent(eventName);
    }
}
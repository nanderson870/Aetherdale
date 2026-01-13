using System.Collections;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class Evonix : Boss
{
    [SerializeField] VisualEffect staffTopVfx;

    // Spec 1: Rapidfire Calldown
    [SerializeField] AreaOfEffect rapidFireLightningBolt;
    readonly int rapidFireBoltDamage = 15;
    readonly float rapidFireLightningRange = 100.0F;
    readonly float rapidFireLightningCooldown = 15.0F;
    readonly int numRapidFireLightnings = 15;
    readonly float rapidFireLightningInterval = 0.25F;

    float lastRapidFireLightning = 0;
    

    // Spec 2/Regular attack?: Laser Bolt
    [SerializeField] EventReference laserBoltTelegraphSound;
    [SerializeField] Transform laserBoltOrigin;
    [SerializeField] OneshotLaser laserBoltInstance;
    readonly int laserBoltDamage = 25;
    readonly float laserBoltRange = 45.0F;
    readonly float laserBoltCooldown = 1.0F;
        
    float lastLaserBolt = 0;


    // Spec 3: Tornado Summon
    [SerializeField] Tornado tornadoPrefab;
    [SerializeField] AreaOfEffect tornadoSummonAOEPrefab;
    readonly float tornadoInitialVelocity = 5;
    readonly float tornadoDesiredVelocity = 4;
    readonly int numTornadoes = 4;
    readonly float tornadoSummonCooldown = 25;
    readonly float tornadoSummonRange = 15;
    float lastTornadoSummon;



    public override void Start()
    {
        base.Start();

        lastRapidFireLightning = Time.time;
        lastLaserBolt = Time.time + 5.0F;
    }


    public override bool CanAttack(Entity target)
    {
        Debug.Log("LB? " + CanLaserBolt(target));
        if (CanTornadoSummon(target)) return true;

        else if (CanRapidFireLightning(target)) return true;

        else if (CanLaserBolt(target)) return true;

        else return false;
        //return base.CanAttack(target);
    }



    public override void Attack(Entity target)
    {
        if (CanTornadoSummon(target)) TornadoSummon(target);
        else if (CanRapidFireLightning(target)) RapidFireLightning(target);
        else if (CanLaserBolt(target)) LaserBolt(target);
        else base.Attack();

    }


#region RAPIDFIRE IMPL

    public bool CanRapidFireLightning(Entity target)
    {
        return SeesEntity(target)
            && (Time.time - lastRapidFireLightning) >= rapidFireLightningCooldown
            && Vector3.Distance(target.transform.position, transform.position) <= rapidFireLightningRange;
    }

    public void RapidFireLightning(Entity target)
    {
        PlayAnimation("RapidFireLightning", 0.05F);
        StartCoroutine(RapidFireLightningCoroutine(target));

        lastRapidFireLightning = lastAttack = Time.time;
        
    }

    IEnumerator RapidFireLightningCoroutine(Entity target)
    {
        int remaining = numRapidFireLightnings;
        while (remaining > 0)
        {
            yield return new WaitForSeconds(rapidFireLightningInterval);

            AreaOfEffect.AOEProperties props = AreaOfEffect.Create(rapidFireLightningBolt, target.transform.position + new Vector3(0, 0.5F, 0), this, HitType.Ability, 20);
            props.damage = rapidFireBoltDamage;

            remaining--;
        }
    }
#endregion

#region LASER BOLT IMPL

    public bool CanLaserBolt(Entity target)
    {
        Debug.Log(laserBoltTarget == null);
        return laserBoltTarget == null
            && SeesEntity(target)
            && (Time.time - lastLaserBolt) >= laserBoltCooldown
            && Vector3.Distance(target.transform.position, transform.position) <= laserBoltRange;
    }

    [SyncVar] Entity laserBoltTarget = null;
    public void LaserBoltTelegraph()
    {
        staffTopVfx.SendEvent("Pulse");
        AudioManager.Singleton.PlayOneShot(laserBoltTelegraphSound, staffTopVfx.transform.position);
    }

    public void LaserBolt(Entity target)
    {
        if (isServer)
        {
            laserBoltTarget = target;
            SetAnimatorTrigger("StartLaserBolt"); 
        }
    }

    public void LaserBoltFire()
    {
        if (isServer)
        {
            Vector3 direction = (laserBoltTarget.GetWorldPosCenter() - laserBoltOrigin.position).normalized;
            laserBoltInstance.SetPositions(laserBoltOrigin.position, laserBoltTarget.GetWorldPosCenter() + direction.normalized);
            laserBoltInstance.damagePerHit = laserBoltDamage;
            laserBoltInstance.damageType = Element.Storm;
            laserBoltInstance.Fire();
            
            lastLaserBolt = lastAttack = Time.time;
            laserBoltTarget = null;

            
            SetAnimatorTrigger("StopLaserBolt");
        }
    }
#endregion


#region TORNADO SUMMON IMPL
    public bool CanTornadoSummon(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) < tornadoSummonRange && (Time.time - lastTornadoSummon) >= tornadoSummonCooldown;
    }

    public void TornadoSummon(Entity target)
    {
        PlayAnimation("TornadoSummon", 0.05F);
        lastTornadoSummon = Time.time;
        attacking = true;
        lastAttack = Time.time;
    }


    public void TornadoSummonHit(Entity target)
    {
        AreaOfEffect.Create(tornadoSummonAOEPrefab, transform.position, this, HitType.Ability, 100);

        float degreesPerTornado = 360F / numTornadoes;
        for (int i = 0; i < numTornadoes; i++)
        {
            Vector3 direction = Quaternion.Euler(0, (0.5F * degreesPerTornado) + (degreesPerTornado * i), 0) * transform.forward;
            Tornado tornado = Instantiate(tornadoPrefab, transform.position, Quaternion.identity);

            tornado.transform.forward = direction;

            tornado.velocity = tornadoInitialVelocity;
            tornado.desiredVelocity = tornadoDesiredVelocity;

            tornado.origin = this;

            NetworkServer.Spawn(tornado.gameObject);
        }
    }
#endregion
}

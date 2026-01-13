using Aetherdale;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class RunewaspDrone : FlyingStatefulCombatEntity
{
    [Header("Attack")]
    [SerializeField] int attackDamage = 15;
    [SerializeField] Hitbox stingerHitbox;


    [Header("Spit Attack")]
    const float SPIT_PROJECTILE_SPEED = 15.0F;
    [SerializeField] float spitRange = 40.0F;
    [SerializeField] float spitCooldown = 8.0F;
    [SerializeField] Projectile spitProjectile;
    [SerializeField] Transform spitOrigin;

    [SerializeField] AlphabetSymbol[] runicAlphabetSymbols;

    EventInstance idleSoundInstance;

    Entity spitTarget = null;
    float lastSpit = -5.0F;

    public override void Start()
    {
        base.Start();

        for (int i = 0; i < runicAlphabetSymbols.Length; i++)
        {
            runicAlphabetSymbols[i].SetIndex(Random.Range(0, 25));
        }

        idleSoundInstance = RuntimeManager.CreateInstance(idleSound);
        RuntimeManager.AttachInstanceToGameObject(idleSoundInstance, transform);
        idleSoundInstance.start();

    }

    public override bool CanAttack(Entity target)
    {
        if (CanSpit(target))
        {
            return true;
        }

        return base.CanAttack(target);
    }

    bool CanSpit(Entity target)
    {
        float distance = Vector3.Distance(transform.position, target.transform.position);
        
        // Debug.Log(Time.time - lastSpit <= spitCooldown);
        // Debug.Log(distance < spitRange);
        // Debug.Log(SeesEntity(target));

        return Time.time - lastSpit >= spitCooldown
            && distance < spitRange
            && SeesEntity(target);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        idleSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }


    public override void Die()
    {
        base.Die();

        idleSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public override void Attack(Entity target = null)
    {
        if (CanSpit(target))
        {
            Spit(target);
            return;
        }

        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        lastAttack = Time.time;
        PlayAnimation("Stinger Attack", 0.05F);
        SetAttacking();
    }

    public void AttackHit()
    {
        if (isServer)
        {
            stingerHitbox.HitOnce(attackDamage, Element.Physical, this, impact: 50);
        }
    }


    void Spit(Entity target)
    {
        spitTarget = target;
        lastSpit = Time.time;
        PlayAnimation("Spit", 0.05F);
    }

    public void SpitCallback()
    {
        if (spitTarget == null)
        {
            return;
        }

        lastSpit = Time.time;
        Projectile.FireAtEntityWithPrediction(this, spitTarget, spitProjectile, spitOrigin.position, SPIT_PROJECTILE_SPEED, 0.5F);

        spitTarget = null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using UnityEngine.VFX;
using FMODUnity;

public class RogueWraithGuardian : Boss
{
    /*---- constant variables ----*/
    [Header("Attacks")]
    [SerializeField] Hitbox attack1Hitbox;
    int attack1Damage = 30;
    [SerializeField] VisualEffect attack1Slash;

    [SerializeField] Hitbox attack2Hitbox;
    int attack2Damage = 30;
    [SerializeField] VisualEffect attack2Slash;

    [SerializeField] EventReference meleeAttackSound;



    [Header("Special 1 (Jumpslam)")]
    [SerializeField] Hitbox special1Hitbox;
    int special1MeleeDamage = 90;
    [SerializeField] Projectile special1Projectile;
    [SerializeField] Transform special1ProjectileSpawn;
    float special1ProjectileSpeed = 18.0F;
    readonly float special1TriggerRange = 5.0F;
    readonly float special1Cooldown = 15.0F;


    [Header("Magic Attack")]
    [SerializeField] Projectile magic1Projectile;
    [SerializeField] Transform magic1Origin;
    readonly float magic1Velocity = 8.0F;
    readonly float magic1TriggerRange = 8.0F;
    readonly float magic1Cooldown = 8.0F;


    [Header("Special 2 (Nuke)")]
    readonly int special2Damage = 90;
    [SerializeField] GameObject special2Indicator;
    [SerializeField] ParticleSystem special2ChargeParticles;
    float special2HoldTime = 0.1F; // time to charge explosion
    [SerializeField] AreaOfEffect special2Explosion;
    readonly float special2TriggerRange = 5.0F;
    readonly float special2Cooldown = 30.0F;
    

    [Header("Magic Beam")]
    [SerializeField] ParticleSystem magic2Beam;
    [SerializeField] float magic2HitInterval = 0.1F;

    [SerializeField] Hitbox magic2Hitbox;
    

    /*---- runtime variables ----*/
    State currentState;

    float lastMagic1 = -10.0F;

    float lastSpecial1 = -10.0F;

    float lastSpecial2 = -10.0F;
    bool chargingSpecial2 = false;

    bool channelingMagic2 = false;

    float lastMagic2Hit = -900.0F;

    Entity currentAttackTarget;

    public override void Start()
    {
        base.Start();

        if (isServer)
        {
            stateMachine.ChangeState(new DormantState(this));
        }

        special2Indicator.SetActive(false);

        special2ChargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        magic2Beam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isServer)
        {
            if (channelingMagic2 && (Time.time - lastMagic2Hit) >= magic2HitInterval)
            {
                lastMagic2Hit = Time.time;
                magic2Hitbox.HitOnce(4, Element.Dark, this);
            }
        }
    }

    protected override void Animate()
    {
        animator.SetFloat("zVelocity", transform.InverseTransformVector(navMeshAgent.velocity).z);
    }
    

    public override void Die()
    {
        special2ChargeParticles.Stop();
        
        base.Die();
    }

    #region ATTACKS
    // Starts an attack
    public override void Attack(Entity target = null)
    {
        currentAttackTarget = target;
        if (currentPhaseIndex >= 2 && CanSpecial2(target))
        {
            // Special 2
            SetAnimatorTrigger("Special2Enter");
            special2Indicator.SetActive(true);
            special2ChargeParticles.Play();
            chargingSpecial2 = true;
        }
        else if (currentPhaseIndex >= 1 && CanSpecial1(target))
        {
            // Special 1
            SetAnimatorTrigger("Special1");
            
        }
        //else if (currentPhaseIndex >= 1 && CanMagic1(target))
        //{
        //    // Magic 1
        //    SetAnimatorTrigger("Magic1");
        //}
        else
        {
            // Random selection between melee attacks
            int attack = Random.Range(0, 2);
            if (attack == 0)
            {
                SetAnimatorTrigger("Attack1");
            }
            else
            {
                SetAnimatorTrigger("Attack2");
            }
            
        }
        SetAttacking(true);
    }
    
    public override bool CanAttack(Entity target)
    {
        if (!base.CanAttack(target))
        {
            return false;
        }

        if (IsAttacking() || chargingSpecial2)
        {
            return false;
        }

        if (currentPhaseIndex >= 2 && CanSpecial2(target))
        {
            return true;
        }
        else if (currentPhaseIndex >= 1 && CanSpecial1(target))
        {
            return true;
        }
        else if (currentPhaseIndex >= 1 && CanMagic1(target))
        {
            return true;
        }
        else
        {
            return InAttackRange(target);
        }
    }

    public override bool CanMove()
    {
        return base.CanMove() && !attacking;
    }

    bool CanMagic1(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= magic1TriggerRange
            && Time.time - lastMagic1 >= magic1Cooldown;
    }

    bool CanSpecial1(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= special1TriggerRange
            && Time.time - lastSpecial1 >= special1Cooldown;
    }

    bool CanSpecial2(Entity target)
    {
        return Vector3.Distance(transform.position, target.transform.position) <= special2TriggerRange
            && Time.time - lastSpecial2 >= special2Cooldown;
    }
    
    // invoked by animator to denote that attack is complete
    #endregion

    // us
    [Server]
    public void Special2TriggerRelease()
    {
        SetAnimatorTrigger("Special2Release");
        special2ChargeParticles.Stop(true);
    }

    #region ANIMATOR CALLBACKS
    public void Special2Explosion()
    {
        special2Indicator.SetActive(false);

        if (isServer)
        {
            lastSpecial2 = Time.time;

            AreaOfEffect.AOEProperties properties = AreaOfEffect.Create(special2Explosion, transform.position, this, hitType:HitType.Ability);
            properties.damage = special2Damage;
        }
    }

    
    public void Special2EnterHold()
    {
        if (isServer)
        {
            Invoke(nameof(Special2TriggerRelease), special2HoldTime); // entered hold, wait for hold duration, invoke Special2TriggerRelease to pass control back to animator
        }
        
    }
    
    public void Magic1Throw()
    {
        Vector3 direction = (currentAttackTarget.GetWorldPosCenter() + new Vector3(0, 2.0F, 0) - magic1Origin.transform.position).normalized;

        Projectile blast = Projectile.Create(magic1Projectile, magic1Origin, gameObject, magic1Velocity * direction);
        blast.SetTarget(currentAttackTarget.gameObject);

        lastMagic1 = Time.time;
    }

    
    public new void AttackComplete()
    {
        SetAttacking(false);
        chargingSpecial2 = false;

        currentAttackTarget = null;
    }
    
    public void Attack1Hit()
    {
        if (isServer)
            attack1Hitbox.HitOnce(attack1Damage, Element.Physical, this);
    }

    public void Attack1SlashVFX()
    {
        attack1Slash.Play();

        AudioManager.Singleton.PlayOneShot(meleeAttackSound, transform.position);
    }

    public void Attack2Hit()
    {
        if (isServer)
            attack2Hitbox.HitOnce(attack2Damage, Element.Physical, this);
    }

    public void Attack2SlashVFX()
    {
        attack2Slash.Play();

        AudioManager.Singleton.PlayOneShot(meleeAttackSound, transform.position);
    }


    public void Special1Collision()
    {
        if (isServer)
        {
            special1Hitbox.HitOnce(special1MeleeDamage, Element.Physical, this);
            Projectile.Create(special1Projectile, special1ProjectileSpawn, gameObject, special1ProjectileSpawn.TransformVector(new Vector3(0.0F, 0.0F, special1ProjectileSpeed)));
            Projectile.Create(special1Projectile, special1ProjectileSpawn, gameObject,  Quaternion.Euler(0, -20, 0) * special1ProjectileSpawn.TransformVector(new Vector3(0.0F, 0.0F, special1ProjectileSpeed)));
            Projectile.Create(special1Projectile, special1ProjectileSpawn, gameObject, Quaternion.Euler(0, 20, 0) * special1ProjectileSpawn.TransformVector(new Vector3(0.0F, 0.0F, special1ProjectileSpeed)));

            lastSpecial1 = Time.time;
        }
    }
    #endregion

    [Server]
    public void Magic2ChannelStart()
    {
        magic2Beam.Play();
        channelingMagic2 = true;
    }

    [Server]
    public void Magic2ChannelStop()
    {
        magic2Beam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        channelingMagic2 = false;
    }
}

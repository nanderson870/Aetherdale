using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEngine;

public class LashingVine : Entity
{
    const float BASE_LASHING_VINE_HEALTH_REGEN = -7.5F;

    [SerializeField] Hitbox lashHitbox;

    [SyncVar] float attackCooldown = 5.0F;
    [SyncVar] float attackRange = 10.0F;

    [SerializeField] EventReference attackSound; 

    public override void Start()
    {
        base.Start();

        if (isServer)
        {
            lastAttack = Time.time - attackCooldown;

            animator.SetFloat("attackSpeed", GetStat(Stats.AttackSpeed));
        }
    }

    public override void Update()
    {
        base.Update();

        if (isServer)
        {
            currentTarget = GetPreferredEnemy(attackRange);
            if (currentTarget != null && CanAttack())
            {
                Attack();
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (currentTarget != null)
        {
            TurnTowards(currentTarget.gameObject, 90.0F);
        }
    }

    public override void SetDefaultStats()
    {
        base.SetDefaultStats();
        
        SetStat(Stats.HealthRegen, BASE_LASHING_VINE_HEALTH_REGEN);
    }

    bool CanAttack()
    {
        return Time.time - lastAttack >= attackCooldown
            && !attacking;
    }

    
    public override Vector3 GetVelocity()
    {
        return Vector3.zero;
    }

    [Server]
    void Attack()
    {
        SetAnimatorTrigger("Strike");
    }

    public override void Move(Vector3 magnitude)
    {
    }

    protected override void Animate()
    {
    }

    public void LashHit()
    {
        if (isServer)
        {
            lashHitbox.HitOnce(10 , Element.Nature, this, hitType:HitType.Attack);
        }
        
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
    }
}

using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class IthindarImp : StatefulCombatEntity
{
    [SerializeField] Effect invisibilityEffect;
    [SerializeField] Effect attackBleedEffect;
    [SerializeField] Hitbox knifeHitbox;


    int stabDamage = 12;
    int slashDamage = 8;
    float invisibilityCooldown = 15.0F;
    float stealthSpeedMult = 0.35F;

    // runtime
    float originalSpeed;
    float lastInvisible = 0;


    public override void Start()
    {
        base.Start();
        
        if (isServer)
        {
            originalSpeed = navMeshAgent.speed;

            GoInvisible();
        }
    }

    public override void Update()
    {
        base.Update();

        if (!invisible && CanGoInvisible())
        {
            GoInvisible();
        }
        
        if (invisible)
        {

        }
    }

    protected override void Animate()
    {
        base.Animate();

        Vector2 horizontalVelocity = new(localVelocity.x, localVelocity.z);

        if (horizontalVelocity.magnitude > 0.5F && !airborne)
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 1.0F);
        }
        else
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 0.0F);
        }
    }

    bool CanGoInvisible()
    {
        return (Time.time - lastInvisible >= invisibilityCooldown) && (Time.time - lastDamaged >= invisibilityCooldown / 2);
    }

    [Server]
    void GoInvisible()
    {
        if (IsInvisible())
        {
            return;
        }

        SetStat(Stats.MovementSpeedMult, stealthSpeedMult);

        EffectInstance invisInstance = AddEffect(invisibilityEffect, this);
        invisInstance.OnEffectEnd += InvisibilityBroken;
    }

    [Server]
    void InvisibilityBroken(EffectInstance effectInstance)
    {
        SetStat(Stats.MovementSpeedMult, 1.0F);

        effectInstance.OnEffectEnd -= InvisibilityBroken;

        lastInvisible = Time.time;
    }

    public override void Attack(Entity target = null)
    {
        lastAttack = Time.time;

        if (IsInvisible())
        {
            Stab(target);
        }
        else
        {
            Slash(target);
        }

        SetAttacking();
    }

    void Stab(Entity entity)
    {
        PlayAnimation("Stab", 0.05F);
    }

    void Slash(Entity entity)
    {
        if (Random.Range(0, 2) < 1)
        {
            PlayAnimation("Slash 1", 0.05F);
        }
        else
        {
            PlayAnimation("Slash 2", 0.05F);
        }
    }

    void ApplyBleed(HitboxHitData data)
    {
        Debug.Log("Apply bleed to " + data.hitEntity);
        data.hitEntity.AddEffect(attackBleedEffect, this);
    }

    public override bool CanMove()
    {
        return !stunned && !animator.GetBool("staggered") && Time.time - lastStagger > UNIVERSAL_STAGGER_DURATION && navMeshAgent.enabled;
    }


    #region ANIMATOR CALLBACKS
    [ServerCallback]
    public void StabStart()
    {
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        knifeHitbox.StartHit(stabDamage, Element.Physical, HitType.Attack, this, onHitAction:ApplyBleed);

        if (IsInvisible())
        {
            knifeHitbox.SetForceCritical();
            RemoveEffect(invisibilityEffect);
        }
    }

    [ServerCallback]
    public void StabEnd()
    {
        knifeHitbox.EndHit();
    }

    [ServerCallback]
    public void SlashStart()
    {
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
        knifeHitbox.StartHit(slashDamage, Element.Physical, HitType.Attack, this, onHitAction:ApplyBleed);

        if (IsInvisible())
        {
            knifeHitbox.SetForceCritical();
            RemoveEffect(invisibilityEffect);
        }
    }

    [ServerCallback]
    public void SlashEnd()
    {
        knifeHitbox.EndHit();
    }
    #endregion

}

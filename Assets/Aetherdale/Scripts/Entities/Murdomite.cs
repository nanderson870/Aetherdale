using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.VFX;
using FMODUnity;
using FMOD.Studio;

public class Murdomite : StatefulCombatEntity
{
    [SerializeField] VisualEffect rumbleVFX;


    [Header("Attack 1")]
    public Hitbox attack1Hitbox;
    public int attack1Damage = 35;
    public float attack1KnockbackMagnitude = 20.0F;

    [Header("Attack 2")]
    public Hitbox attack2Hitbox;
    public int attack2Damage = 25;
    public float attack2KnockbackMagnitude = 30.0F;

    [Header("Burrow")]
    public Hitbox unburrowHitbox;
    public float unburrowStunDuration = 0.75F;
    public int unburrowDamage = 30;
    public float burrowCooldown = 15F;
    public float burrowMoveSpeed = 20.0F;
    public float unburrowDelay = 0.25F;
    public Material burrowMaterialSwap;
    [SerializeField] EventReference burrowIdleSound;
    [SerializeField] EventReference burrowEnterSound;
    [SerializeField] EventReference burrowExitSound;

    bool burrowing = false;
    bool burrowMovementStarted = false;
    float lastBurrow = 0;

    EventInstance burrowIdleInstance;

    public override void Start()
    {
        base.Start();

        lastBurrow = Time.time;
    }


    [ServerCallback]
    public override void LateUpdate()
    {
        base.LateUpdate();

        Animate();
    }

    void StartRumbleVFX()
    {
        rumbleVFX.SendEvent("Start");
    }

    void StopRumbleVFX()
    {
        rumbleVFX.SendEvent("Stop");
    }

    public override bool CanAttack(Entity target)
    {
        if (burrowing)
        {
            return false;
        }

        if (CanBurrow(target))
        {
            return true;
        }
        
        return base.CanAttack(target);
    }

    [Server]
    public override void Attack(Entity target = null)
    {
        if (CanBurrow(target))
        {
            stateMachine.ChangeState(new BurrowState(this, target));
            return;
        }

        int chosenAttack = Random.Range(0, 2);

        if (chosenAttack == 0)
        {
            SetAnimatorTrigger("attack");
        }
        else
        {
            SetAnimatorTrigger("attack2");
        }
    }

    [Server]
    public void Burrow()
    {
        burrowing = true;

        SetAnimatorTrigger("Burrow");
        AudioManager.Singleton.PlayOneShot(burrowEnterSound, transform.position);

        RpcSetMaterial(burrowMaterialSwap.name, MaterialChangeProperties.All);

        RpcBurrow();

        invulnerable = true;
    }

    protected override State GetPreferredState()
    {
        if (stateMachine.GetState() is BurrowState burrowState && !burrowState.ReadyForExit())
        {
            return null;
        }

        return base.GetPreferredState();
    }

    [ClientRpc]
    void RpcBurrow()
    {
        StartRumbleVFX();

        if (!burrowIdleInstance.isValid())
        {
            burrowIdleInstance = RuntimeManager.CreateInstance(burrowIdleSound);
            RuntimeManager.AttachInstanceToGameObject(burrowIdleInstance, transform);

            burrowIdleInstance.start();
        }

    }

    [Server]
    public void ExitBurrow()
    {
        StartCoroutine(ExitBurrowCoroutine());
    }

    [Server]
    public IEnumerator ExitBurrowCoroutine()
    {
        yield return new WaitForSeconds(unburrowDelay);

        lastAttack = Time.time;
        lastBurrow = Time.time;
        burrowing = false;
        burrowMovementStarted = false;
        SetAnimatorTrigger("ExitBurrow");
        RpcResetMaterials();

        RpcExitBurrow();

        invulnerable = false;
    }

    [ClientRpc]
    void RpcExitBurrow()
    {
        StopRumbleVFX();
        AudioManager.Singleton.PlayOneShot(burrowExitSound, transform.position);
        burrowIdleInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        burrowIdleInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    bool CanBurrow(Entity target)
    {
        return !burrowing && (Time.time - lastBurrow) > burrowCooldown;
    }

    public override bool CanMove()
    {
        if (burrowing)
        {
            return burrowMovementStarted;
        }

        return base.CanMove();
    }


    private void ApplyAttack1Knockback(HitboxHitData data)
    {
        Entity target = data.hitEntity;

        Vector3 direction = (target.transform.position - attack1Hitbox.transform.position + new Vector3(0, 1F, 0) + transform.forward).normalized;
        target.Push(direction * attack1KnockbackMagnitude, forceMode:ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.6F, 1F, target.transform.position, frequency:3);
    }


    private void ApplyAttack2Knockback(HitboxHitData data)
    {
        Entity target = data.hitEntity;

        Vector3 direction = (target.transform.position - attack2Hitbox.transform.position + new Vector3(0, 1F, 0) + transform.forward).normalized;
        target.Push(direction * attack2KnockbackMagnitude, forceMode: ForceMode.Impulse);

        PlayerCamera.ApplyScreenShake(0.6F, 1F, target.transform.position, frequency: 3);
    }
    
    private void ApplyUnburrowStun(HitboxHitData hitData)
    {
        if (hitData.hitEntity != null)
        {
            hitData.hitEntity.Stun(unburrowStunDuration);
        }
    }


    public void HitboxEvent()
    {
        if (isServer)
        {
            attack1Hitbox.HitOnce(attack1Damage, Element.Physical, this, ApplyAttack1Knockback);
        }
    }

    public void UnburrowHit()
    {
        if (isServer)
        {
            unburrowHitbox.HitOnce(unburrowDamage, Element.Physical, this, ApplyUnburrowStun);
        }
    }

    public void Attack2Start()
    {
        if (isServer)
        {
            attack2Hitbox.StartHit(attack2Damage, Element.Physical, HitType.Attack, this, 80, ApplyAttack2Knockback);
        }
    }


    public void Attack2End()
    {
        if (isServer)
        {
            attack2Hitbox.EndHit();
        }
    }

    public void FullyBurrowed()
    {
        burrowMovementStarted = true;
    }

    public class BurrowState : State
    {
        Murdomite murdomite;
        Entity target;
        float startTime;
        float maxDuration = 8;
        float eruptRadius = 4F;

        bool entered = false;

        public BurrowState(Murdomite murdomite, Entity target)
        {
            this.murdomite = murdomite;
            this.target = target;
        }

        public override void OnEnter()
        {
            startTime = Time.time;

            murdomite.Burrow();

            base.OnEnter();
        }

        public override void Update()
        {
            base.Update();

            murdomite.SetDestination(target.transform.position, murdomite.burrowMoveSpeed);
        }

        float DistanceToTarget()
        {
            return Vector3.Distance(murdomite.transform.position, target.transform.position);
        }

        public override void OnExit()
        {
            murdomite.ExitBurrow();
            murdomite.ClearDestination();

            base.OnExit();
        }

        public override bool ReadyForExit()
        {
            if (!murdomite.burrowing || !murdomite.burrowMovementStarted) return false; // not even started yet
            
            return Time.time - startTime > maxDuration
                || DistanceToTarget() <= eruptRadius;
        }
    }
}

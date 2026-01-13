using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using FMODUnity;
using FMOD.Studio;

public class RockElemental : StatefulCombatEntity
{
    [SerializeField] int punchDamage = 15;
    [SerializeField] Hitbox punchHitbox;

    [SerializeField] float slamCooldown = 6.0f;
    [SerializeField] int slamDamage = 25;
    [SerializeField] Hitbox slamHitbox;

    float lastSlam = -10.0F;

    [SerializeField] EventReference movementSound;


    EventInstance movementSoundInstance;

    public override void Start()
    {
        base.Start();

    }

    bool playingMovementSound = false;
    
    public override void Update()
    {
        base.Update();

        if (isClient)
        {
            if (GetVelocity().magnitude >= 0.3F && !playingMovementSound)
            {
                playingMovementSound = true;
                movementSoundInstance = RuntimeManager.CreateInstance(movementSound);
                RuntimeManager.AttachInstanceToGameObject(movementSoundInstance, transform);
                movementSoundInstance.start();
            }
            else if (GetVelocity().magnitude < 0.3F && playingMovementSound)
            {
                playingMovementSound = false;
                movementSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                movementSoundInstance.release();
            }
        }
    }


    public override void OnDestroy()
    {
        base.OnDestroy();

    }

    [Server]
    public override void Attack(Entity target = null)
    {
        if ((Time.time - lastSlam) >= slamCooldown)
        {
            lastSlam = Time.time;
            SetAnimatorTrigger("Slam");
            SetAttacking();
        }
        else
        {
            SetAnimatorTrigger("Attack");
            SetAttacking();
        }

        lastAttack = Time.time;
    }

    [ServerCallback]
    protected void AttackHit()
    {
        punchHitbox.HitOnce(punchDamage, Element.Physical, this);
    }

    [ServerCallback]
    protected void SlamHit()
    {
        PlayerCamera.ApplyScreenShake(0.1F, 0.25F, transform.position);
        slamHitbox.HitOnce(slamDamage, Element.Physical, this);
    }
}

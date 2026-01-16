using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.VFX;
using FMOD.Studio;
using FMODUnity;

public class Rootrot : StatefulCombatEntity
{
    [Header("Attack")]
    public Hitbox attack1Hitbox;
    public int attack1Damage;

    [Header("Tumble")]
    [SerializeField] VisualEffect tumbleVFX;
    [SerializeField] Hitbox tumbleHitbox;
    [SerializeField] EventReference tumbleTelegraphSound;
    [SerializeField] EventReference tumbleLoopSound;
    readonly int tumbleDamage = 15;
    readonly float tumbleCooldown = 10.0F;
    readonly float tumbleSpeed = 18;
    readonly float tumbleOvershoot = 5.0F;


    float lastTumble = -10;
    bool inTumble = false;

    EventInstance tumbleLoopInstance;


    public override void Stagger()
    {
        base.Stagger();

        if (inTumble)
        {
            inTumble = false;
            Stun(1.0F);   
        }
    }


    [ServerCallback]
    public void HitboxEvent()
    {
        attack1Hitbox.HitOnce(attack1Damage, Element.Physical, this);
    }

    public bool CanTumble(Entity target)
    {
        return IsOvershootGrounded(target, tumbleOvershoot)
            && (Time.time - lastTumble) > tumbleCooldown;
    }

    protected override State GetPreferredState()
    {
        Entity nearestEnemy = GetNearestEnemy();
        if (stateMachine.GetState() is TumbleState tumbleState)
        {
            if (!tumbleState.ReadyForExit())
            {
                return null;
            }
        }

        if (nearestEnemy != null && CanTumble(nearestEnemy))
        {
            return new TumbleState(this, nearestEnemy);
        }

        return base.GetPreferredState();
    }


    public void TumbleStart()
    {
        inTumble = true;

        Vector3 targetPos = currentTarget.transform.position + (0.2F * currentTarget.GetVelocity());
        Vector3 overshoot = (targetPos - transform.position).normalized * 3.0F;
        SlideToPositionWithSpeed(targetPos + overshoot, tumbleSpeed);

        tumbleHitbox.StartHit(tumbleDamage, Element.Physical, HitType.Ability, this, 100);

        RpcTumbleStart();

    }

    [ClientRpc]
    void RpcTumbleTelegraph()
    {
        AudioManager.Singleton.PlayOneShot(tumbleTelegraphSound, GetWorldPosCenter());
    }

    [ClientRpc]
    void RpcTumbleStart()
    {
        tumbleVFX.SendEvent("Start");

        tumbleLoopInstance = RuntimeManager.CreateInstance(tumbleLoopSound);
        RuntimeManager.AttachInstanceToGameObject(tumbleLoopInstance, transform);
        tumbleLoopInstance.start();
    }

    [ClientRpc]
    void RpcTumbleStop()
    {
        tumbleVFX.SendEvent("Stop");

        tumbleLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        tumbleLoopInstance.release();
    }

    public class TumbleState : State
    {
        Rootrot rootrot;
        Entity target;
        public TumbleState(Rootrot rootrot, Entity target)
        {
            this.rootrot = rootrot;
            this.target = target; 
        }

        public override void OnEnter()
        {
            base.OnEnter();

            rootrot.ClearDestination();
            rootrot.SetAnimatorTrigger("TumbleEnter");
            rootrot.RpcTumbleTelegraph();

            rootrot.currentTarget = target;
        }

        public override void Update()
        {
            base.Update();

            if (!rootrot.inTumble)
            {
                rootrot.TurnTowards(target.gameObject, 180, predictiveOvershootStrength:0.5F);
                return;
            }

            rootrot.lastTumble = Time.time;
        }

        public override bool ReadyForExit()
        {
            return rootrot.inTumble && rootrot.navMeshAgent.remainingDistance < 1.0F;
            //return rootrot.GetRelativeBearingAngle(target.gameObject) > rootrot.tumbleContinueRelativeBearing;
        }

        public override void OnExit()
        {
            rootrot.ClearDestination(abruptStop:true);

            rootrot.SetAnimatorTrigger("TumbleExit");

            rootrot.RpcTumbleStop();

            rootrot.inTumble = false;

            rootrot.currentTarget = null;

            rootrot.tumbleHitbox.EndHit();

            base.OnExit();
        }
    }
}

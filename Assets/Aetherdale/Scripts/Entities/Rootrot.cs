using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.VFX;

public class Rootrot : StatefulCombatEntity
{
    [Header("Attack")]
    public Hitbox attack1Hitbox;
    public int attack1Damage;

    [Header("Tumble")]
    [SerializeField] VisualEffect tumbleVFX;
    readonly float tumbleDistance = 70.0F;
    readonly float tumbleContinueRelativeBearing = 60F;
    readonly float tumbleCooldown = 10.0F;
    readonly float tumbleSpeed = 30;
    readonly float tumbleAngularSpeed = 0F;


    float lastTumble = -10;
    bool inTumble = false;

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
        return (Time.time - lastTumble) > tumbleCooldown;
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
        tumbleVFX.SendEvent("Start");

        Vector3 targetOffset = (currentTarget.transform.position + (currentTarget.GetVelocity() * 0.5F) - transform.position);
        Vector3 targetOffsetOvershoot = targetOffset.normalized * 8.0F;
        Vector3 destination = transform.position +  targetOffset + targetOffsetOvershoot;
        SetDestination(destination, speed:tumbleSpeed, acceleration:999, angularSpeed:tumbleAngularSpeed);
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

            rootrot.tumbleVFX.SendEvent("Stop");

            rootrot.inTumble = false;

            rootrot.currentTarget = null;

            base.OnExit();
        }
    }
}

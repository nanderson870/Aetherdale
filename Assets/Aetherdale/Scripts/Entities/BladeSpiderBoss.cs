using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

// Blade Spider Boss Behavior:
//
// 1. The spider will attack with left and right stabs.
// 2. It will turn to face its current target until the stab is likely to hit - DONE
// 3. If the target is out of reach, it will approach the target, gaining a burst of speed
//    when about to attack, if appropriate
// 4. Spider will use a channeled whirl attack when any of the following apply:
//      -its target is in its blindspots for more than 3 seconds
//      -it hasn't landed any of its last three attacks
//      -it has gone eight or more attacks without using the whirl

public class BladeSpiderBoss : Boss
{
    [SerializeField] float engageDistance = 7.0F;

    [SerializeField] float rotationHeadingTargetZone = 15.0F; // if player is within +/- this many degrees of heading, we won't turn any further to target
    [SerializeField] float rotationBlindSpotRadius = 3.0F; // if player under this radius, we won't turn to target them

    // attack behavioral
    [SerializeField] Hitbox rightStabHitbox;
    [SerializeField] Hitbox leftStabHitbox;
    [SerializeField] Hitbox whirlHitbox;

    [SerializeField] Hitbox footstepHitboxFrontRight;
    [SerializeField] Hitbox footstepHitboxFrontLeft;
    [SerializeField] Hitbox footstepHitboxBackRight;
    [SerializeField] Hitbox footstepHitboxBackLeft;

    [SerializeField] Animator operatorAnimator;

    //[SerializeField] List<ParticleSystem> whirlParticleSystems;
    [SerializeField] VisualEffect whirlVFX;
    [SerializeField] ParticleSystem deathExplosion;


    [SerializeField] EventReference whirlHitSound;
    

    int footstepDamage = 5;
    int stabDamage = 45;
    float stabCooldown = 3.0F;

    float whirlBlindspotLimit = 3.0F; // if target stays in blind spot this long, we whirl
    float whirlCooldown = 10.0F;

    // attack runtime tracking
    bool stabbing = false;
    float lastStab = -1.0F;
    
    float lastTimeAbleToStabTarget = 0.0F;
    int consecutiveStabs;
    int consecutiveMissedAttacks;
    float lastWhirlTime;

    EventInstance idleSoundInstance;


    public override void Start()
    {
        base.Start();

        navMeshAgent = GetComponent<NavMeshAgent>();

        lastTimeAbleToStabTarget = Time.time;
        lastWhirlTime = Time.time;

        deathExplosion.Stop(true);

        whirlVFX.Stop();

        
        idleSoundInstance = RuntimeManager.CreateInstance(idleSound);
        RuntimeManager.AttachInstanceToGameObject(idleSoundInstance, transform);
        idleSoundInstance.start();
    }

    protected override State GetPreferredState()
    {
        State currentState = stateMachine.GetState();
        Entity preferredTarget = GetPreferredEnemy();

        if (currentState is BladeSpiderWhirlState && !currentState.ReadyForExit())
        {
            return null;
        }

        if (CanWhirl(preferredTarget))
        {
            return new BladeSpiderWhirlState(this, preferredTarget);
        }
        else if (preferredTarget != null && currentState is not BladeSpiderPursuitState)
        {
            return new BladeSpiderPursuitState(this, preferredTarget);
        }

        return null;
    }

    public bool CanWhirl(Entity target)
    {
        return !stabbing
            && Time.time - lastWhirlTime >= whirlCooldown
            && (Time.time - lastTimeAbleToStabTarget >= whirlBlindspotLimit
                || consecutiveMissedAttacks >= 3
                || consecutiveStabs >= 8);
    }


    protected override void Animate()
    {
        animator.SetFloat("zVelocity", localVelocity.z);
        animator.SetFloat("xVelocity", localVelocity.x);

        
        operatorAnimator.SetFloat("zVelocity", localVelocity.z);
    }

    public bool AtEngageDistance(Entity target)
    {
        return Vector3.Distance(target.transform.position, this.transform.position) <= this.engageDistance;
    }

    float TimeUntilStab()
    {
        return stabCooldown - (Time.time - lastStab);
    }

    void Stab(Entity target)
    {
        stabbing = true;

        if (GetRelativeBearingAngle(target.gameObject) > 0)
        {
            // target is to our right
            SetAnimatorTrigger("StabRight");
            operatorAnimator.SetTrigger("StabRight");
        }
        else
        {
            // target is to our left or (unlikely and trivially) directly in the center
            SetAnimatorTrigger("StabLeft");
            operatorAnimator.SetTrigger("StabLeft");
        }

        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);

        consecutiveStabs++;
    }

    [ClientRpc]
    void RpcStartWhirl()
    {
        whirlVFX.Play();
    }

    [ClientRpc]
    void RpcStopWhirl()
    {
        whirlVFX.Stop();
    }

    // animator
    public void StabComplete()
    {
        stabbing = false;
        lastStab = Time.time;
    }

    public override bool CanMove()
    {
        return base.CanMove() && !stabbing;
    }

    public override void Die()
    {
        base.Die();

        deathExplosion.Play();
    }

    public override void OnDestroy()
    {
        idleSoundInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        idleSoundInstance.release();

        base.OnDestroy();
    }

    #region Animator Callbacks

    [ServerCallback]
    public void RightStabHit()
    {
        if (rightStabHitbox.HitOnce(stabDamage, Element.Physical, this).Count > 0)
        {
            consecutiveMissedAttacks = 0;
        }
        else
        {
            consecutiveMissedAttacks++;
        }
    }

    [ServerCallback]
    public void LeftStabHit()
    {
        if (leftStabHitbox.HitOnce(stabDamage, Element.Physical, this).Count > 0)
        {
            consecutiveMissedAttacks = 0;
        }
        else
        {
            consecutiveMissedAttacks++;
        }
    }

    [ServerCallback]
    public void FootstepFrontRight()
    {
        footstepHitboxFrontRight.HitOnce(footstepDamage, Element.Physical, this);
    }

    [ServerCallback]
    public void FootstepFrontLeft()
    {
        footstepHitboxFrontLeft.HitOnce(footstepDamage, Element.Physical, this);
    }

    [ServerCallback]
    public void FootstepBackRight()
    {
        footstepHitboxFrontRight.HitOnce(footstepDamage, Element.Physical, this);
    }

    [ServerCallback]
    public void FootstepBackLeft()
    {
        footstepHitboxFrontRight.HitOnce(footstepDamage, Element.Physical, this);
    }


    #endregion

    

    #region States
    class BladeSpiderPursuitState : State
    {
        BladeSpiderBoss bsb;
        Entity target;
    
        public BladeSpiderPursuitState(BladeSpiderBoss bsb, Entity target)
        {
            this.bsb = bsb;
            this.target = target;
        }

        public override void OnEnter()
        {
            target.OnTransformed += ChangeTarget;
        }

        public override void Update()
        {
            if (target == null)
            {
                bsb.stateMachine.ChangeState(new WaitState(bsb));
            }
            if (bsb.CanMove() && target != null)
            {
                bsb.navMeshAgent.SetDestination(target.transform.position);
            }
            else
            {
                bsb.navMeshAgent.ResetPath();
            }

            bsb.animator.SetFloat("yRotational", 0.0F);

            if (bsb.AtEngageDistance(target) && Vector3.Distance(target.transform.position, bsb.transform.position) > bsb.rotationBlindSpotRadius)
            {
                if (Mathf.Abs(bsb.GetRelativeBearingAngle(target.gameObject)) >= bsb.rotationHeadingTargetZone)
                {
                    float rot = 360 * bsb.TurnTowards(target.gameObject, bsb.rotationSpeed);
                    bsb.animator.SetFloat("yRotational", rot);
                }
                else
                { 
                    bsb.lastTimeAbleToStabTarget = Time.time;
                    if (bsb.TimeUntilStab() <= 0.0F && !bsb.stabbing)
                    {
                        bsb.Stab(target);
                    }
                }
            }
        }

        public override void OnExit()
        {
            target.OnTransformed -= ChangeTarget;
            bsb.navMeshAgent.ResetPath();
        }

        public void ChangeTarget(Entity oldEntity, Entity newTarget)
        {
            target = newTarget;
        }

        public override bool ReadyForExit()
        {
            return false;
        }

    }

    class BladeSpiderWhirlState : State
    {
        BladeSpiderBoss bsb;
        Entity target;

        float originalNavMeshAgentStoppingDistance;

        bool whirling;
        float whirlDuration = 10.0F;
        float whirlHitInterval = 0.2F;
        int whirlDamage = 3;

        float startTime;
        float lastWhirlHit = -1.0F;


        public BladeSpiderWhirlState(BladeSpiderBoss bsb, Entity target)
        {
            this.bsb = bsb;
            this.target = target;

            originalNavMeshAgentStoppingDistance = bsb.navMeshAgent.stoppingDistance;

            startTime = Time.time;
            whirling = true;
        }

        public override void Update()
        {
            if ((Time.time - startTime) >= whirlDuration)
            {
                if (target != null)
                {
                    bsb.stateMachine.ChangeState(new BladeSpiderPursuitState(bsb, target));
                }
                else
                {
                    bsb.stateMachine.ChangeState(null);
                }

                whirling = false;
                return;
            }

            if (bsb.CanMove() && target != null)
            {
                bsb.navMeshAgent.SetDestination(target.transform.position);
            }
            else
            {
                bsb.navMeshAgent.ResetPath();
            }

            if (whirling && (Time.time - lastWhirlHit) > whirlHitInterval)
            {
                lastWhirlHit = Time.time;
                bsb.whirlHitbox.HitOnce(whirlDamage, Element.Physical, bsb);
                AudioManager.Singleton.PlayOneShot(bsb.whirlHitSound, bsb.GetWorldPosCenter());
            }
        }

        public override void OnEnter()
        {
            bsb.navMeshAgent.stoppingDistance = 0.0F;

            target.OnTransformed += ChangeTarget;

            bsb.SetAnimatorTrigger("EnterWhirl");
            
            bsb.RpcStartWhirl();
        }

        public override void OnExit()
        {
            bsb.navMeshAgent.stoppingDistance = originalNavMeshAgentStoppingDistance;

            if (target != null)
            {
                target.OnTransformed -= ChangeTarget;
            }

            bsb.SetAnimatorTrigger("ExitWhirl");

            bsb.RpcStopWhirl();

            bsb.lastWhirlTime = Time.time;
        }

        void ChangeTarget(Entity oldEntity, Entity newTarget)
        {
            if (target != null)
            {
                target.OnTransformed -= ChangeTarget;
            }

            target = newTarget;
        }

        public override bool ReadyForExit()
        {
            return !whirling;
        }
    }

    #endregion
}

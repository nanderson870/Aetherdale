using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using UnityEngine;

public class CavernbreakerBoss : Boss
{
    [SerializeField] Hitbox attackHitbox;
    [SerializeField] Hitbox slamHitbox;
    int attackDamage = 35;
    int slamDamage = 50;

    float forwardAttackAngle = 35.0F;
    
    public const float BARGE_MIN_RANGE = 10.0F;
    public const float BARGE_MAX_RANGE = 30.0F;
    public const float BARGE_FORCE = 50.0F;
    public const float BARGE_COOLDOWN = 25.0F;


    bool barging = false;
    float bargeCooldownRemaining = 8F;
    Entity bargeTarget;


    public const float CAVERNBREAK_COOLDOWN = 10.0F;
    
    float cavernbreakCooldownRemaining = 0F;

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        bargeCooldownRemaining -= Time.deltaTime;
        if (bargeCooldownRemaining < 0)
        {
            bargeCooldownRemaining = 0;
        }

        cavernbreakCooldownRemaining -= Time.deltaTime;
        if (cavernbreakCooldownRemaining < 0)
        {
            cavernbreakCooldownRemaining = 0;
        }
    }

    /*
    protected override State GetPreferredState()
    {
        return new DormantState();
    }
    */

    public override bool CanTurn()
    {
        return false && base.CanTurn() 
            && !attacking 
            && movementMode != MovementMode.Rigidbody
            && !JustBarged();
    }

    public override bool CanMove()
    {
        return base.CanMove()
            && !JustBarged();
    }

    #region ATTACK LOOP
    public override bool CanAttack(Entity target)
    {
        if (CanBarge(target))
        {
            return true;
        }
        // else if (CanCavernbreak())
        // {
        //     return true;
        // }

        return base.CanAttack(target);
    }

    public override void Attack(Entity target = null)
    {
        if (CanBarge(target))
        {
            Barge(target);
        }
        // else if (CanCavernbreak())
        // {
        //     Cavernbreak();
        // }
        else
        {

            // Determine if roughly left, right or center
            float angle = GetRelativeBearingAngle(target.gameObject);

            if (Mathf.Abs(angle) < forwardAttackAngle)
            {
                // Frontal attack
                AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
                int attack = Random.Range(0, 2);
                if (attack == 0)
                {
                    SetAnimatorTrigger("Slash");
                }
                else
                {
                    SetAnimatorTrigger("Smash");
                }
                lastAttack = Time.time;
            }
            else if (angle > 0)
            {
                AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
                SetAnimatorTrigger("RightTurnAttack");
                lastAttack = Time.time;

                //SetMovementMode(MovementMode.Rigidbody);
            }
            else if (angle < 0)
            {
                AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
                SetAnimatorTrigger("LeftTurnAttack");
                lastAttack = Time.time;

                //SetMovementMode(MovementMode.Rigidbody);
            }


            SetAttacking(true);
        }

    }
    #endregion

    #region BARGE

    bool JustBarged()
    {
        return bargeCooldownRemaining > (BARGE_COOLDOWN - 0.5F);
    }

    bool CanBarge(Entity target)
    {
        if (movementMode == MovementMode.Rigidbody || bargeCooldownRemaining > 0 || GetRelativeBearingAngle(target.gameObject) > 5.0F)
        {
            return false;
        }

        float distance = Vector3.Distance(target.transform.position, transform.position);
        Debug.Log(distance);
        return distance >= BARGE_MIN_RANGE && distance <= BARGE_MAX_RANGE;
    }

    void Barge(Entity target)
    {
        bargeTarget = target;
        barging = true;

        SetAnimatorTrigger("BargeWindupEnter");
        bargeCooldownRemaining = BARGE_COOLDOWN;
    }

    void BargeWindupComplete()
    {
        if (!isServer)
        {
            return;
        }

        SetAttacking(true);

        Debug.Log("barging");
        Vector3 direction = bargeTarget.transform.position - transform.position;

        float distance = direction.magnitude + 4.0F;

        direction.y = 0;
        if (direction.magnitude > 1.0F)
        {
            direction = direction.normalized;
        }

        SetAnimatorTrigger("BargeEnter");

        PushSelf(direction * BARGE_FORCE, BargeEnd, ForceMode.Impulse);
        //SlideRigidBodyDistance(direction * BARGE_VELOCITY, distance, BargeEnd);
        bargeTarget = null;
    }

    void BargeEnd()
    {
        Debug.Log("ending barge");
        SetAnimatorTrigger("BargeExit");
        SetAttacking(false);
    }

    #endregion

    #region CAVERNBREAK

    /* A big slam that unleashes debris from above */

    bool CanCavernbreak()
    {
        return cavernbreakCooldownRemaining <= 0;
    }

    void Cavernbreak()
    {
        cavernbreakCooldownRemaining = CAVERNBREAK_COOLDOWN;

        SetAnimatorTrigger("CavernbreakSlam");

        SetAttacking(true);
    }

    #endregion

    #region STALACTITE SWARM
    #endregion

    #region ATTACK CALLBACKS
    public void BladeSwingHit()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.HitOnce(attackDamage, Element.Physical, this, hitType:HitType.Attack, impact:100);
        PlayerCamera.ApplyScreenShake(0.3F, 0.5F, attackHitbox.transform.position);
    }

    public void MaceSwingHit()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.HitOnce(attackDamage, Element.Physical, this, hitType:HitType.Attack, impact:100);
        PlayerCamera.ApplyScreenShake(0.3F, 0.5F, attackHitbox.transform.position);
    }

    
    public void RightTurnAttackStart()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.StartHit(attackDamage, Element.Physical, hitType:HitType.Attack, this, impact:100);
        PlayerCamera.ApplyScreenShake(0.3F, 0.5F, attackHitbox.transform.position);
    }

    public void RightTurnAttackEnd()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.EndHit();

        SetMovementMode(MovementMode.NavMeshAgent);
    }


    public void LeftTurnAttackStart()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.StartHit(attackDamage, Element.Physical, hitType:HitType.Attack, this, impact:100);
        PlayerCamera.ApplyScreenShake(0.3F, 0.5F, attackHitbox.transform.position);
    }

    public void LeftTurnAttackEnd()
    {
        if (!isServer)
        {
            return;
        }

        attackHitbox.EndHit();

        SetMovementMode(MovementMode.NavMeshAgent);
    }
    
    void CavernbreakImpact()
    {
        if (!isServer)
        {
            return;
        }

        PlayerCamera.ApplyScreenShake(0.5F, 1.0F, transform.position);

        List<Stalactite> stalactites = FindObjectsByType<Stalactite>(FindObjectsSortMode.None).ToList();
        for (int i = stalactites.Count - 1; i >= 0; i--)
        {
            if (stalactites[i].Falling)
            {
                stalactites.RemoveAt(i);
            }
        }

        int numStalactites = 5;
        while (stalactites.Count > 0 && numStalactites > 0)
        {
            numStalactites--;

            Stalactite stalactite = stalactites[Random.Range(0, stalactites.Count)];

            stalactite.Fall();

            stalactites.Remove(stalactite);
        }

        slamHitbox.HitOnce(slamDamage, Element.Physical, this, hitType:HitType.Attack, impact:300);
    }


    #endregion
}

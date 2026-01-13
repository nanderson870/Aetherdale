using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class IthindarRanger : StatefulCombatEntity, IWeaponBehaviourWielder
{
    [Header("Config")]
    public IthindarCrossbowBehaviour crossbowBehaviour;
    public Laser laserSight;

    [Header("Ranged Attacks")]
    public float aimedShotCooldown = 5.0F;
    public float aimedShotMaxRange = 100.0F;
    public float aimedShotMinRange  = 10.0F;

    [Header("Alternate Attacks")]
    public float bargeDistance = 5.0F;
    public Hitbox bargeHitbox;
    public float bargeCooldown = 10.0F;
    public int bargeDamage = 15;
    public float bargeStunDuration = 1.5F;

    [Header("Teleport Escape")]
    public float teleportChargeDuration = 2;
    public float teleportCooldown = 60;
    public float teleportMaxDistance = 75;
    public float teleportMinDistance = 50;
    public float teleportEnemyRange = 8;

    public VisualEffect teleportVFXPrefab;
    public float teleportParticleMaxSpeed = 40;
    public Material teleportDissolveMaterial;

    public VisualEffect warpSparksVFXPrefab;


    float aimedShotCooldownRemaining = 0;
    float bargeCooldownRemaining = 0;
    float teleportCooldownRemaining = 0;

    public Transform weaponTransform => throw new System.NotImplementedException();

    bool IWeaponBehaviourWielder.sprinting => false;

    [SyncVar] Vector3 aimedPosition = new();

    

    VisualEffect teleportVFXInstance;
    public override void Start()
    {
        base.Start();

        crossbowBehaviour.SetWielder(this);

        teleportCooldownRemaining = 10.0F; // prevent teleporting for a few seconds after spawn
    }

    public override void Update()
    {
        base.Update();

        ProcessCooldown(ref aimedShotCooldownRemaining);

        ProcessCooldown(ref bargeCooldownRemaining);

        ProcessCooldown(ref teleportCooldownRemaining);
    }

    // TODO maybe we want this functionality in entity?
    public static void ProcessCooldown(ref float cooldownCounter)
    {
        if (cooldownCounter > 0)
        {
            cooldownCounter = Mathf.Clamp(cooldownCounter - Time.deltaTime, 0, Mathf.Infinity);
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
    
    protected override State GetPreferredState()
    {   
        State currentState = stateMachine.GetState();

        Entity nearestEnemy = GetNearestEnemy(aggroRadius);
        float distanceToNearest = Mathf.Infinity;
        if (nearestEnemy != null)
        {
            distanceToNearest = Vector3.Distance(transform.position, nearestEnemy.transform.position);
        }

        Entity preferredEnemy = GetPreferredEnemy(aggroRadius);


        //Debug.Log(currentState);
        if (!currentState.ReadyForExit())
        {
            return null;
        }

        if (nearestEnemy != null)
        {
            if (currentState is not TeleportAwayState && CanTeleport() && distanceToNearest < teleportEnemyRange)
            {
                return new TeleportAwayState(this, nearestEnemy);
            }
            else 
            if (currentState is not AimedShotState && aimedShotCooldownRemaining <= 0 && ShouldAimedShot(preferredEnemy))
            {
                return new AimedShotState(this, preferredEnemy);
            }
            else 
            if (currentState is not PursuitState)
            {
                return CreatePursuitState(preferredEnemy);
            }
        }
        else if (currentState is not WanderState)
        {
            return CreateWanderState();
        }

        return null;
    }

    public override void Attack(Entity target)
    {
        if (CanBarge(target))
        {
            Barge(target);
        }
    }

    public override bool CanAttack(Entity target)
    {
        if (PreventedFromAttacking())
        {
            return false;
        }

        // Check for barge
        if (CanBarge(target))
        {
            return true;
        }

        return false;
    }

    public bool ShouldAimedShot(Entity target)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        return distanceToTarget > aimedShotMinRange && distanceToTarget <= aimedShotMaxRange;
    }


    public bool CanBarge(Entity target)
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        return distanceToTarget <= bargeDistance && bargeCooldownRemaining <= 0;
    }

    void Barge(Entity target)
    {
        animator.Play("CrossbowBarge");
        bargeCooldownRemaining = bargeCooldown;
    }

    void OnBargeHit(HitboxHitData hitData)
    {
        hitData.hitEntity.Stun(bargeStunDuration);
    }

    
    public bool CanTeleport()
    {
        return !IsStunned() && teleportCooldownRemaining <= 0;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (teleportVFXInstance != null)
        {
            teleportVFXInstance.SendEvent("Stop");
            Destroy(teleportVFXInstance.gameObject);
        }
    }

    [ClientRpc]
    void RpcStartTeleportCharge()
    {
        if (teleportVFXInstance != null)
        {
            teleportVFXInstance.SendEvent("Stop");
            Destroy(teleportVFXInstance.gameObject);
        }

        teleportVFXInstance = Instantiate(teleportVFXPrefab, transform.position, transform.rotation);
        teleportVFXInstance.SetFloat("Speed", 0);
        teleportVFXInstance.SendEvent("Start");

        RpcSetMaterial(teleportDissolveMaterial.name, MaterialChangeProperties.ColorAndEmission);

        animator.SetBool("chargingTeleport", true);
        PlayAnimation("EnterTeleportCharge", 0.2F);
    }

    [ClientRpc]
    void RpcStopTeleportCharge()
    {
        if (teleportVFXInstance != null)
        {
            teleportVFXInstance.SendEvent("Stop");
            Destroy(teleportVFXInstance.gameObject);
        }

        animator.SetBool("chargingTeleport", false);
        RpcResetMaterialsColors();
    }

    [ClientRpc]
    void RpcSetTeleportChargeRatio(float ratio)
    {
        if (teleportVFXInstance != null)
        {
            teleportVFXInstance.SetFloat("Speed", Mathf.Lerp(0, teleportParticleMaxSpeed, ratio));
        }

        RpcSetMaterialFloat("_Dissolve", 1.0F - ratio);
        ResetColors();
    }

    [ClientRpc]
    void RpcTeleported(Vector3 previousPosition, Vector3 newPositon)
    {
        Instantiate(warpSparksVFXPrefab, previousPosition, Quaternion.identity);
        Instantiate(warpSparksVFXPrefab, newPositon, Quaternion.identity);
    }

    #region Animator Callbacks
    void BargeStart()
    {
        if (isServer)
        {
            bargeHitbox.StartHit(bargeDamage, Element.Physical, HitType.Attack, this, impact:50, onHitAction:OnBargeHit);
            attacking = true;
        }
    }

    void BargeEnd()
    {
        if (isServer)
        {
            attacking = false;
            bargeHitbox.EndHit();
        }
    }
    #endregion


    #region IWeaponBehaviourWielder
    public int GetAttackDamage()
    {
        return 30;
    }

    public Vector3 GetAimedPosition()
    {
        return aimedPosition;
    }

    public void EquipWeapon(WeaponData weaponData, bool dropPrevious=false)
    {
    }

    public void EquipWeaponBehaviour(WeaponBehaviour weaponBehaviour, bool dropPrevious)
    {
    }

    public void StartWeaponHit(int damage, Element damageType, HitType hitType, int impact)
    {
    }

    public void EndWeaponHit()
    {
    }

    public WeaponData GetEquippedWeaponData()
    {
        return crossbowBehaviour.weaponData;
    }
    #endregion



    #region Behavior

    public class AimedShotState : State
    {
        readonly float chargeTime = 3.0F;

        IthindarRanger ranger;
        Entity target;
        Laser laserSight;
        float startTime;

        bool done = false;

        public AimedShotState(IthindarRanger ranger, Entity target)
        {
            this.ranger = ranger;
            this.target = target;

            this.updateInterval = 0.008F;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            ranger.SetAnimatorTrigger("EnterAim");

            startTime = Time.time;

            ranger.aimedShotCooldownRemaining = ranger.aimedShotCooldown;

            ranger.crossbowBehaviour.StartCharging();

            ranger.navMeshAgent.velocity = Vector3.zero;
            ranger.ClearDestination();
        }

        public override void Update()
        {
            base.Update();

            if (target == null || !ranger.SeesEntity(target) || !target.gameObject.activeSelf)
            {
                done = true;
                return;
            }

            
            if (Time.time - startTime < chargeTime - 0.05F)
            {
                ranger.TurnTowards(target.gameObject, 360);

                ranger.aimedPosition = target.GetWorldPosCenter();

                if (laserSight != null)
                {
                    laserSight.SetPositions(ranger.crossbowBehaviour.transform.Find("AmmoHolder").position, target.GetWorldPosCenter());
                }
            }


            if (Time.time - startTime < 0.25F * chargeTime)
            {
                return;
            }

            if (laserSight == null)
            {
                laserSight = Laser.Create(ranger.laserSight, ranger.crossbowBehaviour.transform.Find("AmmoHolder").position, ranger.aimedPosition, Quaternion.identity, ranger, 0, Element.Dark, 1.0F, Mathf.Infinity);
            }

        }

        public override void OnExit()
        {
            base.OnExit();

            if (ranger != null)
            {
                ranger.SetAnimatorTrigger("ExitAim");

                ranger.crossbowBehaviour.FinishCharging();
                ranger.crossbowBehaviour.FireLaser(ranger.aimedPosition);

                if (laserSight != null)
                {
                    NetworkServer.UnSpawn(laserSight.gameObject);
                    Destroy(laserSight.gameObject);
                }
            }

        }

        public override bool ReadyForExit()
        {
            return Time.time >= startTime + chargeTime || done;
        }
    }


    public class TeleportAwayState : State
    {
        IthindarRanger ranger;

        Vector3 destination;

        float chargeDurationRemaining = 0;
        bool charging = false;
        bool done = false;
        

        public TeleportAwayState(IthindarRanger ranger, Entity target)
        {
            this.ranger = ranger;
        }

        public override void OnEnter()
        {
            // Get radius between min and max teleport ranges
            float radiusDifference = ranger.teleportMaxDistance - ranger.teleportMinDistance;
            Vector2 position = Random.insideUnitCircle * radiusDifference;

            // Scale it out by adding min radius to its offset
            Vector2 offset = position.normalized * ranger.teleportMinDistance;

            Vector2 finalXY = position + offset;
        
            if (NavMesh.SamplePosition(ranger.transform.position + new Vector3(finalXY.x, 0, finalXY.y), out NavMeshHit hit, 100.0F, NavMesh.AllAreas))
            {
                destination = hit.position;
            }

            charging = true;
            chargeDurationRemaining = ranger.teleportChargeDuration;

            ranger.teleportCooldownRemaining = ranger.teleportCooldown;

            ranger.RpcStartTeleportCharge();
        }

        public override void Update()
        {
            if (done)
            {
                return;
            }

            if (ranger.IsStunned())
            {
                // Interrupt on stun
                End();
                return;
            }

            if (charging)
            {
                chargeDurationRemaining -= Time.deltaTime;

                ranger.RpcSetTeleportChargeRatio((ranger.teleportChargeDuration - chargeDurationRemaining) / ranger.teleportChargeDuration);
            }

            if (chargeDurationRemaining < 0 && !done)
            {
                charging = false;
                ranger.RpcTeleported(ranger.transform.position, destination);
                ranger.ChangePosition(destination);
                End();
            }
        }

        void End()
        {
            ranger.RpcStopTeleportCharge();
            done = true;
        }

        public override bool ReadyForExit() => done;
    }
    #endregion
}

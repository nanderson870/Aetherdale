using UnityEngine;
using Mirror;
using System.Collections;
using FMODUnity;
using UnityEngine.VFX;

public class Overgrowth : IdolForm
{
    [Header("Attacks")]
    [SerializeField] EventReference attackSound;
    [SerializeField] VisualEffect attack1VFX;
    [SerializeField] VisualEffect attack2VFX;

    [Header("Furious Lash")]
    [SerializeField] Hitbox furiousLashHitbox;
    readonly int furiousLashDamage = 45;
    readonly float furiousLashKnockupStrength = 6.0F;

    [Header("Ensnaring Rush")]
    [SerializeField] Projectile ensnaringRushLeadProjectile;
    [SerializeField] OvergrowthVine ensnaringRushTrailingVine;
    [SerializeField] Transform ensnaringRushOriginTransform;
    [SerializeField] GameObject ensnaringRushIndicatorPrefab;
    readonly float ensnaringRushProjectileVelocity = 160.0F;
    readonly float ensnaringRushPullVelocity = 45.0F;
    readonly float ensnaringRushEnemyPullForceMult = 2.0F; /* sqrt(distance) * this */

    [Header("Dominion of Vines")]
    [SerializeField] LashingVine ultVinePrefab;
    readonly float ultSpawnRadius = 8.0F;
    readonly float abilityStrengthPerVine = 0.2F; // 0.2F = 20% ability strength


    [SyncVar] bool usingLash = false;

    [SyncVar] bool aimingEnsnaringRush = false;
    [SyncVar] bool inEnsnaringRush = false;
    [SyncVar] float ensnaringRushStart;
    [SyncVar] Projectile currentEnsnaringRushProjectileInstance = null;
    OvergrowthVine currentEnsnaringRushVine = null;
    GameObject currentEnsnaringRushIndicator = null;

    readonly SyncList<LashingVine> currentUltVines = new();


    public override void Update()
    {
        base.Update();

        if (aimingEnsnaringRush)
        {
            if (isOwned)
            {
                float projectileDistance = ensnaringRushLeadProjectile.GetLifespan() * ensnaringRushProjectileVelocity;
                
                if (currentEnsnaringRushIndicator != null)
                {
                    currentEnsnaringRushIndicator.transform.position = GetCamera().GetAimedPosition();
                }
            }
        }
        
        if (inEnsnaringRush && currentEnsnaringRushProjectileInstance != null)
        {
            float distance = Vector3.Distance(ensnaringRushOriginTransform.position, currentEnsnaringRushProjectileInstance.transform.position);

            if (isServer && Time.time - ensnaringRushStart >= 0.25F)
            {
                // Check for exit conditions
                if (distance <= globalVelocity.magnitude * 0.05F + 1.0F || globalVelocity.magnitude < 0.1F)
                {
                    // Done ensnaring rush
                    TearDownEnsnaringRush(null);
                    return; 
                }

            }

            // Process ensnaring rush
            if (isOwned && currentEnsnaringRushProjectileInstance.Stuck())
            {
                Vector3 direction = currentEnsnaringRushProjectileInstance.transform.position - transform.position;
                globalVelocity = Vector3.Lerp(globalVelocity, ensnaringRushPullVelocity * direction.normalized, 25.0F * Time.deltaTime);
            }
        }
    }

    protected override void Animate()
    { 
        base.Animate();

        animator.SetBool("airborne", airborne && !inEnsnaringRush);

        Vector2 horizontalVelocity = new(GetLocalVelocity().x, GetLocalVelocity().z);
        if (horizontalVelocity.magnitude > 1F && !airborne && !inEnsnaringRush)
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 1.0F);
        }
        else
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 0.0F);
        }
    }

    
    [ClientRpc]
    public override void RpcSetActive(bool active)
    {
        base.RpcSetActive(active);

        if (active == false)
        {
            if (currentEnsnaringRushIndicator != null)
            {
                Destroy(currentEnsnaringRushIndicator.gameObject);
            }
        }
    }

    [Command]
    protected override void CmdAttack1()
    {
        if (aimingEnsnaringRush)
        {
            StartEnsnaringRush();
        }
        else
        {
            base.CmdAttack1();
        }
    }

    [Command]
    public override void CmdReleaseAttack1()
    {
        if (inEnsnaringRush)
        {
            TearDownEnsnaringRush(currentEnsnaringRushProjectileInstance);
        }
    }

    #region FURIOUS LASH

    public override bool CanAbility1()
    {
        return base.CanAbility1() && !usingLash;
    }

    [Server]
    protected override void Ability1()
    {
        if (!CanAbility1())
        {
            return;
        }

        CastedAbility1();
        SetAnimatorTrigger("FuriousLash");

        TargetLashStart();
        RpcSetAttackMode();

        usingLash = true;
    }

    [TargetRpc]
    void TargetLashStart()
    {
        // Add camera context to show the lash
        cameraContext.AddOffset(new Vector3(0, 0.2F, 0.2F));
    }

    [ServerCallback] //animator
    public void FuriousLashHit()
    {
        foreach(HitInfo hitResult in furiousLashHitbox.HitOnce(furiousLashDamage, Element.Nature, this, hitType:HitType.Ability))
        {
            if (hitResult.entityHit == null)
            {
                continue;
            }
            
            hitResult.entityHit.Push(Vector3.up * furiousLashKnockupStrength, forceMode:ForceMode.Force);

            if (!hitResult.staggeredTarget)
            {
                hitResult.entityHit.Stagger();
            }
        }
    }

    
    [ServerCallback]
    public void FuriousLashEnd()
    {
        usingLash = false;

        TargetLashEnd();
    }

    [TargetRpc]
    public void TargetLashEnd()
    {
        cameraContext.RemoveOffset(new Vector3(0, 0.2F, 0.2F));
    }

    #endregion


    #region ENSNARING RUSH
    [Server]
    protected override void Ability2()
    {
        if (inEnsnaringRush || aimingEnsnaringRush || !CanAbility2())
        {
            return;
        }

        TargetEnterAimMode(true);
        TargetStartAbility2Aim();

        aimingEnsnaringRush = true;

        animator.SetBool("aimingEnsnaringRush", true);
    }

    [Server]
    protected override void ReleaseAbility2()
    {
        if (aimingEnsnaringRush)
        {
            StopAimingEnsnaringRush();
        }
    }

    [Server]
    void StopAimingEnsnaringRush()
    {
        if (!aimingEnsnaringRush || !CanAbility2())
        {
            return;
        }

        aimingEnsnaringRush = false;

        animator.SetBool("aimingEnsnaringRush", false);

        RpcReleaseAbility2();
        TargetExitAimMode();
    }

    [Server]
    void StartEnsnaringRush()
    {
        if (!CanAbility2())
        {
            return;
        }

        StopAimingEnsnaringRush();

        RpcSetUpRushVine();

        CastedAbility2();

        inEnsnaringRush = true;
        ensnaringRushStart = Time.time;

        Vector3 projectileVelocity = (GetCamera().GetAimedPosition() - ensnaringRushOriginTransform.position).normalized * ensnaringRushProjectileVelocity;

        currentEnsnaringRushProjectileInstance = Projectile.Create(ensnaringRushLeadProjectile, ensnaringRushOriginTransform.position, ensnaringRushOriginTransform.rotation, gameObject, projectileVelocity);
        currentEnsnaringRushProjectileInstance.OnStick += RushToProjectile;
        currentEnsnaringRushProjectileInstance.OnFlightEnd += TearDownEnsnaringRush;

        SetAnimatorTrigger("EnsnaringRush");
    }

    [TargetRpc]
    void TargetStartAbility2Aim()
    {

        Reticle.AddRightKeybindPrompt("attack", "Grapple");

        SetRotationTrackCamera(true);

        //GetOwningPlayer().GetUI().ShowReticle();

        currentEnsnaringRushIndicator = Instantiate(ensnaringRushIndicatorPrefab);
    }


    [ClientRpc]
    void RpcReleaseAbility2()
    {
        if (isOwned)
        {
            Reticle.ClearKeybindPrompts();

            SetRotationTrackCamera(false);

            //GetOwningPlayer().GetUI().HideReticle();

            Destroy(currentEnsnaringRushIndicator);
        }

    }

    [ClientRpc]
    void RpcSetUpRushVine()
    {
        currentEnsnaringRushVine = Instantiate(ensnaringRushTrailingVine);

        currentEnsnaringRushVine.SetStartTransform(ensnaringRushOriginTransform);

        StartCoroutine(nameof(SetupProjectileTransformWhenAvailable));
    }

    IEnumerator SetupProjectileTransformWhenAvailable()
    {
        float startTime = Time.time;
        while (currentEnsnaringRushProjectileInstance == null && Time.time - startTime <= 0.5F)
        {
            yield return null;
        }

        currentEnsnaringRushVine.SetEndTransform(currentEnsnaringRushProjectileInstance.transform);
    }

    [Server]
    void RushToProjectile(Entity target)
    {
        if (currentEnsnaringRushProjectileInstance == null)
        {
            Debug.LogError("RushToProjectile() called but there is no current projectile instance");
            return;
        }
        else
        {
            if (target != null)
            {
                Vector3 towardsOvergrowth = transform.position - target.transform.position;

                target.Push(ensnaringRushEnemyPullForceMult * Mathf.Sqrt(towardsOvergrowth.magnitude) * towardsOvergrowth.normalized );
            }
        }
    }

    [Server]
    void TearDownEnsnaringRush(Projectile projectile)
    {
        SetAnimatorTrigger("EndEnsnaringRush");
        inEnsnaringRush = false;
        if (currentEnsnaringRushProjectileInstance != null)
        {
            NetworkServer.UnSpawn(currentEnsnaringRushProjectileInstance.gameObject);
            Destroy(currentEnsnaringRushProjectileInstance.gameObject);
        }
        currentEnsnaringRushProjectileInstance = null;

        RpcTearDownEnsnaringRush();
    }

    [ClientRpc]
    void RpcTearDownEnsnaringRush()
    {
        globalVelocity = new(0, globalVelocity.y * 0.5F, 0);

        Destroy(currentEnsnaringRushVine.gameObject);
    }
    
    #endregion


    #region DOMINION OF VINES
    [Server]
    protected override void UltimateAbility()
    {
        if (!CanUltimate())
        {
            return;
        }
        
        currentUltVines.Clear();

        int numVines = 2 + (int) ((GetStat(Stats.AbilityStrength, 1.0F) - 1) / abilityStrengthPerVine);

        for (int i = 0; i < numVines; i++)
        {
            Vector2 spawnOffset2D = Random.insideUnitCircle * ultSpawnRadius;
            Vector3 spawnOffset = new(spawnOffset2D.x, 0, spawnOffset2D.y);

            // Raycast from above the spawn offset, spawn the vine on the ground if it hits
            if (Physics.Raycast(transform.position + spawnOffset + new Vector3(0, 10.0F, 0), Vector3.down, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("Default")))
            {
                LashingVine newUltVine = Instantiate(ultVinePrefab, hitInfo.point, transform.rotation);
                NetworkServer.Spawn(newUltVine.gameObject, connectionToClient);

                currentUltVines.Add(newUltVine);

                newUltVine.SetOwningPlayer(owningPlayer);
                newUltVine.SetFaction(GetFaction());
                newUltVine.SetStat(Stats.AttackDamageMultiplier, GetStat(Stats.AttackDamageMultiplier));
                newUltVine.SetStat(Stats.AttackSpeed, GetStat(Stats.AttackSpeed));

                newUltVine.OnHitEntity += OnHitEntity;
            }
        }

        CastedUltimateAbility();
    }


    #endregion

    public void Attack1Hit()
    {
        if (isServer)
        {
            attackHitbox.HitOnce(attackDamage, Element.Nature, this, hitType: HitType.Attack);
        }

        attack1VFX.SendEvent("Play");
    }

    public void Attack2Hit()
    {
        if (isServer)
        {
            attackHitbox.HitOnce(attackDamage, Element.Nature, this, hitType:HitType.Attack);
        }

        attack2VFX.SendEvent("Play");
    }

    public void AttackSound()
    {
        AudioManager.Singleton.PlayOneShot(attackSound, transform.position);
    }


    public override bool CanMove()
    {
        return base.CanMove() && !usingLash;
    }

    public override bool CanAttack()
    {
        return base.CanAttack() && !usingLash && !inEnsnaringRush;
    }

    protected override void Dodge(Vector3 dodgeDirection)
    {
        base.Dodge(dodgeDirection);

        inEnsnaringRush = false;
        usingLash = false;
    }
}

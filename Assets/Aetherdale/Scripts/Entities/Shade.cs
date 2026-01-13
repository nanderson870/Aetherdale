using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using FMODUnity;
using UnityEngine.VFX;

public class Shade : IdolForm
{
    [Header("Attacks")]
    [SerializeField] Hitbox attack2Hitbox;
    [SerializeField] Hitbox doublestrikeHitbox;  
    [SerializeField] EventReference attackSwingSound;
    [SerializeField] EventReference attackHitSound;
    [SerializeField] VisualEffect attack1RightVFX;
    [SerializeField] VisualEffect attack1LeftVFX;


    [Header("Warp")]
    [SerializeField] AreaOfEffect warpExplosion;
    [SerializeField] GameObject warpIndicatorPrefab;
    readonly int warpExplosionDamage = 25;
    readonly float freeWarpDistance = 35.0F;
    readonly float maxTargetWarpDistance = 45.0F;
    readonly float warpDissolveDuration = 0.2F;
    readonly float warpStayDissolvedDuration = 0.5F;

    [Header("Vanish")]
    [SerializeField] InvisibilityEffect shadeInvisibility;

    [Header("Mark")]
    [SerializeField] TargetPaintMode markTargetPaint;
    [SerializeField] Effect markEffect;
    readonly float markTransferRange = 20.0F;


    // -- Runtime --
    OutlineTrigger warpIndicator;

    Vector3 warpTargetPosition; // only server matters, gets updated by client

    bool warping = false; 
    bool marking = false;
    bool dissolved = false;


    public override void Update()
    {
        base.Update();

        if (isOwned)
        {
            if (warpIndicator != null)
            {
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hitInfo, maxTargetWarpDistance, LayerMask.GetMask("Default", "Entities")))
                {  
                    warpIndicator.transform.position = hitInfo.point;
                }
                else
                {
                    warpIndicator.transform.position = Camera.main.transform.position + Camera.main.transform.forward * freeWarpDistance;
                }

                CmdUpdateWarpTargetPosition(warpIndicator.transform.position);
            }
        }
    }

    [ClientRpc]
    public override void RpcSetActive(bool active)
    {
        base.RpcSetActive(active);

        if (active == false)
        {
            if (warpIndicator != null)
            {
                Destroy(warpIndicator.gameObject);
            }
        }
    }

    [Command]
    void CmdUpdateWarpTargetPosition(Vector3 position)
    {
        warpTargetPosition = position;
    }


    // Shade floats, no surface grip is applicable
    public override float GetSurfaceGrip()
    {
        return 1.0F;
    }
    
    #region VANISH
    [Server]
    protected override void Ability1() // Vanish
    {
        if (invisible || !CanAbility1())
        {
            return;
        }

        AddEffect(shadeInvisibility, this);

        CastedAbility1();
    }

    #endregion VANISH

    #region WARP
    [Server]
    protected override void Ability2()
    {
        if (warping || marking || !CanAbility2())
        {
            return;
        }

        warping = true;

        TargetCreateWarpIndicator();

    }

    [TargetRpc]
    void TargetCreateWarpIndicator()
    {
        warpIndicator = Instantiate(AetherdaleData.GetAetherdaleData().outlineTriggerSphere, transform);
        float aoeRadius = warpExplosion.GetRadius(this);

        warpIndicator.SetScale(new Vector3(aoeRadius, aoeRadius, aoeRadius));
        warpIndicator.transform.SetParent(null);

        // Add camera context so we can see where we're aiming
        cameraContext.AddOffset(new Vector3(1.5F, 1.0F, 2.0F));

        SetRotationTrackCamera(true);
    }

    [Server]
    protected override void ReleaseAbility2()
    {
        if (!warping)
        {
            return;
        }

        TargetDissolve();
        TargetTeardownWarpIndicator();

        
        Invoke(nameof(CompleteWarp), warpStayDissolvedDuration);
        
        RpcFallInterrupt();

        CastedAbility2();
    }

    [Server]
    void CompleteWarp()
    {
        AreaOfEffect.AOEProperties properties = AreaOfEffect.Create(warpExplosion, warpTargetPosition, this, HitType.Ability);
        properties.damage = warpExplosionDamage;

        TargetSetPosition(warpTargetPosition);
        RpcFallInterrupt();
        warping = false;
        TargetUndissolve();
    }

    [TargetRpc]
    void TargetTeardownWarpIndicator()
    {

        if (warpIndicator != null)
            Destroy(warpIndicator.gameObject);


        // Return camera context to normal
        cameraContext.RemoveOffset(new Vector3(1.5F, 1.0F, 2.0F));

        SetRotationTrackCamera(false);
    }

    [TargetRpc]
    void TargetDissolve()
    {
        StartCoroutine(nameof(Dissolve));
    }

    [TargetRpc]
    void TargetUndissolve()
    {
        StartCoroutine(nameof(Undissolve));
    }


    IEnumerator Dissolve()
    {
        float startTime = Time.time;

        while (Time.time - startTime <= warpDissolveDuration)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    if(material.HasFloat("_Dissolve"))
                    {
                        material.SetFloat("_Dissolve",  1 - ((Time.time - startTime) / warpDissolveDuration));
                    }
                }
            }

            yield return null;
        }
        dissolved = true;
    }

    
    IEnumerator Undissolve()
    {
        dissolved = false;
        float startTime = Time.time;

        while (Time.time - startTime <= warpDissolveDuration)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    if(material.HasFloat("_Dissolve"))
                    {
                        material.SetFloat("_Dissolve",  (Time.time - startTime) / warpDissolveDuration);
                    }
                }
            }


            yield return null;
        }
    }

    #endregion WARP


    #region MARK
    [Server]
    protected override void UltimateAbility()
    {
        if (warping || marking || !CanUltimate())
        {
            return;
        }

        marking = true;

        TargetEnterMarkTargetPaint();
    }

    [TargetRpc]
    void TargetEnterMarkTargetPaint()
    {
        EnterTargetPaintMode(markTargetPaint);
        
        SetRotationTrackCamera(true);
    }

    [Server]
    protected override void ReleaseUltimateAbility()
    {
        TargetExitMarkTargetPaint();

        if (!marking || !CanUltimate())
        {
            return;
        }

        marking = false;
    }

    [TargetRpc]
    void TargetExitMarkTargetPaint()
    {
        List<Entity> painted = ClearTargetPaintMode();

        if (painted.Count > 0)
        {
            Entity markedTarget = painted[0];
            CmdMarkTarget(markedTarget);
        }

        SetRotationTrackCamera(false);
    }

    [Command]
    void CmdMarkTarget(Entity target)
    {
        if (!CanUltimate())
        {
            return;
        }

        CastedUltimateAbility();
        MarkTarget(target);
    }

    [Server]
    void MarkTarget(Entity target)
    {
        EffectInstance markEffectInst = target.AddEffect(markEffect, this);
        markEffectInst.OnTargetDeath += MarkConsecutiveTarget;
        markEffectInst.OnTargetDeath += (_, _, _) => {AddEnergy(Ability1Cost);};
    }

    [Server]
    void MarkConsecutiveTarget(Entity deadTarget, Entity origin, Vector3 deathPosition)
    {
        Entity closest = WorldManager.GetWorldManager().GetNearestEntity(deathPosition, markTransferRange, excludeEntity:deadTarget, excludeAlliesOf:this);

        if (closest != null)
        {
            MarkTarget(closest);
        }
    }

    #endregion MARK


    #region ATTACKS

    [Server]
    public override void Attack2()
    {
        if (attacking && !animator.GetCurrentAnimatorStateInfo(0).IsName("Doublestrike"))
        {
            SetAnimatorTrigger("Doublestrike");
        }
    }

    [ServerCallback]
    public void AttackStart()
    {
        AudioManager.Singleton.PlayOneShot(attackSwingSound, transform.position);
    }

    public void AttackHit()
    {
        if (isServer)
        {
            if (attackHitbox.HitOnce(attackDamage, Element.Dark, this).Count > 0)
            {
                RpcPlayAttackHitSound();
            }
        }

        attack1LeftVFX.SendEvent("Play");
        attack1RightVFX.SendEvent("Play");
    }

    public void Attack2Hit()
    {
        if (isServer)
        {
            if (attack2Hitbox.HitOnce(attackDamage, Element.Dark, this).Count > 0)
            {
                RpcPlayAttackHitSound();
            }
        }


    }

    [ServerCallback]
    public void DoublestrikeHit()
    {
        if (doublestrikeHitbox.HitOnce(attackDamage, Element.Dark, this).Count > 0)
        {  
            RpcPlayAttackHitSound();
        }
    }

    [ClientRpc]
    public void RpcPlayAttackHitSound()
    {
        AudioManager.Singleton.PlayOneShot(attackHitSound, transform.position);
    }

    [ServerCallback]
    public void OpenFollowupWindow()
    {
    }

    [ServerCallback]
    public void CloseFollowupWindow()
    {
    }

    #endregion

    public override bool CanMove()
    {
        return base.CanMove() && !dissolved;
    }

}

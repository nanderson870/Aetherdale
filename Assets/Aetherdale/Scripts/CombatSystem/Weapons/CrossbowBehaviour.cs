
using FMODUnity;
using Mirror;
using UnityEngine;

public class CrossbowBehaviour : WeaponBehaviour
{
    public Projectile projectile;
    public float projectileVelocity = 130.0F;

    public override void Start()
    {
        base.Start();

        if (projectile == null)
        {
            projectile = AetherdaleData.GetAetherdaleData().wraithBoltProjectile;
        }
    }

    public override bool CanAttack2()
    {
        return true;
    }

    /// <summary>
    /// Fires this weapon
    /// </summary>
    /// <param name="projectileToFire">Optional. Allows overriding the default fired projectile with a specific one.</param>
    [Server]
    public void Fire(Projectile projectileToFire = null)
    {
        if (projectileToFire == null)
        {
            projectileToFire = projectile;
        }

        Transform projectileSpawnTransform = transform.Find("AmmoHolder");

        Vector3 shootDirection = wielder.GetAimedPosition() - projectileSpawnTransform.position;

        Projectile projectileInst = Projectile.Create(projectileToFire, projectileSpawnTransform, wielder.gameObject, shootDirection.normalized * projectileVelocity);
        projectileInst.SetDamage(wielder.GetAttackDamage());

        
        wielder.gameObject.GetComponent<Entity>().GetOwningPlayer().GetCamera().RpcApplyZoomRecoil(.75f);
        wielder.gameObject.GetComponent<Entity>().GetOwningPlayer().GetCamera().TargetApplyScreenShake(.025F, 0.04F, 0.0125F);

        RpcShootRangedWeapon();
    }

    [Server]
    public void FireWithPrediction(Entity target, float predictionStrength = 0.5F, Projectile projectileToFire = null)
    {
        if (projectileToFire == null)
        {
            projectileToFire = projectile;
        }

        Transform projectileSpawnTransform = transform.Find("AmmoHolder");

        Projectile projectileInst = Projectile.FireAtEntityWithPrediction(wielder.gameObject.GetComponent<Entity>(), target, projectileToFire, projectileSpawnTransform.position, projectileVelocity, predictionStrength);
        projectileInst.SetDamage(wielder.GetAttackDamage());

        RpcShootRangedWeapon();
    }

    [Server]
    public override bool PerformAttack1()
    {
        // Check if attack sequence should reset
        if (nextAttackIndex >= attackAnimationNames.Length)
        {
            nextAttackIndex = 0;
        }

        if (attackAnimationNames.Length == 0)
        {
            Debug.LogWarning("No attack animations available");
            return false;
        }

        float interval = GetAttackInterval() / wielder.GetStat(Stats.AttackSpeed);

        // Check if we're currently timed out of making another attack
        float timeSinceLastAttack = Time.time - lastAttack;
        if (timeSinceLastAttack < interval * 0.75F)
        {
            return false;
        }

        lastAttack = Time.time;

        // Check if attack sequence should reset
        if (timeSinceLastAttack > interval * 1.5F)
        {
            nextAttackIndex = 0;
        }

        wielder.PlayAnimation(attackAnimationNames[nextAttackIndex], 0.05F);

        nextAttackIndex++;

        RpcDrawRangedWeapon();

        return true;
    }

    [Server]
    public override bool PerformAttack2()
    {
        wielder.TargetEnterAimMode(true);
        wielder.RpcSetAnimatorBool("aimingWeapon", true); // TODO merge into TargetExitAimMode when spear aiming is better integrated
        wielder.ActivateRig("crossbowAimRig");
        return true;
    }


    [Server]
    public override bool ReleaseAttack2()
    {
        wielder.TargetExitAimMode();
        wielder.RpcSetAnimatorBool("aimingWeapon", false); // TODO merge into TargetExitAimMode when spear aiming is better integrated
        wielder.DeactivateRig("crossbowAimRig");
        return true;
    }

    [ClientRpc]
    public void RpcShootRangedWeapon()
    {
        animator.SetTrigger("Shoot");
        PlayAttackSound();

    }


    [ClientRpc]
    public void RpcDrawRangedWeapon()
    {
        animator.GetComponent<Animator>().SetTrigger("Draw");
    }

    public override float GetAimAssistMaxDistance()
    {
        return ControlledEntity.RANGED_AIM_ASSIST_MAX_DEFAULT;
    }

    public override void PerformJumpAttack1()
    {
    }

    public override Vector3 GetCameraOffset()
    {
        return Vector3.zero;//return new Vector3(1F, -.5F, 0);
    }

    public override EventReference GetEquipSound()
    {
        if (equipSound.IsNull)
        {
            return AetherdaleData.GetAetherdaleData().soundData.defaultCrossbowEquipSound;
        }

        return equipSound;
    }
}
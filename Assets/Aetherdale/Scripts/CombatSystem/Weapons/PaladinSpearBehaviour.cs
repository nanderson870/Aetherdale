using System.Collections;
using FMODUnity;
using UnityEngine;

public class PaladinSpearBehaviour : SpearBehaviour
{
    [SerializeField] Renderer lightBlastRenderer;
    [SerializeField] Projectile lightBlastProjectile;
    [SerializeField] EventReference specSound;

    const float THROW_RECHARGE = 5.0F;
    const float THROW_ATTACK_TIMEOUT = 0.5F;
    const float LIGHT_BLAST_VELOCITY = 45.0F;

    const float ENERGY_ACTIVE_ALPHA = 0.75F;
    const float ENERGY_ALPHA_LERP_VALUE = 2.0F;

    const float DAMAGE_MULT_PER_EXPLOSION = 4.0F;

    float throwRechargeRemaining = 0;

    public override void Update()
    {
        base.Update();
        
        if (throwRechargeRemaining > 0)
        {
            lightBlastRenderer.material.SetFloat("_Alpha", 0);
            throwRechargeRemaining -= Time.deltaTime;
        }
        else
        {
            float currentAlpha = lightBlastRenderer.material.GetFloat("_Alpha");

            lightBlastRenderer.material.SetFloat("_Alpha", Mathf.Lerp(currentAlpha, ENERGY_ACTIVE_ALPHA, ENERGY_ALPHA_LERP_VALUE * Time.deltaTime));
        }

    }

    public override bool CanAttack2()
    {
        return throwRechargeRemaining <= 0;
    }

    public override bool CanAttack1()
    {
        if (THROW_RECHARGE - throwRechargeRemaining < THROW_ATTACK_TIMEOUT)
        {
            Debug.Log(THROW_RECHARGE - throwRechargeRemaining);
            return false;
        }

        return base.CanAttack1();
    }

    public override bool CanThrowWeapon()
    {
        return base.CanThrowWeapon() && throwRechargeRemaining <= 0;
    }

    public override void ThrowWeapon()
    {
        wielder.RpcSetAnimatorBool("aimingSpear", false);
        wielder.TargetExitAimMode();

        wielder.PlayAnimation("SpearThrow", 0.05F);

        aiming = false;

        throwRechargeRemaining = THROW_RECHARGE;

        AudioManager.Singleton.PlayOneShot(specSound, transform.position);

        StartCoroutine(nameof(ThrowWeaponCoroutine)); // Invoke throw soon, to give animation some time
    }


    IEnumerator ThrowWeaponCoroutine()
    {
        yield return new WaitForSeconds(0.25F);
        Vector3 aimPos = wielder.GetAimedPosition();

        Vector3 throwDirection = (aimPos - transform.position).normalized;

        Projectile weaponProjectile = Projectile.Create(
            lightBlastProjectile,
            lightBlastRenderer.transform,
            wielder.gameObject,
            throwDirection * LIGHT_BLAST_VELOCITY
        );

        weaponProjectile.SetAOEDamage((int) (GetDamage() * DAMAGE_MULT_PER_EXPLOSION));
    }

    
}

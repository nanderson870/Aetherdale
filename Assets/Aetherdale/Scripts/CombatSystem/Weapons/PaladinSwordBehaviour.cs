
using UnityEngine;
using Mirror;
using System.Collections;
using FMODUnity;

public class PaladinSwordBehaviour : SwordWeaponBehaviour
{
    [SerializeField] Renderer lightBlastRenderer;
    [SerializeField] Projectile specProjectile;
    [SerializeField] EventReference specSound;

    const float SPEC_RECHARGE = 5.0F;
    const float SPEC_PROJECTILE_SPEED = 25.0F;

    const float ENERGY_ACTIVE_ALPHA = 0.75F;
    const float ENERGY_ALPHA_LERP_VALUE = 2.0F;

    const float DAMAGE_MULT_PER_EXPLOSION = 0.5F;

    float specRechargeRemaining = 0;

    public override void Update()
    {
        base.Update();
        
        if (specRechargeRemaining > 0)
        {
            lightBlastRenderer.material.SetFloat("_Alpha", 0);
            specRechargeRemaining -= Time.deltaTime;
        }
        else
        {
            float currentAlpha = lightBlastRenderer.material.GetFloat("_Alpha");

            lightBlastRenderer.material.SetFloat("_Alpha", Mathf.Lerp(currentAlpha, ENERGY_ACTIVE_ALPHA, ENERGY_ALPHA_LERP_VALUE * Time.deltaTime));
        }

    }

    public override bool CanAttack2()
    {
        return specRechargeRemaining <= 0;
    }

    [Server]
    public override bool PerformAttack2()
    {
        Debug.Log("Perform attack 2");

        specRechargeRemaining = SPEC_RECHARGE;

        wielder.PlayAnimation("Big 1H Slash", 0.05F);

        AudioManager.Singleton.PlayOneShot(specSound, transform.position);

        StartCoroutine(CreateSpecProjectiles());

        return true;
    }

    IEnumerator CreateSpecProjectiles()
    {
        yield return new WaitForSeconds(0.5F);

        Entity wieldingEntity = wielder.gameObject.GetComponent<Entity>();
        Vector3 originPos = wieldingEntity.GetWorldPosCenter() + wieldingEntity.transform.TransformVector(new Vector3(0, 0, 0.5F));

        float cameraX = wieldingEntity.GetCameraContext().transform.rotation.eulerAngles.x;

        for (int i = -30; i <= 30; i += 15)
        {
            Projectile projectile = Projectile.Create(specProjectile, originPos, Quaternion.identity, wielder.gameObject, Quaternion.Euler(0, i, 0) * wieldingEntity.transform.TransformVector(new Vector3(0.0F, 0.0F, SPEC_PROJECTILE_SPEED)));
            projectile.lifespanSeconds = 0.25F;
            projectile.hasLifespan = true;
            projectile.useGravity = false;

            projectile.SetAOEDamage((int) (GetDamage() * DAMAGE_MULT_PER_EXPLOSION));
        }
    }
}
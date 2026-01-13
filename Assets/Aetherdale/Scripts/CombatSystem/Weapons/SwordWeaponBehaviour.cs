
using UnityEngine;

public class SwordWeaponBehaviour : MeleeWeaponBehaviour
{
    public const float WIND_SLASH_VELOCITY = 60.0F;

    Projectile windSlashProjectile;

    float lastAttack1 = 0;


    public override void Start()
    {
        base.Start();

        windSlashProjectile = AetherdaleData.GetAetherdaleData().sword1HWindSlashProjectile;
    }

    public override bool CanAttack2()
    {
        return true;
    }

    AttackInputType lastAttackInputType = AttackInputType.None;
    public override void GiveInput(AttackInputType inputType)
    {
        Entity wielderEntity = wielder.gameObject.GetComponent<Entity>();
        if (inputType == AttackInputType.Attack2 && lastAttackInputType == AttackInputType.Attack1 && (Time.time - lastAttack1) < GetAttackInterval())
        {
            Windslash();
        }
        else
        {
            base.GiveInput(inputType);
        }

        lastAttackInputType = inputType;
    }

    public override bool PerformAttack1()
    {
        lastAttack1 = Time.time;

        return base.PerformAttack1();
    }

    public override bool PerformAttack2()
    {
        return base.PerformAttack2();
    }

    void Windslash()
    {
        lastAttack = Time.time;

        wielder.PlayAnimation("Radial Slash L to R", 0);

        Invoke(nameof(CreateWindslashProjectile), 0.3F);
    }

    void CreateWindslashProjectile()
    {
        Vector3 direction = (wielder.GetAimedPosition() - transform.position).normalized;
        Projectile projectile = Projectile.Create(windSlashProjectile, transform.position, Quaternion.identity, wielder.gameObject, direction * WIND_SLASH_VELOCITY);
        projectile.SetDamage(wielder.GetAttackDamage());
    }

    public override float GetAimAssistMaxDistance()
    {
        return ControlledEntity.MELEE_AIM_ASSIST_MAX_DEFAULT;
    }

}
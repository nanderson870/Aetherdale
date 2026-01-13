using FMODUnity;
using Mirror;
using UnityEngine;

public abstract class MeleeWeaponBehaviour : WeaponBehaviour
{
    public const float JUMP_ATTACK_DOWN_VELOCITY = 20.0F;

    public override bool CanJumpAttack()
    {
        return true;
    }

    public override bool CanDodgeAttack()
    {
        return true;
    }

    public override void PerformJumpAttack1()
    {
        Entity wielderEntity = wielder.gameObject.GetComponent<Entity>();
        lastAttack = Time.time;

        wielder.PlayAnimation("Jump Attack", 0.05F);

        wielder.gameObject.GetComponent<Entity>().SetAttacking(true);

        wielder.StartWeaponHit(wielder.GetAttackDamage() * 2, GetDamageType(), HitType.Attack, GetImpact() * 3);

        wielderEntity.inJumpAttack = true;
        wielderEntity.AddVelocity(Vector3.down * JUMP_ATTACK_DOWN_VELOCITY);
        wielderEntity.OnNextGrounded += wielder.EndWeaponHit;
        wielderEntity.OnNextGrounded += wielderEntity.JumpAttackDone;


        wielderEntity.RpcSetAttackMode();

        lastAttack = Time.time;

        RpcPerformJumpAttack();
    }

    [ClientRpc]
    void RpcPerformJumpAttack()
    {
        Entity wielderEntity = wielder.gameObject.GetComponent<Entity>();
        if (wielderEntity.isOwned)
        {
            wielderEntity.SwitchMovementToRigidBodyUntilGrounded(wielderEntity.GetVelocity());
        }

        wielder.gameObject.GetComponent<PlayerWraith>().PlaySword2HSlash3();
        wielder.gameObject.GetComponent<Rigidbody>().rotation = Quaternion.Euler(0, PlayerInput.Input.lookingAngle, 0);
        AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.defaultJumpAttackSound1HSword);
    }

    
    public override EventReference GetEquipSound()
    {
        if (equipSound.IsNull)
        {
            return AetherdaleData.GetAetherdaleData().soundData.defaultMeleeEquipSound;
        }

        return equipSound;
    }
}
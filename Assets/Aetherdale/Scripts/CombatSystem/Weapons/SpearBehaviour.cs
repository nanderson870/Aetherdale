using UnityEngine;
using Mirror;
using System.Collections;

public class SpearBehaviour : MeleeWeaponBehaviour
{
    const float WEAPON_THROW_VELOCITY = 45.0F;
    const float WEAPON_RECOLLECT_RANGE = 3.0F;
    const float WEAPON_AUTO_RECOLLECT_TIME = 10.0F;

    protected bool aiming = false;

    public override void Update()
    {
        base.Update();
        
        if (isServer && !inHand && !inFlight)
        {
            if ((Time.time - weaponThrowStart) >= WEAPON_AUTO_RECOLLECT_TIME
              || Vector3.Distance(transform.position, wielder.transform.position) <= WEAPON_RECOLLECT_RANGE)
            {
                PickupWeapon();
            }
        }
    }

    public override bool CanAttack2()
    {
        return inHand;
    }

    public override bool PerformAttack1()
    {
        if (!inHand)
        {
            return false;
        }

        if (CanThrowWeapon())
        {
            ThrowWeapon(); // Invoke throw soon, to give animation some time

            return true;
        }
        else
        {
            if (CanAttack1())
            {
                // Default attack
                return base.PerformAttack1();
            }
        }

        return false;
    }


    [Server]
    public override bool PerformAttack2()
    {
        Debug.Log("PERFORM SPEAR 2");
        if (!inHand)
        {
            return false;
        }

        aiming = true;
        wielder.TargetEnterAimMode(true);
        wielder.RpcSetAnimatorBool("aimingSpear", true);
        return true;
    }


    [Server]
    public override bool ReleaseAttack2()
    {
        if (!inHand)
        {
            return false;
        }

        wielder.RpcSetAnimatorBool("aimingSpear", false);
        wielder.TargetExitAimMode();

        aiming = false;

        return true;
    }


    public virtual bool CanThrowWeapon()
    {
        return aiming;
    }

    [Server]
    public virtual void ThrowWeapon()
    {
        wielder.RpcSetAnimatorBool("aimingSpear", false);
        wielder.TargetExitAimMode();

        wielder.PlayAnimation("SpearThrow", 0.05F);

        aiming = false;
        inHand = false;
        inFlight = true;

        weaponThrowStart = Time.time;

        StartCoroutine(nameof(ThrowWeaponCoroutine)); // Invoke throw soon, to give animation some time
    }

    
    IEnumerator ThrowWeaponCoroutine()
    {
        yield return new WaitForSeconds(0.25F);
        Vector3 aimPos = wielder.GetAimedPosition();

        Vector3 throwDirection = (aimPos - transform.position).normalized;

        Projectile weaponProjectile = Projectile.Create(
            AetherdaleData.GetAetherdaleData().thrownWeaponHolderProjectile, 
            transform.position,
            Quaternion.Euler(transform.rotation.eulerAngles - new Vector3(90, 0, 0)),
            gameObject,
            throwDirection * WEAPON_THROW_VELOCITY
        );

        // Disable projectile implicit collider
        weaponProjectile.GetComponent<Collider>().enabled = false;
        weaponProjectile.SetDamage(wielder.GetAttackDamage() * 2);
        weaponProjectile.SetImpact(200);
        weaponProjectile.progenitor = wielder.gameObject;


        // Then use the weapon's hitbox and relay hit events to the projectile
        Hitbox weaponHitbox = GetComponentInChildren<Hitbox>(includeInactive:true);
        if (weaponHitbox != null)
        {
            weaponHitbox.CollisionReroute += weaponProjectile.OnTriggerEnter;
            weaponHitbox.StartHit(0, Element.Physical, hitType:HitType.Attack, wielder.gameObject.GetComponent<Entity>(),  impact:200);
        }

        transform.SetParent(weaponProjectile.transform);
        transform.localRotation = Quaternion.Euler(90, 0, 0); // because weapons are stored pointy side up as prefabs

        yield return null; // frame for projectile to get spawned first

        RpcParentWeaponToProjectile(weaponProjectile);

        weaponProjectile.OnFlightEnd += EndWeaponThrownFlight;
    }

    [Server]
    void PickupWeapon()
    {
        Projectile weaponProjectile = GetComponentInParent<Projectile>(true);
        
        transform.SetParent(wielder.weaponTransform);
        transform.SetLocalPositionAndRotation(new(), new());

        Hitbox hitbox = GetComponentInChildren<Hitbox>(true);
        if (hitbox != null)
        {
            hitbox.CollisionReroute = null;
            hitbox.EndHit(); 
        }

        RpcParentWeaponToWeaponTransform();
        inHand = true;
        
        NetworkServer.UnSpawn(weaponProjectile.gameObject);
        Destroy(weaponProjectile.gameObject);
    }

    [Server]
    void EndWeaponThrownFlight(Projectile projectile)
    {
        inFlight = false;
    }

    [ClientRpc]
    void RpcParentWeaponToProjectile(Projectile parentProjectile)
    {
        transform.SetParent(parentProjectile.transform);
        transform.localPosition = new();
        transform.localRotation = Quaternion.Euler(90, 0, 0); // because weapons are stored pointy side up as prefabs
    }

    [ClientRpc]
    void RpcParentWeaponToWeaponTransform()
    {
        transform.SetParent(wielder.weaponTransform);
        transform.localPosition = new();
        transform.localRotation = new();
    }
    public override float GetAimAssistMaxDistance()
    {
        return ControlledEntity.MELEE_AIM_ASSIST_MAX_DEFAULT * 1.25F;
    }

    public override void PerformJumpAttack1()
    {
    }
}
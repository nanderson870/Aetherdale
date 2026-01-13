using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

public class Grublik : StatefulCombatEntity, IWeaponBehaviourWielder
{
    const float SLINGSHOT_ATTACK_RANGE = 18.0F;
    [SerializeField][FormerlySerializedAs("weaponNode")] GameObject rightWeaponNode;
    [SerializeField] GameObject leftWeaponNode;

    [SerializeField] WeaponData weapon;

    [SerializeField] List<WeaponData> potentialWeapons = new();

    [SerializeField] Projectile slingshotProjectile;

    [SyncVar(hook = nameof(WeaponBehaviourChanged))] public WeaponBehaviour weaponBehaviour;
    public Hitbox attack1Hitbox;
    public int attack1Damage;

    public Transform weaponTransform => rightWeaponNode.transform;

    public bool sprinting => false;

    RuntimeAnimatorController defaultAnimatorController;

    float defaultAttackRange = 0;


    public override void Start()
    {
        base.Start();

        defaultAnimatorController = animator.runtimeAnimatorController;

        defaultAttackRange = maxAttackRange;

        if (isServer)
        {
            WeaponData weaponData = potentialWeapons[Random.Range(0, potentialWeapons.Count)];

            WeaponBehaviour unspawned = Instantiate(weaponData.GetMesh()).GetComponent<WeaponBehaviour>();
            NetworkServer.Spawn(unspawned.gameObject);

            weaponBehaviour = unspawned;
            weaponBehaviour.SetWielder(this);
        }
    }

    void WeaponBehaviourChanged(WeaponBehaviour oldWeapBehaviour, WeaponBehaviour newWeaponBehaviour)
    {
        if (newWeaponBehaviour != null)
        {

            if (newWeaponBehaviour is CrossbowBehaviour crossbowBehaviour)
            {
                newWeaponBehaviour.transform.SetParent(leftWeaponNode.transform);
                animator.runtimeAnimatorController = crossbowBehaviour.controller;
                maxAttackRange = SLINGSHOT_ATTACK_RANGE;
            }
            else
            {
                newWeaponBehaviour.transform.SetParent(rightWeaponNode.transform);
                animator.runtimeAnimatorController = defaultAnimatorController;
                maxAttackRange = defaultAttackRange;
            }

            newWeaponBehaviour.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), weaponBehaviour.transform.rotation);
        }
        
        StartCoroutine(RefreshDefaultMatsColors());

    }

    [Server]
    public override void Attack(Entity target = null)
    {
        lastAttack = Time.time;

        if (weaponBehaviour is CrossbowBehaviour crossbowBehaviour && target != null)
        {
            crossbowBehaviour.GiveInput(AttackInputType.Attack1);
            currentTarget = target;
        }
        else
        {
            base.Attack(target);
        }
    }

    public void FireRangedWeapon()
    {
        if (isServer && currentTarget != null)
        {
            if (weaponBehaviour is CrossbowBehaviour crossbowBehaviour && currentTarget != null)
            {
                crossbowBehaviour.FireWithPrediction(currentTarget, 0.25F);
            }
        }
        currentTarget = null;
    }

    public void HitboxEvent()
    {
        if (isServer)
        {
            attack1Hitbox.HitOnce(GetAttackDamage(), Element.Physical, this);
        }
    }

    public int GetAttackDamage()
    {
        return weaponBehaviour.GetDamage();
    }

    public Vector3 GetAimedPosition()
    {
        return GetWorldPosCenter();
    }

    public void EquipWeapon(WeaponData weapon, bool dropPrevious = false)
    {
        throw new System.NotImplementedException();
    }

    public void EquipWeaponBehaviour(WeaponBehaviour weaponBehaviour, bool dropPrevious)
    {
        throw new System.NotImplementedException();
    }

    public void StartWeaponHit(int damage, Element damageType, HitType hitType, int impact)
    {
        attack1Hitbox.StartHit(damage, damageType, hitType, this, impact);
    }

    public void EndWeaponHit()
    {
        attack1Hitbox.EndHit();
    }

    public WeaponData GetEquippedWeaponData()
    {
        return weapon;
    }
}

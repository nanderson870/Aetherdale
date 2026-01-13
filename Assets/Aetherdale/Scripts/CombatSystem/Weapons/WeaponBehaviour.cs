using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Mirror;
using UnityEngine;


public abstract class WeaponBehaviour : NetworkBehaviour
{
    public static readonly LinearEquation SHOP_COST = new LinearEquation(20, 30.0F);

    public const float INPUT_HISTORY_REFRESH = 20.0F;



    public const float IDOL_STRIKE_SNAP_RANGE = 20.0F;
    public const float IDOL_STRIKE_SNAP_ANGLE = 25.0F;

    // Config
    public WeaponData weaponData;
    [SerializeField] protected EventReference equipSound;
    public EventReference attackSound;
    public string[] attackAnimationNames;
    public float attackBlendDuration = 0.03F;
    public RuntimeAnimatorController controller;
    public Color energyColor1 = new Color(0.8F, 0.8F, 0.8F, 1);
    public Color energyColor2 = Color.black;

    // Runtime
    protected IWeaponBehaviourWielder wielder;
    // public Weapon weaponItem;
    public Animator animator;

    protected float lastAttack = -10.0F;
    protected int nextAttackIndex = 0;
    protected bool inHand = true;
    protected bool inFlight = false;
    protected float weaponThrowStart = 0;

    Rarity rarityOverride;


    public virtual void Start()
    {
        animator = GetComponent<Animator>();

        if (attackAnimationNames.Length == 0)
        {
            attackAnimationNames = new string[3];
            attackAnimationNames[0] = "Attack1";
            attackAnimationNames[1] = "Attack2";
            attackAnimationNames[2] = "Attack3";
        }
    }

    public virtual void Update()
    {
        // for (int i = currentCombo.Count - 1; i >= 0; i--)
        // {
        //     if ((Time.time - currentCombo[i].Item1) > INPUT_HISTORY_REFRESH)
        //     {
        //         currentCombo.RemoveAt(i);
        //     }
        // }
    }


    [Server]
    public void SetWielder(IWeaponBehaviourWielder wielder)
    {
        this.wielder = wielder;

        RpcSetWielder(wielder.gameObject.GetComponent<NetworkBehaviour>());
    }

    [ClientRpc]
    public void RpcSetWielder(NetworkBehaviour wielderBehaviour)
    {
        this.wielder = (IWeaponBehaviourWielder)wielderBehaviour.GetComponent<IWeaponBehaviourWielder>();
    }
    public EventReference GetAttackSound()
    {
        if (attackSound.IsNull)
        {
            // Try to default to weapon type noise
            switch (weaponData.GetWeaponType())
            {
                case WeaponType.Sword1H:
                    return AetherdaleData.GetAetherdaleData().soundData.defaultAttackSound1HSword;
                case WeaponType.Sword2H:
                    return AetherdaleData.GetAetherdaleData().soundData.defaultAttackSound2HSword;
                case WeaponType.Spear:
                    return AetherdaleData.GetAetherdaleData().soundData.defaultAttackSoundSpear;
                case WeaponType.Crossbow:
                    return AetherdaleData.GetAetherdaleData().soundData.defaultAttackSoundCrossbow;
                default:
                    break;
            }
        }

        return attackSound;
    }

    public abstract EventReference GetEquipSound();

    public void PlayAttackSound()
    {
        AudioManager.Singleton.PlayOneShot(GetAttackSound(), transform.position);
    }

    public virtual bool CanAttack1()
    {
        if (attackAnimationNames.Length == 0)
        {
            Debug.LogWarning("No attack animations available");
            return false;
        }

        // Check if we're currently timed out of making another attack
        float timeSinceLastAttack = Time.time - lastAttack;
        if (timeSinceLastAttack < GetAttackInterval() * 0.85F)
        {
            return false;
        }

        return true;
    }

    public virtual bool CanAttack2() { return false; }

    public virtual bool CanJumpAttack() { return false;  }

    public virtual bool CanDodgeAttack() { return false; }

    public virtual void GiveInput(AttackInputType inputType)
    {
        // Entity wielderEntity = wielder.gameObject.GetComponent<Entity>();
        // if (inputType == AttackInputType.Attack3 && wielderEntity is PlayerWraith wraith)
        // {
        //     IdolStrike(wraith);
        // }


        //currentCombo.Add(new(Time.time, inputType));
        if (inputType == AttackInputType.Attack1 && CanAttack1())
        {
            PerformAttack1();
        }
        else if (inputType == AttackInputType.Attack2 && CanAttack2())
        {
            PerformAttack2();
        }
        else if (inputType == AttackInputType.JumpAttack1 && CanAttack1())
        {
            PerformJumpAttack1();
        }
    }

    public virtual Vector3 GetCameraOffset()
    {
        return Vector3.zero;
    }


    void IdolStrike(PlayerWraith wraith)
    {
        Debug.Log("IDOL STRIKE");
        IdolForm idolForm = wraith.GetOwningPlayer().GetIdolForm();
        if (idolForm == null)
        {
            Debug.Log("No idol");
            return;
        }

        Entity targetedEntity = wraith.GetNearestEnemy(IDOL_STRIKE_SNAP_RANGE, IDOL_STRIKE_SNAP_ANGLE, IDOL_STRIKE_SNAP_ANGLE * 2, relativeToLookingDirection: true);
        idolForm.RpcSetActive(true);

        Vector3 offset = (wraith.transform.position - targetedEntity.transform.position).normalized * 2.0F;
        Vector3 position = targetedEntity.transform.position + offset;

        var lookDirection = Quaternion.LookRotation(targetedEntity.transform.position - position);

        idolForm.TargetSetPositionAndRotation(position, lookDirection);

        StartCoroutine(IdolStrikeCoroutine(idolForm));
    }

    IEnumerator IdolStrikeCoroutine(IdolForm idolForm)
    {
        yield return null;

        idolForm.Attack1();

        idolForm.RpcSetActive(false);
    }



    // TODO derive
    public virtual bool PerformAttack1()
    {
        float timeSinceLastAttack = Time.time - lastAttack;

        lastAttack = Time.time;

        // Check if attack sequence should reset
        if (timeSinceLastAttack > GetAttackInterval() * 1.5F)
        {
            nextAttackIndex = 0;
        }


        // float interval = weaponItem.GetAttackInterval() / wielder.GetStat(Stats.AttackSpeed);
        // foreach (AnimationClip clip in controller.animationClips)
        // {
        //     if (clip.name.Replace(" ", "").Contains(attackAnimationNames[currentAttackIndex]))
        //     {
        //         interval *= clip.length;
        //         break;
        //     }
        // }


        wielder.PlayAnimation(attackAnimationNames[nextAttackIndex], 0.05F);

        wielder.gameObject.GetComponent<Entity>().SetAttacking(true);

        nextAttackIndex++;

        // Check if attack sequence should reset
        if (nextAttackIndex >= attackAnimationNames.Length)
        {
            nextAttackIndex = 0;
        }

        return true;
    }


    [Server]
    public virtual bool ReleaseAttack1() { return true; }



    // TODO mark abstract and split into derived classes
    [Server]
    public virtual bool PerformAttack2()
    {
        if (GetWeaponType() == WeaponType.Sword1H)
        {
            wielder.gameObject.GetComponent<PlayerWraith>().blocking = true;
            return true;
        }

        return false;
    }

    // TODO mark abstract and split into derived classes
    [Server]
    public virtual bool ReleaseAttack2()
    {
        if (GetWeaponType() == WeaponType.Sword1H)
        {
            wielder.gameObject.GetComponent<PlayerWraith>().blocking = false;
            return true;
        }

        return false;
    }

    
    // TODO mark abstract and split into derived classes
    [Server]
    public virtual bool ReleaseAttack3()
    {
        return false;
    }

    public abstract void PerformJumpAttack1();

    public Entity GetWielderAsEntity()
    {
        return wielder.gameObject.GetComponent<Entity>();
    }

    public virtual float GetAimAssistMaxDistance()
    {
        return ControlledEntity.MELEE_AIM_ASSIST_MAX_DEFAULT;
    }

    #region DATA
    //[Obsolete("Add out parameter for additional damage types")]
    public int GetDamage()
    {
        return weaponData.GetDamage(rarityOverride);
    }

    public int GetDamage(out Dictionary<Element, int> secondaryDamage)
    {
        return weaponData.GetDamage(rarityOverride, out secondaryDamage);
    }

    public Element GetDamageType()
    {
        return weaponData.GetDamageType();
    }

    public float GetAttackInterval()
    {
        return 1 / weaponData.GetAttackSpeed();
    }

    public WeaponType GetWeaponType()
    {
        return weaponData.GetWeaponType();
    }

    public int GetImpact()
    {
        return weaponData.GetImpact();
    }

    public int GetUpgradeCost()
    {
        return weaponData.CalculateUpgradeAetherCost(rarityOverride);
    }

    public Rarity GetRarity()
    {
        return rarityOverride;
    }

    #endregion

}


public enum AttackInputType
{
    None = -1,
    Attack1 = 0, // M1
    Attack2 = 1, // M2
    Attack3 = 2, // Middle Mouse
    JumpAttack1=3
}

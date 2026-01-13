using System.Collections;
using UnityEngine;
using Mirror;
using FMODUnity;
using UnityEngine.VFX;
using UnityEngine.Animations.Rigging;

public class PlayerWraith : ControlledEntity, IWeaponBehaviourWielder
{

    const float BLOCKED_DAMAGE_MULT = 0.5F;
    const float BLOCK_HEADING_ANGLE = 60.0F;

    [SerializeField] VisualEffect[] oneHandSwordSlashEffects;
    [SerializeField] VisualEffect[] twoHandSwordSlashEffects;
    public Transform weaponTransform;
    [SerializeField] Transform weaponHitboxParent;


    [Header("Wraith Rigging")]
    [SerializeField] Rig crossbowAimRig;

    [SerializeField] EventReference attackSound;
    [SerializeField] EventReference crossbowAttackSound;

    public RuntimeAnimatorController defaultAnimatorController;



    [SyncVar(hook = nameof(WeaponInHandStatusChanged))] bool weaponInHand = true;

    [SyncVar(hook = nameof(WeaponBehaviourChanged))] public WeaponBehaviour heldWeaponBehaviour;

    public bool blocking = false;

    GameObject heldObject;
    Consumable consumableInUse = null;


    Transform IWeaponBehaviourWielder.weaponTransform { get => weaponTransform; }

    bool IWeaponBehaviourWielder.sprinting => sprinting;


    public GameObject hitSplatPrefab;

    public override void Start()
    {
        base.Start();

        defaultAnimatorController = GetComponent<Animator>().runtimeAnimatorController;

        crossbowAimRig.weight = 0;

        if (heldWeaponBehaviour != null)
        {
            heldWeaponBehaviour.transform.parent = weaponTransform;
            heldWeaponBehaviour.transform.localPosition = Vector3.zero;
            heldWeaponBehaviour.transform.localRotation = Quaternion.identity;
        }
    }

    public override void Update()
    {
        base.Update();

        if (consumableInUse != null)
        {
            consumableInUse.Update(this);
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        for (int i = 0; i < oneHandSwordSlashEffects.Length; i++)
        {
            oneHandSwordSlashEffects[i].enabled = false;
            oneHandSwordSlashEffects[i].enabled = true;
            oneHandSwordSlashEffects[i].Stop();
            oneHandSwordSlashEffects[i].Reinit();
        }


        for (int i = 0; i < twoHandSwordSlashEffects.Length; i++)
        {
            twoHandSwordSlashEffects[i].enabled = false;
            twoHandSwordSlashEffects[i].enabled = true;
            twoHandSwordSlashEffects[i].Stop();
            twoHandSwordSlashEffects[i].Reinit();
        }
    }

    public override void ProcessInput()
    {
        base.ProcessInput();

        PlayerInputData inputData = PlayerInput.Input;

        if (inputData == null)
        {
            return;
        }

        if (inputData.offensiveConsumable) { CmdOffensiveConsumable(); Debug.Log("using offensive consumable"); }
        if (inputData.offensiveConsumableReleased) CmdOffensiveConsumableReleased();

        if (inputData.defensiveConsumable) CmdDefensiveConsumable();
        if (inputData.defensiveConsumableReleased) CmdDefensiveConsumableReleased();

        if (inputData.utilityConsumable) CmdUtilityConsumable();
        if (inputData.utilityConsumableReleased) CmdUtilityConsumableReleased();
    }


    // SyncVar hook
    public override void OwningPlayerChanged(Player oldPlayer, Player newPlayer)
    {
        base.OwningPlayerChanged(oldPlayer, newPlayer);

        if (newPlayer == null || !isOwned)
        {
            return;
        }

        StartCoroutine(SetupWraithFormForPlayerClient(newPlayer));
    }

    IEnumerator SetupWraithFormForPlayerClient(Player newOwningPlayer)
    {
        while (newOwningPlayer.GetPlayerData() == null || newOwningPlayer.GetCamera() == null)
        {
            yield return null;
        }

        if (newOwningPlayer.GetSequenceWeapon() != null)
        {
            EquipWeapon(newOwningPlayer.GetSequenceWeapon());
        }
        else if (newOwningPlayer.GetPlayerData().EquippedWeapon is WeaponData equippedWeapon)
        {
            EquipWeapon(equippedWeapon);
        }
    }

    protected override void Animate()
    {
        base.Animate();

        if (isDead)
        {
            return;
        }

        Vector3 commandedVelocity = inputVelocity * GetStat(Stats.MovementSpeed);
        if (commandedVelocity.magnitude > 0.5F)
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 1.0F);
        }
        else
        {
            animator.SetLayerWeight(animator.GetLayerIndex("Lower Body Movement Layer"), 0.0F);
        }

        animator.SetBool("blocking", blocking);

        if (heldWeaponBehaviour != null)
        {
            animator.SetFloat("attackSpeed", heldWeaponBehaviour.weaponData.GetAttackSpeed() * GetStat(Stats.AttackSpeed, 1.0F));
        }
    }


    public override float GetAttackingMovespeedMult()
    {
        if (heldWeaponBehaviour != null)
        {
            return heldWeaponBehaviour.weaponData.GetAttackMovespeedMult();
        }
        else
        {
            Debug.Log(1.0F);
            return 1.0F;
        }
    }


    [Command]
    void CmdOffensiveConsumable()
    {
        consumableInUse = owningPlayer.GetInventory().OffensiveConsumableSlot.GetConsumable();

        if (consumableInUse != null)
        {
            consumableInUse.Use(this);
            consumableInUse.OnConsumed += () =>
            {
                consumableInUse = null;
            };
        }
    }

    [Command]
    void CmdOffensiveConsumableReleased()
    {
        if (consumableInUse != null)
        {
            consumableInUse.Release(this);

            consumableInUse = null;
        }
    }


    [Command]
    void CmdDefensiveConsumable()
    {
        consumableInUse = owningPlayer.GetInventory().DefensiveConsumableSlot.GetConsumable();

        if (consumableInUse != null)
        {
            consumableInUse.Use(this);
            consumableInUse.OnConsumed += () =>
            {
                consumableInUse = null;
            };
        }
    }

    [Command]
    void CmdDefensiveConsumableReleased()
    {
        if (consumableInUse != null)
        {
            consumableInUse.Release(this);

            consumableInUse = null;
        }
    }

    [Command]
    void CmdUtilityConsumable()
    {
        consumableInUse = owningPlayer.GetInventory().UtilityConsumableSlot.GetConsumable();

        if (consumableInUse != null)
        {
            consumableInUse.Use(this);
            consumableInUse.OnConsumed += () =>
            {
                consumableInUse = null;
            };
        }
    }

    [Command]
    void CmdUtilityConsumableReleased()
    {
        if (consumableInUse != null)
        {
            consumableInUse.Release(this);

            consumableInUse = null;
        }
    }


    [Client]
    public void EquipWeapon(WeaponData weaponData, bool dropPrevious = false)
    {
        // Debug.Log("Set weapon to " + weapon);
        CmdUpdateEquippedWeapon(weaponData != null ? weaponData.GetItemID() : "", dropPrevious);
    }

    [Command]
    void CmdUpdateEquippedWeapon(string weaponID, bool dropPrevious)
    {
        WeaponBehaviour newWeaponBehaviour = null;
        if (weaponID != "")
        {
            WeaponData newWeaponData = (WeaponData)ItemManager.LookupItemData(weaponID);
            newWeaponBehaviour = Instantiate(newWeaponData.GetMesh()).GetComponent<WeaponBehaviour>();
            newWeaponBehaviour.transform.SetLocalPositionAndRotation(Vector3.zero, newWeaponData.GetMesh().transform.rotation);

            NetworkServer.Spawn(newWeaponBehaviour.gameObject);
        }

        EquipWeaponBehaviour(newWeaponBehaviour, dropPrevious);
    }


    [Server]
    public void EquipWeaponBehaviour(WeaponBehaviour weaponBehaviour, bool dropPrevious)
    {
        if (heldWeaponBehaviour != null && dropPrevious)
        {
            heldWeaponBehaviour.weaponData.CreatePickup(GetWorldPosCenter(), heldWeaponBehaviour.transform.rotation);
        }

        foreach (Transform child in weaponTransform)
        {
            NetworkServer.UnSpawn(child.gameObject);
            Destroy(child.gameObject);
        }

        TargetExitAimMode();

        weaponBehaviour.SetWielder(this);
        heldWeaponBehaviour = weaponBehaviour;

        ModifyWeaponCollider(weaponBehaviour);
    }


    protected virtual void ModifyWeaponCollider(WeaponBehaviour weaponBehaviour)
    {
        GameObject newHitboxObject = new($"Weapon Collider - {weaponBehaviour.weaponData.GetName()}");
        newHitboxObject.transform.parent = weaponHitboxParent;
        newHitboxObject.transform.SetLocalPositionAndRotation(new(), Quaternion.identity);
        newHitboxObject.layer = LayerMask.NameToLayer("Hitboxes");

        Hitbox hitbox = null;
        if (weaponBehaviour.weaponData.GetWeaponType() != WeaponType.Crossbow)
        {
            if (weaponBehaviour.weaponData.GetWeaponType() == WeaponType.Spear)
            {
                BoxCollider boxCollider = newHitboxObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;

                float range = weaponBehaviour.weaponData.GetRange();

                boxCollider.size = new(2, 2, range);

                boxCollider.transform.localPosition = new Vector3(
                    0,
                    0,
                    range * 0.5F
                );
            }
            else
            {
                SphereCollider sphereCollider = newHitboxObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;

                float radius = weaponBehaviour.weaponData.GetRange() / 2;

                sphereCollider.radius = radius;

                sphereCollider.transform.localPosition = new Vector3(
                    0,
                    0,
                    radius
                    );
            }

            hitbox = newHitboxObject.AddComponent<Hitbox>();

        }


        SetAttackHitbox(hitbox);
    }


    void WeaponBehaviourChanged(WeaponBehaviour oldWeapon, WeaponBehaviour newWeapon)
    {
        cameraContext.ClearOffset();
        ExitAimMode();
        
        // GetCameraContext().ClearOffset();
        // if (oldWeapon != null)
        // {
        //     Debug.Log("Remove offset from " + oldWeapon);
        //     GetCameraContext().RemoveOffset(oldWeapon.GetCameraOffset());
        // }

        if (newWeapon != null)
        {
            newWeapon.transform.SetParent(weaponTransform);
            newWeapon.transform.localPosition = Vector3.zero;
            newWeapon.transform.localRotation = newWeapon.weaponData.GetMesh().transform.rotation;
            
            GetComponent<Animator>().runtimeAnimatorController = newWeapon.controller;
            Debug.Log("Add offset from " + oldWeapon);
            GetCameraContext().AddOffset(newWeapon.GetCameraOffset());

            Debug.Log("WEAPON SOUND");
            AudioManager.Singleton.PlayOneShot(newWeapon.GetEquipSound(), transform.position);
        }
        else
        {
            GetComponent<Animator>().runtimeAnimatorController = defaultAnimatorController;
        }


        gameObject.BroadcastMessage("MeshesChanged", SendMessageOptions.DontRequireReceiver);

        StartCoroutine(RefreshDefaultMatsColors());
    }


    void WeaponInHandStatusChanged(bool weaponInHandPreviously, bool weaponInHandNow)
    {
        //heldWeapon.SetActive(weaponInHandNow);
    }

    [ClientRpc]
    public void RpcHoldBlazeBomb()
    {
        if (heldObject != null)
        {
            Destroy(heldObject);
        }

        heldObject = Instantiate(AetherdaleData.GetAetherdaleData().blazeBombHeldPrefab, weaponTransform);
    }

    [ClientRpc]
    public void RpcHoldBrindleberryMuffin()
    {
        if (heldObject != null)
        {
            Destroy(heldObject);
        }

        heldObject = Instantiate(AetherdaleData.GetAetherdaleData().brindeberryMuffinHeldPrefab, weaponTransform);
    }


    [ClientRpc]
    public void RpcRemoveHeldObject()
    {
        if (heldObject == null)
        {
            return;
        }

        Destroy(heldObject);
        heldObject = null;
    }

    [ClientRpc]
    public void RpcShowHeldWeapon()
    {
        if (heldWeaponBehaviour != null)
        {
            heldWeaponBehaviour.gameObject.SetActive(true);
        }
    }

    [ClientRpc]
    public void RpcHideHeldWeapon()
    {
        if (heldWeaponBehaviour != null)
        {
            heldWeaponBehaviour.gameObject.SetActive(false);
        }
    }

    public override float ModifyDamageFromTarget(Entity target, float damage)
    {
        if (blocking && Mathf.Abs(GetRelativeBearingAngle(target.gameObject)) <= BLOCK_HEADING_ANGLE)
        {
            float newDamage = damage * BLOCKED_DAMAGE_MULT;
            if (newDamage < 1)
            {
                newDamage = 1;
            }

            return newDamage;
        }
        else
        {
            return base.ModifyDamageFromTarget(target, damage);
        }
    }

    [Server]
    public override void Attack1()
    {
        if (heldWeaponBehaviour == null || !weaponInHand)
        {
            return;
        }

        heldWeaponBehaviour.GiveInput(AttackInputType.Attack1);
        if (aimedEnemy != null)
        {
            transform.LookAt(aimedEnemy.transform);
        }

        RpcSetAttackMode();
        lastAttack = Time.time;
    }

    [Command]
    public override void CmdReleaseAttack1()
    {
        if (heldWeaponBehaviour == null || !weaponInHand)
        {
            return;
        }

        heldWeaponBehaviour.ReleaseAttack1();

        RpcSetAttackMode();
    }

    [Server]
    public override void Attack2()
    {
        if (!weaponInHand || heldWeaponBehaviour == null)
        {
            return;
        }

        heldWeaponBehaviour.GiveInput(AttackInputType.Attack2);

        RpcSetAttackMode();
    }

    [Command]
    public override void CmdReleaseAttack2()
    {
        if (!weaponInHand || heldWeaponBehaviour == null)
        {
            return;
        }

        heldWeaponBehaviour.ReleaseAttack2();
    }

    [Server]
    public override void Attack3()
    {
        if (!weaponInHand || heldWeaponBehaviour == null)
        {
            return;
        }

        heldWeaponBehaviour.GiveInput(AttackInputType.Attack3);

        RpcSetAttackMode();
    }

    [Command]
    public override void CmdReleaseAttack3()
    {
        if (!weaponInHand || heldWeaponBehaviour == null)
        {
            return;
        }

        heldWeaponBehaviour.ReleaseAttack3();
    }

    [Command]
    public override void CmdJumpAttack1()
    {
        if (!weaponInHand || heldWeaponBehaviour == null)
        {
            return;
        }

        if (!CanAttack())
        {
            return;
        }

        heldWeaponBehaviour.GiveInput(AttackInputType.JumpAttack1);
    }

    public override void ActivateRig(string name)
    {
        if (name == "crossbowAimRig")
        {
            StartCoroutine(SetRigActive(crossbowAimRig, true));
        }
    }

    public override void DeactivateRig(string name)
    {
        if (name == "crossbowAimRig")
        {
            StartCoroutine(SetRigActive(crossbowAimRig, false));
        }
    }

    [Client]
    protected override void Transform()
    {
        if (owningPlayer.GetPlayerData().UnlockData.idolsUnlocked)
        {
            CmdTransform();
        }
    }

    [Command]
    protected override void CmdTransform()
    {
        base.CmdTransform();
        GetOwningPlayer().TransformIntoIdol();
    }

    [ServerCallback]
    public void FireRangedWeapon()
    {
        if (heldWeaponBehaviour is CrossbowBehaviour crossbowBehaviour)
        {
            crossbowBehaviour.Fire();
        }
    }

    public void SetAttackHitbox(Hitbox hitbox)
    {
        if (attackHitbox != null)
        {
            Destroy(attackHitbox.gameObject);
        }

        attackHitbox = hitbox;

    }

    public void AttackHit()
    {
        switch (heldWeaponBehaviour.GetWeaponType())
        {
            case WeaponType.Crossbow:
                // Does not use this event
                break;

            default:

                if (isServer)
                {
                    attackHitbox.HitOnce(GetAttackDamage(), heldWeaponBehaviour.GetDamageType(), this, hitType: HitType.Attack, impact: heldWeaponBehaviour.GetImpact(), onHitAction: RpcMeleeHitEnemy);
                    attacking = false;
                }

                heldWeaponBehaviour.PlayAttackSound();

                if (isOwned)
                {
                    PlayerCamera.ApplyLocalScreenShake(heldWeaponBehaviour.weaponData.GetImpact() / 125.0F, .1F, transform.position, frequency: 1F);
                }
                break;

        }
    }

    public override bool CanJumpAttack()
    {
        return heldWeaponBehaviour!= null && heldWeaponBehaviour.CanJumpAttack();
    }

    public override bool CanDodgeAttack()
    {
        return base.CanDodgeAttack() && heldWeaponBehaviour.CanDodgeAttack();
    }

    [ClientRpc]
    void RpcMeleeHitEnemy(HitboxHitData data)
    {
        attacking = false;
        // Debug.Log(data.velocity);
        StartCoroutine(CreateHitSplatCoroutine(data));
    }

    protected override void OnGrounded()
    {
        base.OnGrounded();
    }

    public override int GetAttackDamage()
    {
        return heldWeaponBehaviour.GetDamage();
    }

    void PlayMeleeSlashVFX(VisualEffect vfx)
    {
        vfx.SetVector4("Main Color", heldWeaponBehaviour.energyColor1);
        vfx.SetVector4("Secondary Color", heldWeaponBehaviour.energyColor2);
        vfx.SendEvent("Play");
    }

    public void PlaySword1HSlash1()
    {
        PlayMeleeSlashVFX(oneHandSwordSlashEffects[0]);
    }

    public void PlaySword1HSlash2()
    {
        PlayMeleeSlashVFX(oneHandSwordSlashEffects[1]);
    }

    public void PlaySword2HSlash1()
    {
        PlayMeleeSlashVFX(twoHandSwordSlashEffects[0]);
    }
    public void PlaySword2HSlash2()
    {
        PlayMeleeSlashVFX(twoHandSwordSlashEffects[1]);
    }
    public void PlaySword2HSlash3()
    {
        PlayMeleeSlashVFX(twoHandSwordSlashEffects[2]);
    }



    IEnumerator CreateHitSplatCoroutine(HitboxHitData data)
    {
        float totalTime = 0.25F;
        float timeRemaining = totalTime;

        GameObject hitsplatInst = Instantiate(hitSplatPrefab, Player.GetLocalPlayer().GetUI().transform);

        hitsplatInst.transform.position = data.position;

        while (timeRemaining > 0)
        {
            float elapsed = totalTime - timeRemaining;

            timeRemaining -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        Destroy(hitsplatInst.gameObject);
    }

    public override void Die()
    {
        if (HasEffectOfType(typeof(ResurrectionEffect)))
        {
            // Limited subset of original death function
            isDead = true;
            resurrecting = true;
            SetAnimatorTrigger("Die");

            for (int i = activeEffectInstances.Count - 1; i >= 0; i--)
            {
                if (activeEffectInstances[i] != null)
                {
                    activeEffectInstances[i].TargetEntityDeath();
                }
            }
        }
        else
        {
            base.Die();
        }
    }

    public override void DeathComplete()
    {
        Debug.Log("WRAITH DC");
        if (!resurrecting)
        {
            Debug.Log("GO TO BASE");
            base.DeathComplete();
        }
    }

    public override void CreateCorpse(Entity entity)
    {
        base.CreateCorpse(entity);

        Debug.Log("CREATE CORPSE");

        if (isServer)
        {
            // Create a revivable and place it where we died, spawn it
            Revivable revivable = Instantiate(AetherdaleData.GetAetherdaleData().revivablePrefab); //new GameObject("Revivable Wraith").AddComponent<Revivable>(); //
            revivable.owningPlayer = this.GetOwningPlayer();
            revivable.transform.position = corpseObject.transform.position;
            NetworkServer.Spawn(revivable.gameObject);

            // Server only (not spawned) - add held interaction prompt
            const float REVIVE_TIME = 5.0F;
            HeldInteractionPrompt prompt = revivable.gameObject.AddComponent<HeldInteractionPrompt>();
            prompt.requiredHoldTime = REVIVE_TIME; 
        }
        
        GetOwningPlayer().OnNextRevive += corpseObject.GetComponent<Corpse>().OnOwnerRevived;
    }

    public override float GetAimAssistMaxDistance()
    {
        if (heldWeaponBehaviour == null)
        {
            return MELEE_AIM_ASSIST_MAX_DEFAULT;
        }

        return heldWeaponBehaviour.GetAimAssistMaxDistance();
    }

    public override float GetHealthOrbsOnKillMult()
    {
        if (heldWeaponBehaviour is MeleeWeaponBehaviour)
        {
            return MELEE_HEALTH_ORBS_ON_KILL_MULT;
        }

        return base.GetHealthOrbsOnKillMult();
    }

    public void StartWeaponHit(int damage, Element damageType, HitType hitType, int impact)
    {
        attackHitbox.StartHit(damage, damageType, hitType, this, impact);
    }

    public void EndWeaponHit()
    {
        attackHitbox.EndHit();
    }

    public WeaponData GetEquippedWeaponData()
    {
        if (heldWeaponBehaviour == null)
        {
            return null;
        }

        return heldWeaponBehaviour.weaponData;
    }
}

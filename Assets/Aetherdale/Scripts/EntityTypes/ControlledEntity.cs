using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using FMODUnity;
using UnityEngine.Animations.Rigging;
using System.Collections;
using UnityEngine.Serialization;
using UnityEngine.VFX;


/* Generic movement functionality for player-controlled entities */


public abstract class ControlledEntity : Entity
{

    public const float MELEE_AIM_ASSIST_MAX_DEFAULT = 20;
    public const float RANGED_AIM_ASSIST_MAX_DEFAULT = 50;
    public const float RIG_LERP_VALUE = 8.0F;
    public const float MAX_LOCK_ON_RANGE = 20.0F;
    public const float FALL_MULT = 1.75F;
    public const float AIRBORNE_HORIZONTAL_VELOCITY_LERP = 0.25F;
    public const float TRANSFORM_INVULN_SECONDS = 0.25F;



    // How much of an attack animation must play before the next in a combo begins
    protected readonly float comboInputBlockout = 0.6F;
    [Header("Transformation")]
    [SerializeField] AreaOfEffect transformationExplosion;

    [Header("Attacks")]
    [SerializeField] public Hitbox attackHitbox;
    [SerializeField] protected int attackDamage = 10;
    [SerializeField] protected string[] attackAnimationNames;
    [SerializeField] protected float attackBlendDuration = 0.03F;
    [SerializeField] float attackInputTimeout = 0.5F; // how long we have to wait between attacks
    [SerializeField] float attackSequenceExpiration = 1.25F;
    protected int currentAttackIndex = 0;
    [SerializeField] protected AreaOfEffect slashDashAOE;



    [Header("Movement")]
    [SerializeField] float baseMoveSpeed = 8.0F;
    [SerializeField] int baseNumberOfJumps = 2;
    [SerializeField] EventReference jumpSound;
    [SerializeField] float jumpMult = 8.0F;
    [SerializeField][FormerlySerializedAs("sprintSpeedMult")] float baseSprintSpeedMult = 1.5F;
    float baseDodgeSpeedMult = 2.0F;
    float dodgeDuration = 0.25F;
    float dodgeInvulnerabilityDuration = 1F; // occurs in middle of dodge
    float baseDodgeCooldown = 0.75F;
    [SerializeField] EventReference dodgeSound;
    [SerializeField] Transform upperBodyAimTransform;
    [SerializeField] Transform weaponAimTransform;
    [SerializeField] GameObject blobShadow;


    [Header("Stepfinding")]
    [SerializeField] LayerMask stairLayer;

    [Header("VFX")]
    [SerializeField] VisualEffect dashVFX; // TODO unserialize and manage at runtime

    Collider moveCollider;

    Entity lockOnTarget;


    Vector3 velocityToAdd = new();
    bool resettingFall = false;

    bool entityReady = false;

    /// <summary> Whether the entity rotation follows the camera angle </summary>
    bool rotationTrackCamera = false;

    protected bool inGUI = false;

    protected int jumpsRemaining;

    [SyncVar] protected bool sprinting = false;

    protected bool dodging = false;

    /// <summary>Whether we are prevented from from dodging right now</summary>
    protected bool dodgeBlocked = false;
    protected bool attackedThisDodge = false;
    protected float dodgeTimeRemaining = 0.0F;
    protected float dodgeCooldownRemaining = 0;
    protected Vector3 currentDodgeVelocity;
    readonly List<InteractionPrompt> currentAvailableInteractionPrompts = new();

    /// <summary>The interaction prompt that will be interacted with if interact is pressed</summary>
    InteractionPrompt selectedInteractionPrompt;
    [SyncVar(hook = nameof(OnSelectedInteractionPromptChangedClient))] SelectedInteractionPromptData selectedInteractionPromptData;

    void OnSelectedInteractionPromptChangedClient(SelectedInteractionPromptData oldData, SelectedInteractionPromptData newData)
    {
        if (!isOwned)
        {
            return;
        }

        if (oldData != null)
        {
            if (oldData.associatedObject != null)
            {
                Outliner.ClearOutline(oldData.associatedObject.gameObject);
            }
        }

        if (selectedInteractionPromptData == null)
        {
            Tooltip.Hide();
            return;
        }

        GameObject go = null;
        if (selectedInteractionPromptData.associatedObject != null)
        {
            go = selectedInteractionPromptData.associatedObject.gameObject;
        }

        TooltipData data = selectedInteractionPromptData.tooltipData;
        if (data != null && data.titleText != "" && data.descriptionText != "")
        {
            Tooltip.Show(go, data.titleText, data.descriptionText);
        }


        if (newData != null)
        {
            if (newData.associatedObject != null)
            {
                Outliner.Outline(newData.associatedObject.gameObject, Color.yellow);
            }
        }
    }

    TargetPaintMode currentTargetPaintMode;

    protected Vector3 globalVelocity;
    protected Vector3 inputVelocity;

    float rigWeight = 1.0F;
    bool cameraInFront = false; // Whether the camera is directed roughly in front of this entity


    readonly float attackModeTimeout = 1.0F;

    // TODO Push this up further into entity class
    GameObject objectAnchoredTo = null;
    Vector3 objectAnchoredToLastPos = new();

    float lastJump;

    GameObject levelupVFXObject;



    public override void OnEnable()
    {
        base.OnEnable();
        SetInvulnerableForSeconds(TRANSFORM_INVULN_SECONDS);
    }

    // Input-bound methods that can be implemented by derivations if an action is supported
    // Must be marked [Command]
    #region Attacks

    [Command]
    // Primary attack action
    protected virtual void CmdAttack1()
    {
        Attack1();

        if (aimedEnemy != null)
        {
            transform.LookAt(aimedEnemy.transform);
        }
    }

    [Command]
    protected virtual void CmdDodgeAttack1(float dodgeProgress)
    {
        DodgeAttack(dodgeProgress);
    }

    [Command]
    public virtual void CmdJumpAttack1()
    {
        // if (!CanAttack())
        // {
        //     return;
        // }


        // Debug.Log("JUMP ATTACK");

        // globalVelocity += Vector3.down * JUMP_ATTACK_DOWN_VELOCITY;

        Attack1(); // TODO

        // inJumpAttack = true;
        // SwitchMovementToRigidBodyUntilGrounded(globalVelocity);
        // OnNextGrounded += JumpAttackDone;
    }


    [Server]
    public virtual void Attack1()
    {
        if (attackAnimationNames.Length == 0)
        {
            Debug.LogWarning("No attack animations available");
        }

        // Check if we're currently timed out of making another attack
        if (!CanAttack())
        {
            return;
        }

        float timeSinceLastAttack = Time.time - lastAttack;
        // Check if attack sequence should reset
        if (currentAttackIndex >= attackAnimationNames.Length
            || timeSinceLastAttack >= attackSequenceExpiration)
        {
            currentAttackIndex = 0;
        }

        PlayAnimation(attackAnimationNames[currentAttackIndex], attackBlendDuration);
        currentAttackIndex++;

        RpcSetAttackMode();
        lastAttack = Time.time;
    }


    [Command]
    public virtual void CmdReleaseAttack1() { }

    [Command]
    public virtual void CmdAttack2()  // Secondary attack action
    {
        Attack2();
    }

    [Server]
    public virtual void Attack2()
    {

    }

    [Command]
    public virtual void CmdReleaseAttack2() { }



    [Command]
    public virtual void CmdAttack3()  // Secondary attack action
    {
        Attack3();
    }

    [Server]
    public virtual void Attack3()
    {

    }

    [Command]
    public virtual void CmdReleaseAttack3() { }

    public virtual bool CanAttack()
    {
        float timeSinceLastAttack = Time.time - lastAttack;
        return timeSinceLastAttack >= (attackInputTimeout / GetStat(Stats.AttackSpeed, 1.0F));
    }

    public virtual bool CanDodgeAttack()
    {
        float timeSinceLastDodgeAttack = Time.time - lastDodgeAttack;
        return dodging && (timeSinceLastDodgeAttack >= attackInputTimeout / GetStat(Stats.AttackSpeed, 1.0F));
    }

    public virtual bool CanJumpAttack()
    {
        return false;
    }

    public virtual int GetAttackDamage()
    {
        return attackDamage;
    }

    [Server]
    protected virtual void DodgeAttack(float dodgeProgress)
    {
        // Check if we're currently timed out of making another attack
        if (!CanDodgeAttack())
        {
            return;
        }

        PlayAnimation("DodgeAttack1", 0.05F);

        AreaOfEffect aoe = AreaOfEffect.CreateNoTelegraph(slashDashAOE, GetWorldPosCenter() - new Vector3(0, 0.5F, 0), this, HitType.Attack);
        aoe.SetDamage((int)(GetAttackDamage() * 1.5F));
        aoe.SetVelocity(globalVelocity);
        aoe.SetDuration(dodgeDuration * (1 - dodgeProgress));
        RpcPositionSlashDashAOE(aoe);

        //lastAttackModeActionTime = Time.time;
        lastDodgeAttack = Time.time;
    }
    
    [ClientRpc]
    void RpcPositionSlashDashAOE(AreaOfEffect aoe)
    {
        aoe.transform.position = GetWorldPosCenter() - new Vector3(0, 0.5F, 0);
    }

    #endregion

    public override void Stagger()
    {
        base.Stagger();

        // Instantly reset attack timer
        lastAttack = Time.time - attackInputTimeout / GetStat(Stats.AttackSpeed, 1.0F);
    }


    [Client]
    protected virtual void Transform()
    {
        body.linearVelocity = new();
        globalVelocity = new();

        CmdTransform();
    }

    [Command]
    protected virtual void CmdTransform()
    {
    } // Transform

    protected virtual void OnGrounded() { }

    [Client]
    public void SetEntityReady()
    {
        entityReady = true;
    }

    [Client]
    public bool IsEntityReady()
    {
        return entityReady;
    }

    public SelectedInteractionPromptData GetSelectedInteractionPromptData()
    {
        return selectedInteractionPromptData;
    }
    // public InteractionPrompt GetSelectedInteractionPrompt()
    // {
    //     return selectedInteractionPrompt;
    // }

    // Sets currently selected interaction prompt, if any are available
    [Server]
    void UpdateInteractionPromptSelection()
    {
        //selectedInteractionPrompt = null;
        //selectedInteractionPromptData = null;

        if (currentAvailableInteractionPrompts.Count == 0)
        {
            selectedInteractionPrompt = null;
            selectedInteractionPromptData = null;
            return;
        }

        InteractionPrompt nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (InteractionPrompt prompt in currentAvailableInteractionPrompts)
        {
            if (prompt == null || !prompt.gameObject.activeSelf)
            {
                continue;
            }

            // Check LOS - at prompt position + a little buffer upwards for the ground
            Vector3 raycastVector = prompt.transform.position + new Vector3(0, 1, 0) - transform.position;
            RaycastHit[] hits = Physics.RaycastAll(GetWorldPosCenter(), raycastVector, raycastVector.magnitude, LayerMask.GetMask("Default"));
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.root != transform.root
                    && hit.transform.root != prompt.transform.root)
                {
                    continue;
                }
            }

            bool canTalk = prompt.GetAttachedDialogueAgent() != null;
            bool hasInteraction = false;
            if (prompt.GetInteractable() != null)
            {
                hasInteraction = prompt.GetInteractable().IsInteractable(this);
            }

            if (!canTalk && !prompt.GetInteractable().IsSelectable())
            {
                continue;
            }

            float distanceToCurrent = Vector3.Distance(transform.position, prompt.transform.position);
            if (distanceToCurrent < nearestDistance)
            {
                nearest = prompt;
                nearestDistance = distanceToCurrent;
            }
        }

        //selectedInteractionPrompt = nearest;
        if (nearest == null)
        {
            selectedInteractionPrompt = null;
            selectedInteractionPromptData = null;
        }
        else if (nearest != selectedInteractionPrompt)
        {
            selectedInteractionPrompt = nearest;
            selectedInteractionPromptData = new(selectedInteractionPrompt, this);
        }
    }


    #region Unity Messages
    public override void Start()
    {
        base.Start();

        moveCollider = GetComponent<Collider>();

        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (isServer)
        {
            animator.SetFloat("attackSpeed", 1.0F);
        }

        levelupVFXObject = Instantiate(AetherdaleData.GetAetherdaleData().levelUpVFXPrefab, transform);

        movementMode = MovementMode.PlayerControlled;
        defaultMovementMode = MovementMode.PlayerControlled;


#if UNITY_EDITOR
        Invoke(nameof(DebugAddTraits), 2);
#endif
    }



    protected override void Animate()
    {
        if (isDead)
        {
            return;
        }

        animator.SetBool("airborne", airborne);

        Vector3 commandedVelocity = inputVelocity * GetStat(Stats.MovementSpeed);

        animator.SetFloat("horizontalVelocityMagnitude", inputVelocity.magnitude * GetStat(Stats.MovementSpeed));

        if (commandedVelocity.magnitude > .3F)
        {
            animator.SetFloat("xVelocity", transform.InverseTransformVector(globalVelocity).x);
            animator.SetFloat("zVelocity", transform.InverseTransformVector(globalVelocity).z);
        }
        else
        {
            animator.SetFloat("xVelocity", 0);
            animator.SetFloat("zVelocity", 0);
        }

        animator.SetBool("dodging", dodging);

        animator.SetFloat("attackSpeed", GetStat(Stats.AttackSpeed, 1.0F));
    }


    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isOwned)
        {
            ProcessAimAssistTarget();

            Vector3 externalForceVelocityLocal = CalculateExternalVelocity();

            if (movementMode == MovementMode.PlayerControlled)
            {
                Move(globalVelocity + externalForceVelocityLocal);
            }


            Vector3 horizontalVelocity = new Vector3(globalVelocity.x, 0, globalVelocity.z).normalized;

            if (CanTurn())
            {
                if (!dodging
                    && !sprinting
                    && (attacking || ((Time.time - lastAttackModeActionTime) <= attackModeTimeout && lastAttackModeActionTime > lastDodge))
                    || rotationTrackCamera)
                {
                    // In attack mode or rotation is specified to track te camera
                    GetComponent<Rigidbody>().rotation = Quaternion.Euler(0, PlayerInput.Input.lookingAngle, 0);
                }
                else if (!Mathf.Approximately(horizontalVelocity.magnitude, 0))
                {
                    // Lerp towards direction of velocity
                    GetComponent<Rigidbody>().rotation = Quaternion.Lerp(GetComponent<Rigidbody>().rotation, Quaternion.LookRotation(horizontalVelocity, Vector3.up), 20.0F * Time.fixedDeltaTime);
                }
            }

        }
    }


    protected Entity aimedEnemy;
    void ProcessAimAssistTarget()
    {
        if (GetCamera() == null) return;

        Entity aimedEnemy = GetNearestEnemy(GetAimAssistMaxDistance(), bearingAngle: GetCamera().aimAssistBearingRange, pitchAngle: GetCamera().aimAssistPitchRange, relativeToLookingDirection: true);

        GetCamera().aimAssistedEntity = aimedEnemy;

        // if (aimedEnemy != null)
        // {
        //     Outliner.Outline(aimedEnemy.gameObject, Color.red, Time.fixedDeltaTime);
        // }
    }

    public abstract float GetAimAssistMaxDistance();

    public override void Update()
    {
        base.Update();

        if (isServer)
        {
            UpdateInteractionPromptSelection();
        }

        if (isOwned && entityReady)
        {
            // ApplyInputData(); //Moved to Player.cs

            if (currentTargetPaintMode != null)
            {
                currentTargetPaintMode.Scan(this);
            }

            Vector3 absoluteAdditiveMovement = new();
            if (objectAnchoredTo != null)
            {
                if (objectAnchoredToLastPos != Vector3.zero)
                {
                    absoluteAdditiveMovement = objectAnchoredTo.transform.position - objectAnchoredToLastPos;
                    //Debug.Log(absoluteAdditiveMovement);
                }

                objectAnchoredToLastPos = objectAnchoredTo.transform.position;
            }

            //if (absoluteAdditiveMovement != Vector3.zero)
            //characterController.Move(absoluteAdditiveMovement);


            CalculateGlobalVelocity();
            CalculateGravity();


            // Tell server what our velocity is. This allows it to animate us. TODO reroute this to come from world manager, after validation
            CmdSetMovementData(globalVelocity, inputVelocity, airborne, sprinting);
        }

        UpdateAimRigTargetPosition();

        Mathf.Clamp(rigWeight, 0, 1.0F);
        float currentRigWeight = rigWeight;
        if (cameraInFront)
        {
            currentRigWeight = 0;
        }

        foreach (Rig rig in GetComponentsInChildren<Rig>())
        {
            if (rig.gameObject.CompareTag("Suppressable"))
            {
                float clampedWeight = Mathf.Clamp(currentRigWeight, 0, 1);
                float lerpedWeight = Mathf.Lerp(rig.weight, Mathf.Clamp(clampedWeight, 0, 1), RIG_LERP_VALUE * Time.deltaTime);
                rig.weight = Mathf.Clamp(lerpedWeight, 0, 1); // re-clamp to prevent unusual values
            }
        }
    }


    public override void LateUpdate()
    {
        base.LateUpdate();

        if (isOwned && entityReady)
        {
            //UpdateUpperBodyAim();
            //
            //if (NetworkServer.active)
            //{
            //    UpdateAimRigTargetPosition();
            //}

            UpdateBlobShadow();
        }
    }


    public override void OnDestroy()
    {
        base.OnDestroy();

        if (owningPlayer != null)
        {
            owningPlayer.OnClientLevelUp -= LevelUp;
        }
    }

    void OnAnimatorMove()
    {
        if (animator.deltaPosition != Vector3.zero)
        {
            body.position += animator.deltaPosition * 1.5F;
        }
        // Vector3 velocityDifference = (animator.deltaPosition / Time.deltaTime) - body.linearVelocity;

        // body.AddForce(velocityDifference, ForceMode.VelocityChange);
    }


    #endregion


    Vector3 CalculateExternalVelocity()
    {
        for (int i = velocitySources.Count - 1; i >= 0; i--)
        {
            if (velocitySources[i] == null)
            {
                velocitySources.RemoveAt(i);
            }
        }

        Vector3 velocity = new();
        foreach (GameObject source in velocitySources)
        {
            //Debug.Log("source " + source + source.GetVelocityApplied(this));
            velocity += source.GetComponent<IVelocitySource>().GetVelocityApplied(this);
        }

        return velocity;
    }
    void UpdateBlobShadow()
    {
        if (blobShadow != null)
        {
            Physics.Raycast(GetWorldPosCenter(), Vector3.down, out RaycastHit hit, 200.0F, LayerMask.GetMask("Default"));
            blobShadow.transform.position = hit.point;
        }
    }


    public override void OwningPlayerChanged(Player oldPlayer, Player newPlayer)
    {
        if (oldPlayer != null)
        {
            oldPlayer.OnClientLevelUp -= LevelUp;
        }

        if (newPlayer != null)
        {
            newPlayer.OnClientLevelUp += LevelUp;
        }
    }

    void LevelUp(int level)
    {
        levelupVFXObject.GetComponent<VisualEffect>().SendEvent("PlayEffect"); //SendMessage("PlayEffect");
    }


    #region Traits and Stats

    void DebugAddTraits()
    {
        //ApplyTrait(new SeekingSpirits());
        //ApplyTrait(new Celerity());
        //ApplyTrait(new Celerity());
        //ApplyTrait(new Celerity());
        //ApplyTrait(new Celerity());
        //
        //ApplyTrait(new LethalAccuracy());
        //
        //EvaluateStats();
        //Debug.Log("DEBUG TRAITS ADDED");
    }


    public override void SetDefaultStats()
    {
        base.SetDefaultStats();

        SetStat(Stats.MovementSpeed, baseMoveSpeed);
        SetStat(Stats.SprintSpeedMult, baseSprintSpeedMult);
        SetStat(Stats.NumberOfJumps, baseNumberOfJumps);
        SetStat(Stats.DodgeSpeedMult, baseDodgeSpeedMult);
    }
    #endregion


    #region Input

    [Client]
    public virtual void ProcessInput()
    {
        // Debug.Log("Process CE Input");
        PlayerInputData inputData = PlayerInput.Input;

        if (inputData == null)
        {
            return;
        }

        inputVelocity = new(inputData.movementInput.x, 0, inputData.movementInput.y);

        dodgeCooldownRemaining -= Time.deltaTime;

        if (inputData != null && !IsStunned() && !isDead && !InGUI())
        {
            if (inputData.dodge && CanDodge() && inputData.movementInput.magnitude >= 0.5F)
            {
                Dodge(new Vector3(inputData.movementInput.x, 0, inputData.movementInput.y));
            }


            if (inputData.basicAttack1)
            {
                if (dodging && CanDodgeAttack())
                {
                    CmdDodgeAttack1((dodgeDuration - dodgeTimeRemaining) / dodgeDuration);
                    attackedThisDodge = true;
                }
                else if (!grounded && CanJumpAttack())
                {
                    CmdJumpAttack1();
                }
                else
                {
                    CmdAttack1();
                }
            }

            if (inputData.releaseBasicAttack1)
            {
                CmdReleaseAttack1();
            }

            if (inputData.releaseBasicAttack2)
            {
                CmdReleaseAttack2();
            }

            if (inputData.basicAttack2)
            {
                CmdAttack2();
            }

            if (inputData.basicAttack3)
            {
                Debug.Log("TERTIARY");
                CmdAttack3();
            }

            if (inputData.releaseBasicAttack3)
            {
                CmdReleaseAttack3();
            }

            // Sprinting handled last because some things that combo with sprint break you out of it
            bool sprintingPreviously = sprinting;
            if (Settings.settings.controlsSettings.toggleSprint) // only change sprint when toggle pressed, and then only flip it
            {
                if (inputData.sprintDown) sprinting = !sprinting;
            }
            else // sprint while button held
            {
                if (inputData.sprintDown)
                    sprinting = true;
                else if (inputData.sprintReleased)
                    sprinting = false;
            }

            // Sprint breakers - stopping moving, attacking, 
            if (inputData.movementInput.magnitude < 0.5F || inputData.basicAttack1)
                sprinting = false;

            if (!sprintingPreviously && sprinting)
                dashVFX.SendEvent("StartSprint");
            else if (sprintingPreviously && !sprinting)
                dashVFX.SendEvent("EndSprint");


            if (inputData.jump && CanJump())
            {
                Jump();
            }

            if (inputData.transform)
            {
                CmdTransform();
            }


            if (inputData.interact)
            {
                CmdInteract();
            }

            if (inputData.interactHeld)
            {
                CmdHoldInteract();
            }


            if (inputData.talk && selectedInteractionPrompt != null)
            {
                if (selectedInteractionPrompt.TryGetComponent(out DialogueAgent dialogueAgent))
                {
                    dialogueAgent.Talk(this);
                }
            }

            if (inputData.trinket)
            {
                CmdUseTrinket();
            }

            cameraContext.AddRotation(inputData.lookInput * GetCameraSensitivity());

            if (inputData.basicAttack2)
            {
                lastAttackModeActionTime = Time.time;
            }

            cameraInFront = false;
            if (GetCamera() != null)
            {
                float headingAngle = GetRelativeBearingAngle(GetCamera().gameObject);
                cameraInFront = headingAngle <= 55 && headingAngle >= -55;
            }

            if (inputData.flipAimOffset && GetCameraContext() != null)
            {
                GetCamera().offsetXFlipped = !GetCamera().offsetXFlipped;
            }
        }
    }

    public float GetDodgeCooldownRemaining()
    {
        return dodgeCooldownRemaining;
    }

    public bool CanDodge()
    {
        return CanMove() && !beingPushed && !dodging && !dodgeBlocked && dodgeCooldownRemaining <= 0;
    }

    [Command]
    void CmdInteract()
    {
        if (selectedInteractionPrompt == null)
        {
            return;
        }

        selectedInteractionPrompt.Interact(this);
    }

    [Command]
    void CmdHoldInteract()
    {
        if (selectedInteractionPrompt == null || selectedInteractionPrompt is not HeldInteractionPrompt heldPrompt)
        {
            return;
        }

        heldPrompt.Hold(this);
    }

    // By default only client sees its input data, this gives it to the server
    [Command]
    protected void CmdSetMovementData(Vector3 globalVelocity, Vector3 inputVelocity, bool airborne, bool sprinting)
    {
        if (gameObject == null)
        {
            return;
        }

        //this.globalVelocity = velocity;
        //this.inputVelocity = inputVelocity;
        RpcBroadcastVelocity(globalVelocity, inputVelocity);

        this.airborne = airborne;
        this.sprinting = sprinting;
    }


    /// <summary>
    /// Method to send global velocity to other clients. I don't want to make globalVelocity a syncvar and screw with latency for movement
    /// </summary>
    /// <param name="globalVelocity"></param>
    [ClientRpc(includeOwner = false)]
    void RpcBroadcastVelocity(Vector3 globalVelocity, Vector3 inputVelocity)
    {
        if (isOwned)
        {
            return;
        }

        this.globalVelocity = globalVelocity;
        this.inputVelocity = inputVelocity;
    }


    #endregion


    #region Movement
    /// <summary>
    /// Teleports this player somewhere
    /// </summary>
    /// <param name="worldPos"></param>
    [TargetRpc]
    public void TargetSetPosition(Vector3 worldPos)
    {
        body.Move(worldPos, transform.rotation);
        body.linearVelocity = new();
    }


    /// <summary>
    /// Teleports this player somewhere
    /// </summary>
    /// <param name="worldPos"></param>
    [TargetRpc]
    public void TargetSetPositionAndRotation(Vector3 worldPos, Quaternion rotation)
    {
        if (isOwned)
        {
            transform.position = worldPos;
            transform.rotation = rotation;
        }
    }

    void CalculateGlobalVelocity()
    {
        if (movementMode != MovementMode.PlayerControlled)
        {
            globalVelocity = new(0, globalVelocity.y, 0);
            return;
        }

        Physics.SyncTransforms();
        Vector3 commandedVelocity = new();


        if (dodging)
        {
            globalVelocity = new(currentDodgeVelocity.x, 0, currentDodgeVelocity.z);
            dodgeTimeRemaining -= Time.deltaTime;

            if (dodgeTimeRemaining <= 0.0F)
            {
                EndDodge();
            }
            else
            {
                return;
            }
        }

        if (CanMove())
        {
            if (dodging)
            {
                globalVelocity = new(currentDodgeVelocity.x, 0, currentDodgeVelocity.z);
                dodgeTimeRemaining -= Time.deltaTime;

                if (dodgeTimeRemaining <= 0.0F)
                {
                    dodgeCooldownRemaining = baseDodgeCooldown;
                    dodging = false;
                    attackedThisDodge = false;

                    foreach (Trait trait in GetTraits().ToProcOrderList())
                    {
                        trait.OnDodgeEnd(this);
                    }
                }
                else
                {
                    return;
                }
            }

            float moveSpeedMult = GetMovespeedMult();
            if (moveSpeedMult < 0) moveSpeedMult = 0;

            float finalMoveSpeed = GetStat(Stats.MovementSpeed) * moveSpeedMult;
            if (sprinting)
            {
                finalMoveSpeed *= GetStat(Stats.SprintSpeedMult);
            }

            Vector3 input = new();
            input.x = PlayerInput.Input.movementInput.x;
            input.z = PlayerInput.Input.movementInput.y;

            Vector3 cameraAdjustedInput = cameraContext.transform.TransformVector(input);
            cameraAdjustedInput.y = 0;

            // Normalize input only if needed. Otherwise, values <1 are partial joystick press and should reflect reduced movement
            if (cameraAdjustedInput.magnitude > 1.0F)
            {
                cameraAdjustedInput = cameraAdjustedInput.normalized;
            }
            else if (cameraAdjustedInput.magnitude <= 0.5F) // TODO settings configurable deadzone
            {
                // If less than a certain threshold, disregard input entirely
                cameraAdjustedInput = new();
            }


            commandedVelocity = cameraAdjustedInput * finalMoveSpeed;

            // Check if there are any velocity bonuses we need to apply
            if (velocityToAdd != Vector3.zero)
            {
                commandedVelocity += velocityToAdd;
                velocityToAdd = Vector3.zero;
            }
        }

        Vector3 stairVelocity = GetStairVelocityNecessary(commandedVelocity);
        commandedVelocity += stairVelocity;


        if (attacking)
        {
            commandedVelocity *= GetAttackingMovespeedMult();
        }
        commandedVelocity.y = globalVelocity.y;

        Vector3 projectedVelocity = commandedVelocity;
        // if (Mathf.Abs(commandedVelocity.y) < (0.8F * jumpMult) && commandedVelocity.magnitude > 0.5F && grounded)
        // {
        //     Vector3 groundNormal = GetGroundNormal();
        //     projectedVelocity = Vector3.ProjectOnPlane(commandedVelocity, groundNormal);
        // }

        Vector3 finalMovement = projectedVelocity;
        float yVal = finalMovement.y;
        float lerpVal = 1F;
        if (grounded)
        {
            lerpVal = GetSurfaceGrip();
        }
        else
        {
            // Only apply airborne lerping to acceleration
            if (globalVelocity.magnitude < finalMovement.magnitude)
            {
                lerpVal = AIRBORNE_HORIZONTAL_VELOCITY_LERP;
            }
        }
        // Lerp horizontally
        globalVelocity = Vector3.Lerp(
            Vector3.ProjectOnPlane(globalVelocity, Vector3.up),
            Vector3.ProjectOnPlane(finalMovement, Vector3.up),
            6.0F * lerpVal * Time.deltaTime
        );

        // Then set Y absolutely
        globalVelocity.y = yVal;
    }

    protected void EndDodge()
    {
        dodgeCooldownRemaining = baseDodgeCooldown;
        dodging = false;
        attackedThisDodge = false;

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnDodgeEnd(this);
        }
    }

    public virtual float GetAttackingMovespeedMult()
    {
        return 0.6F;
    }

    public virtual float GetSurfaceGrip()
    {
        Collider collider = GetComponent<Collider>();

        Vector3 origin = collider.bounds.center - new Vector3(0, collider.bounds.extents.y, 0) + new Vector3(0, 0.1F, 0); ;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 0.5F, LayerMask.GetMask("Default")) && hit.collider.gameObject.CompareTag("Ice"))
        {
            return 0.4F;
        }

        return 1.0F;
    }

    void CalculateGravity()
    {
        float newVelocityY = globalVelocity.y;

        if (resettingFall && newVelocityY <= 0.0F)
        {
            resettingFall = false;
            newVelocityY = 0.0F;
        }

        float gravityThisDelta = Physics.gravity.y * Time.deltaTime;
        if (newVelocityY < -0.2F) // allow some wiggle room before accelerating fall
        {
            gravityThisDelta *= FALL_MULT;
        }

        newVelocityY += gravityThisDelta;

        if (grounded && (Time.time - lastJump) > 0.5F)
        {
            airborne = false;
            if (newVelocityY < -0.5)
            {
                newVelocityY = -0.2F;
                jumpsRemaining = (int)GetStat(Stats.NumberOfJumps);
            }
        }
        else
        {
            if (newVelocityY < -10.0F && !airborne)
            {
                airborne = true;
            }
        }

        globalVelocity.y = newVelocityY;
    }

    void Jump()
    {
        globalVelocity.y = jumpMult;

        jumpsRemaining--;

        attacking = false;

        airborne = true;
        CmdJump();

        if (dodging)
        {
            EndDodge();
        }

        objectAnchoredTo = null;

        lastJump = Time.time;
    }

    public void SetAnchoredObject(GameObject anchoredObject)
    {
        if (Time.time - lastJump <= 0.25F && anchoredObject != null)
        {
            return;
        }

        objectAnchoredTo = anchoredObject;
    }


    Vector3 GetStairVelocityNecessary(Vector3 commandedVelocity)
    {
        Vector3 localCommandedVelocity = transform.InverseTransformDirection(commandedVelocity);

        Vector3 commandedVelocityAdjusted = new(commandedVelocity.x, Mathf.Clamp(commandedVelocity.y, 0, Mathf.Infinity), commandedVelocity.z);
        Vector3 direction = commandedVelocityAdjusted.magnitude > 0.5F ? commandedVelocityAdjusted.normalized : transform.forward;
        // Establish parameters
        float rayLength = GetComponent<CapsuleCollider>().radius + 0.5F;
        List<float> minHeightChecks = new()
        {
            0.3F, 0.15F, 0.0F // Reverse order to start at top
        };

        List<float> lateralOffsets = new()
        {
            -0.5F, 0,  0.5F
        };

        bool minHeightHit = false;
        float minHeightHitHeight = 0.0F;
        float workingLateralOffset = 0;

        foreach (float offset in lateralOffsets)
        {
            foreach (float checkHeight in minHeightChecks)
            {
                Vector3 stairMinOrigin = moveCollider.bounds.center - transform.TransformVector(new Vector3(offset, (moveCollider.bounds.extents.y - checkHeight), 0));
                Ray lowerRay = new(stairMinOrigin, direction);
                if (Physics.Raycast(lowerRay, out RaycastHit lowerHit, rayLength, stairLayer)) //stairMinOrigin, transform.forward, out RaycastHit lowerHit, rayLength, stairLayer))
                {
                    if (lowerHit.collider.tag == "Stairs")
                    {
                        minHeightHit = true;
                        minHeightHitHeight = checkHeight;
                        workingLateralOffset = offset;
                    }
                }


                Debug.DrawRay(stairMinOrigin, direction * rayLength, minHeightHit ? Color.green : Color.white);
                if (minHeightHit)
                {
                    break;
                }
            }

            if (minHeightHit)
            {
                break;
            }

        }

        if (!minHeightHit)
        {
            return Vector3.zero;
        }

        List<float> maxHeightChecks = new()
        {
            minHeightHitHeight + 0.1F,
            minHeightHitHeight + 0.2F,
            minHeightHitHeight + 0.3F,
            minHeightHitHeight + 0.4F,
            minHeightHitHeight + 0.5F,
            minHeightHitHeight + 0.6F,
            minHeightHitHeight + 0.7F,
        };
        bool maxHeightHit = false;
        float maxHeightHitHeight = 0.0F;
        foreach (float checkHeight in maxHeightChecks)
        {
            Vector3 stairMaxOrigin = moveCollider.bounds.center - transform.TransformVector(new Vector3(workingLateralOffset, (moveCollider.bounds.extents.y - checkHeight), 0));

            Ray upperRay = new(stairMaxOrigin, direction);//new(stairMinOrigin, transform.forward, out RaycastHit lowerHit, rayLength, stairLayer)


            maxHeightHit = Physics.Raycast(upperRay, out RaycastHit upperHit, rayLength, stairLayer);

            Debug.DrawRay(stairMaxOrigin, direction * rayLength, !maxHeightHit ? Color.green : Color.cyan);
            if (!maxHeightHit)
            {
                maxHeightHitHeight = checkHeight;
                break;
            }
        }


        if (maxHeightHit)
        {
            return Vector3.zero;
        }


        // Now determine if it's an actual "step" or just a slope
        float verticalDiff = maxHeightHitHeight - minHeightHitHeight;


        // Check if input is appropriate to try and move over the step
        float dotProductCorrelationRequired = 0.5F;
        float dotProduct = Vector3.Dot(commandedVelocity, transform.forward);
        if (dotProduct > dotProductCorrelationRequired)
        {
            body.position += new Vector3(0, commandedVelocity.magnitude, 0) * Time.deltaTime;

        }


        return new();
    }

    public override void Move(Vector3 magnitude)
    {
        MoveRigidBody(magnitude);
    }

    void MoveRigidBody(Vector3 magnitude)
    {
        body.linearVelocity = magnitude;
    }


    public void SetDodgeBlocked(int dodgeBlocked)
    {
        this.dodgeBlocked = dodgeBlocked != 0;
    }

    [Command]
    void CmdJump()
    {
        PlayAnimation("Jump", 0.10F);
        RpcJump();
    }

    [ClientRpc]
    void RpcJump()
    {
        AudioManager.Singleton.PlayOneShot(jumpSound, transform.position);
    }

    [Client]
    protected virtual void Dodge(Vector3 dodgeDirection)
    {
        attackedThisDodge = false;
        lastDodge = Time.time;
        AudioManager.Singleton.PlayOneShot(dodgeSound, transform.position);
        if (isOwned)
        {
            dodging = true;
            dodgeTimeRemaining = dodgeDuration;
            currentDodgeVelocity = (GetCamera().transform.TransformVector(dodgeDirection).normalized * GetStat(Stats.MovementSpeed) * GetStat(Stats.DodgeSpeedMult)) + CalculateExternalVelocity();

            // if (TryGetComponent(out MeshTrail meshTrail))
            // {
            //     meshTrail.StartTrail(dodgeDuration);
            // }

            CmdDodge();
        }
    }

    [Command]
    void CmdDodge()
    {
        SetAnimatorTrigger("Dodge");

        dodging = true;
        invulnerable = true;
        attacking = false;

        Invoke(nameof(ServerEndDodge), dodgeInvulnerabilityDuration);

        RpcDodgeStart();

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnDodgeStart(this);
        }
    }

    [ClientRpc]
    protected void RpcDodgeStart()
    {
        dashVFX.SendEvent("Dash");
        // if (TryGetComponent(out MeshTrail meshTrail))
        // {
        //     meshTrail.StartTrail(dodgeDuration);
        // }
    }

    void ServerEndDodge()
    {
        invulnerable = false;
    }

    /// <summary>
    /// Interrupt the current fall and reset y velocity to zero, on next FixedUpdate
    /// </summary>
    [ClientRpc]
    protected void RpcFallInterrupt()
    {
        resettingFall = true;
    }

    public override Vector3 GetVelocity()
    {
        return globalVelocity;
    }

    public override void AddVelocity(Vector3 velocity)
    {
        TargetAddVelocity(velocity);
    }

    [TargetRpc]
    void TargetAddVelocity(Vector3 velocity)
    {
        globalVelocity += velocity;
    }

    public Vector3 GetLocalVelocity()
    {
        return transform.TransformVector(globalVelocity);
    }

    bool CanJump()
    {
        return CanMove() && !dodgeBlocked && !inGUI && jumpsRemaining > 0 && movementMode == MovementMode.PlayerControlled;
    }

    public override bool CanMove()
    {
        return base.CanMove() && !inGUI && !inJumpAttack && !attacking;
    }

    public override bool CanTurn()
    {
        return base.CanTurn() && !inJumpAttack;
    }


    /*
    void OnControllerColliderHit(ControllerColliderHit collision)
    {
        Vector3 footRayOrigin = moveCollider.bounds.center - new Vector3(0, moveCollider.bounds.extents.y);
        if (Physics.Raycast(footRayOrigin, Vector3.down, out RaycastHit groundhit, groundCheckRayLength, stairLayer))
        {
            Debug.Log("grounded");
            velocity.y = 0;
            if (airborne)
            {
                airborne = false;
                jumpsRemaining = numberOfJumps;

                OnGrounded();
            }
        }
        
    }
    */

    public override string GetDisplayName()
    {
        if (owningPlayer != null)
        {
            return owningPlayer.GetDisplayName();
        }

        return base.GetDisplayName();
    }

    #endregion


    public void PlayTransformationExplosion(Vector3 position)
    {
        AreaOfEffect.Create(transformationExplosion, position, this, HitType.None);
    }

    public void SetInGUI(bool inGUI)
    {
        this.inGUI = inGUI;
    }

    public bool InGUI()
    {
        return inGUI;
    }

    [Command]
    void CmdUseTrinket()
    {
        Trinket heldTrinket = GetOwningPlayer().GetTrinket();

        if (heldTrinket.GetCooldownRemaining() > 0)
        {
            return;
        }

        heldTrinket?.Use(this);
        RpcTrinketUsed();
    }

    [ClientRpc]
    void RpcTrinketUsed()
    {
        GetOwningPlayer().GetTrinket()?.ClientUsed(this);
    }


    public override float GetGlobalDamageMultiplier()
    {
        return Equation.ENEMY_GLOBAL_DAMAGE_MULTIPLIER.Calculate(GetOwningPlayer().level);
    }

    public PlayerCamera GetCamera()
    {
        if (owningPlayer == null)
        {
            return null;
        }


        return owningPlayer.GetCamera();
    }

    [ClientRpc]
    public virtual void RpcSetActive(bool active)
    {
        gameObject.SetActive(active);

        attacking = false;

        if (!active)
        {
            ClearTargetPaintMode();
        }
        else
        {
            UnpauseEntity();
        }
    }


    [Server]
    public bool HasInteractionPrompt(InteractionPrompt interactionPrompt)
    {
        return currentAvailableInteractionPrompts.Contains(interactionPrompt);
    }

    [Server]
    public void AddInteractionPrompt(InteractionPrompt interactionPrompt)
    {
        if (!currentAvailableInteractionPrompts.Contains(interactionPrompt))
        {
            currentAvailableInteractionPrompts.Add(interactionPrompt);
        }
    }

    [Server]
    public void TargetRemoveInteractionPrompt(InteractionPrompt interactionPrompt)
    {
        if (interactionPrompt != null && currentAvailableInteractionPrompts.Contains(interactionPrompt))
        {
            currentAvailableInteractionPrompts.Remove(interactionPrompt);
        }
    }


    [Client]
    protected void EnterTargetPaintMode(TargetPaintMode targetPaintMode)
    {
        currentTargetPaintMode = targetPaintMode;
        currentTargetPaintMode.ResetData();

        cameraContext.AddOffset(targetPaintMode.offset);

        GetOwningPlayer().GetUI().ShowReticle();
    }

    /// <summary>
    /// Exit target painting mode
    /// </summary>
    /// <returns>A list of enemies target painted</returns>
    [Client]
    protected List<Entity> ClearTargetPaintMode()
    {
        if (currentTargetPaintMode == null || GetOwningPlayer() == null)
        {
            return new();
        }

        //GetOwningPlayer().GetUI().HideReticle();
        List<Entity> ret = currentTargetPaintMode.GetTargets();

        cameraContext.RemoveOffset(currentTargetPaintMode.offset);
        currentTargetPaintMode = null;

        return ret;
    }

    [Server]
    public override void HitAnEntity(HitInfo hitResult)
    {
        base.HitAnEntity(hitResult);

        if (gameObject.activeSelf)
        {
            StartCoroutine(HitStop(hitResult));
        }
    }


    IEnumerator HitStop(HitInfo hitResult)
    {
        PauseEntity();

        if (!hitResult.entityHit.IsDead() && hitResult.entityHit != null)
        {
            hitResult.entityHit.PauseEntity();
        }

        yield return null;
        yield return null;

        UnpauseEntity();

        if (!hitResult.entityHit.IsDead() && hitResult.entityHit != null)
        {
            hitResult.entityHit.UnpauseEntity();
        }
    }

    public void SetRotationTrackCamera(bool activated)
    {
        rotationTrackCamera = activated;
    }

    void UpdateAimRigTargetPosition()
    {
        if (upperBodyAimTransform != null && GetCamera() != null)
        {
            upperBodyAimTransform.position = GetCamera().GetAimedPosition();
        }

        if (weaponAimTransform != null && GetCamera() != null)
        {
            weaponAimTransform.position = GetCamera().GetAimedPosition();
        }
    }

    public override Vector3 GetAimedPosition()
    {
        return GetCamera().GetAimedPosition();
    }

    
    public readonly Vector3 AIM_MODE_OFFSET = new Vector3(0, 0, 0.3F);
    [TargetRpc]
    public override void TargetEnterAimMode(bool includeEntities)
    {
        aiming = true;
        // GetCameraContext().ClearOffset();
        GetCameraContext().AddOffset(AIM_MODE_OFFSET);

        GetCamera().OverridePreferredDistance(2.0F);
        GetCamera().SetAimIncludeEntities(includeEntities);

        SetRotationTrackCamera(true);
        GetOwningPlayer().GetUI().ShowReticle();
    }

    [TargetRpc]
    public override void TargetExitAimMode()
    {
        ExitAimMode();
    }

    public void ExitAimMode()
    {
        if (!aiming)
        {
            return;
        }

        aiming = false;
        if (GetCamera() == null)
        {
            return;
        }

        GetCameraContext().RemoveOffset(AIM_MODE_OFFSET);
        GetCamera().ClearPreferredDistanceOverrides();
        GetCamera().SetAimIncludeEntities(false);
        SetRotationTrackCamera(false);
        //GetOwningPlayer().GetUI().HideReticle();
    }

    protected IEnumerator SetRigActive(Rig rig, bool active = true)
    {
        yield return new WaitForEndOfFrame();

        rig.weight = active ? 1 : 0;
    }


    public void SetRigWeight(float value, float transitionDuration)
    {
        StartCoroutine(SetRigWeightCoroutine(value, transitionDuration));
    }

    IEnumerator SetRigWeightCoroutine(float value, float transitionDuration)
    {
        yield return new WaitForEndOfFrame();

        float startTime = Time.time;

        float change = value - rigWeight;

        while ((Time.time - startTime) <= transitionDuration)
        {
            rigWeight += change * Time.deltaTime * (1 / transitionDuration);
            yield return new WaitForFixedUpdate();
        }

        rigWeight = value;
    }


    public override void Die()
    {
        base.Die();

        globalVelocity = Vector3.zero;
        inputVelocity = Vector3.zero;
    }

    public virtual float GetCameraSensitivity()
    {
        if (aiming) return Settings.settings.controlsSettings.aimedSensitivity;

        return Settings.settings.controlsSettings.cameraSensitivity;
    }

}



[Serializable]
public class TargetPaintMode
{
    public Color color;
    public Vector3 offset;
    public float maxRange;
    public int numberOfTargets = 1;

    public bool targetAllies = false;

    readonly List<Entity> paintedEntities = new();

    public delegate void TargetPaintAction(Entity target);
    public event TargetPaintAction OnTargetPainted;
    public event TargetPaintAction OnTargetUnpainted;

    public void ResetData()
    {
        paintedEntities.Clear();
    }

    public void Scan(ControlledEntity owner)
    {
        float closestDistance = Mathf.Infinity;
        Entity closest = null;
        RaycastHit[] hits = Physics.RaycastAll(Camera.main.transform.position, Camera.main.transform.forward, maxRange <= 0 ? Mathf.Infinity : maxRange, LayerMask.GetMask("Entities"));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.TryGetComponent(out Entity entity))
            {
                if (entity != owner && (!entity.IsAlly(owner) || targetAllies))
                {
                    if (hit.distance < closestDistance)
                    {
                        closest = entity;
                    }
                }
            }
        }

        if (closest != null && !paintedEntities.Contains(closest))
        {
            if (paintedEntities.Count >= numberOfTargets)
            {
                UnpaintFirst();
            }

            Paint(closest);
        }
    }

    public List<Entity> GetTargets()
    {
        return paintedEntities;
    }

    void Paint(Entity target)
    {
        paintedEntities.Add(target);
        OnTargetPainted?.Invoke(target);

        Outliner.Outline(target.gameObject, color);
    }

    void UnpaintFirst()
    {
        Outliner.ClearOutline(paintedEntities[0].gameObject);

        OnTargetUnpainted?.Invoke(paintedEntities[0]);
        paintedEntities.RemoveAt(0);
    }

}
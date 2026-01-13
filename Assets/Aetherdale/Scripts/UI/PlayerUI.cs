using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using System;
using Aetherdale;
using UnityEngine.Serialization;
using UnityEngine.UI;

// The player's UI

// This should be set to execute before default in script execution

public class PlayerUI : NetworkBehaviour, IOnLocalPlayerReadyTarget
{
    public const float floatingUIRenderDistance = 40.0F; // max distance at which we should render floating UI for applicable entities

    [Header("Configuration")]
    [SerializeField] ControlledEntityResourceWidget wraithResourceWidget;
    [SerializeField] ControlledEntityResourceWidget idolResourceWidget;
    [SerializeField] BossHealthBar bossHealthBar;
    [SerializeField] DialogueMenu dialogueMenu;
    [SerializeField] BackpackMenu backpackMenu;
    [SerializeField] PauseMenu pauseMenu;
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] JournalMenu journalMenu;
    //[SerializeField] BlacksmithMenu blacksmithMenu;
    [SerializeField] TraitSelectionMenu traitSelectionMenu;
    [SerializeField] ShopMenu shopMenu;

    [SerializeField] SequenceStartMenu sequenceStartMenu;
    [SerializeField] PostAreaMenu postAreaMenu;

    [SerializeField] LoadoutSelectionMenu loadoutSelectionMenu;

    [SerializeField] LockonTargetIndicator lockOnTargetIndicator;
    [SerializeField] ChatMenu chatMenu;

    [SerializeField][FormerlySerializedAs("uiVeil")] GameObject pauseVeil;
    [SerializeField] Image fadeToBlackVeil;
    [SerializeField] GameObject pausedObject;

    [SerializeField] CooldownWidget dodgeWidget;
    [SerializeField] CooldownWidget ability1Widget;
    [SerializeField] CooldownWidget ability2Widget;
    [SerializeField] CooldownWidget ultimateWidget;

    [SerializeField] GameObject traitSelectionReminder;
    [SerializeField] Reticle reticle;

    [Header("Prefabs")]
    [SerializeField] DamagePopup damagePopupPrefab;
    [SerializeField] FloatingHealthBar floatingHealthBarPrefab;
    [SerializeField] FloatingProgressBar floatingProgressBarPrefab;
    [SerializeField] NpcExclamation npcExclamationPrefab;
    [SerializeField] Nameplate nameplatePrefab;
    [SerializeField] ChatBubble npcChatterBubblePrefab;

    [SerializeField] FloatingInteractionPrompt interactionPrompt;


    // ----- Input ---------
    InputAction cancelInputAction;
    InputAction pauseMenuInputAction;
    InputAction chooseTraitInputAction;


    [SyncVar] Player owningPlayer = null;
    Player trackedPlayer = null;
    ControlledEntity playerEntity = null;
    Stack<Menu> openMenuStack = new();

    // Floating UI instances
    Dictionary<Entity, FloatingHealthBar> floatingHealthBars = new();
    Dictionary<Entity, Nameplate> nameplates = new();
    Dictionary<DialogueAgent, NpcExclamation> exclamations = new();
    Dictionary<DialogueAgent, ChatBubble> npcChatBubbles = new();

    Transform damagePopupPoolTransform;
    Pool<DamagePopup> damagePopupPool;
    int numDamagePopups = 0;


    bool fadedToBlack = false;
    float fadeToBlackLerpStrength = 1.0F;

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.activeSceneChanged += OnSceneChanged;

        if (isClient && isOwned)
        {
            Invoke(nameof(ClientInitialize), 0);
        }

        if (!isOwned)
        {
            gameObject.SetActive(false);
        }
    }


    void Start()
    {
        if (!isOwned)
        {
            gameObject.SetActive(false);
            return;
        }

        cancelInputAction = InputSystem.actions.FindAction("Cancel");
        pauseMenuInputAction = InputSystem.actions.FindAction("PauseMenu");
        chooseTraitInputAction = InputSystem.actions.FindAction("ChooseTrait");

        if (isServer)
        {
            Boss.OnBossEncounterStart += ShowBossHealthBar;
            Boss.OnBossEncounterEnd += ClearBossHealthBar;
        }

        RegisterNpcs();

        ShowReticle();

        Player.OnLocalPlayerQuestReceived += OnOwnerQuestStarted;
        PlayerDataRuntime.OnQuestCompleted += OnOwnerQuestCompleted;

        WorldManager.OnGamePaused += OnGamePaused;
        WorldManager.OnGameResumed += OnGameResumed;

        DialogueAgent.OnChatter += OnDialougeAgentChatter;
        DialogueAgent.OnChatterClear += OnDialogueAgentChatterClear;

        AetherdaleNetworkManager.singleton.OnBeforeSceneChange += OnBeforeSceneChange;

        ability1Widget.GetComponentInChildren<KeybindImage>().associatedInputActionName = "ability1";
        ability2Widget.GetComponentInChildren<KeybindImage>().associatedInputActionName = "ability2";
        ultimateWidget.GetComponentInChildren<KeybindImage>().associatedInputActionName = "ultimateAbility";

    }

    void OnDestroy()
    {
        Boss.OnBossEncounterStart -= ShowBossHealthBar;
        Boss.OnBossEncounterEnd -= ClearBossHealthBar;

        SceneManager.activeSceneChanged -= OnSceneChanged;

        Player.OnLocalPlayerQuestReceived -= OnOwnerQuestStarted;
        PlayerDataRuntime.OnQuestCompleted -= OnOwnerQuestCompleted;

        WorldManager.OnGamePaused -= OnGamePaused;
        WorldManager.OnGameResumed -= OnGameResumed;

        DialogueAgent.OnChatter -= OnDialougeAgentChatter;
        DialogueAgent.OnChatterClear -= OnDialogueAgentChatterClear;
    }


    void Update()
    {
        if (!isOwned)
        {
            gameObject.SetActive(false);
            return;
        }

        float fadeToBlackTargetWeight = fadedToBlack ? 1 : 0;
        if (fadeToBlackVeil.color.a != fadeToBlackTargetWeight)
        {
            Color newColor = fadeToBlackVeil.color;
            newColor.a = Mathf.MoveTowards(newColor.a, fadeToBlackTargetWeight, fadeToBlackLerpStrength * Time.deltaTime);
            fadeToBlackVeil.color = newColor;
        }

        fadeToBlackVeil.gameObject.SetActive(fadeToBlackVeil.color.a != 0);

        if (playerEntity != null)
        {
            
            dodgeWidget.SetAvailable(playerEntity.CanDodge());
            if (!playerEntity.CanDodge())
            {
                dodgeWidget.SetCooldownRemaining(playerEntity.GetDodgeCooldownRemaining());
            }

            SelectedInteractionPromptData data = playerEntity.GetSelectedInteractionPromptData();
            if (data != null)
            {
                if (!interactionPrompt.IsShown())
                {
                    interactionPrompt.Show();
                }

                interactionPrompt.SetData(data);

                //TODO need to rework how we get this transform - perhaps serializing a reference through mirror
                // if (interactionPrompt.GetWorldPositionTransform() != data.position)
                // {
                //     interactionPrompt.SetWorldPositionTransform(selectedInteractionPrompt.transform);
                // }
            }
            else if (interactionPrompt.IsShown())
            {
                interactionPrompt.Hide();
            }
        }

        // ProcessInput(); // Moved to Player.cs

    }

    void OnGamePaused()
    {
        //uiVeil.SetActive(true);
        pausedObject.SetActive(true);
    }

    void OnGameResumed()
    {
        //uiVeil.SetActive(false);
        pausedObject.SetActive(false);
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (!isOwned)
        {
            gameObject.SetActive(false);
        }

        if (isServer)
        {
            ClearBossHealthBar(null);

            RegisterNpcs();
        }
    }

    private void OnBeforeSceneChange()
    {
        Debug.Log("scene change");
        if (isOwned)
        {
            FadeToBlack(true, 2.0F);
        }
    }

    public void OnLocalPlayerReady(Player player)
    {
        if (isOwned)
        {
            StartCoroutine(LocalPlayerReadyCoroutine());
        }
    }

    IEnumerator LocalPlayerReadyCoroutine()
    {
        yield return new WaitForSeconds(0.5F);
        FadeToBlack(false, 2.0F);
    }

    [Client]
    void ClientInitialize()
    {
        //EventSystem eventSystem = Instantiate(eventSystemPrefab, this.gameObject.transform);
        bossHealthBar.gameObject.SetActive(false);

        // Ensure all menus start closed
        foreach (Menu menu in GetComponentsInChildren<Menu>())
        {
            menu.gameObject.SetActive(false);
        }

        lockOnTargetIndicator.Hide();

        interactionPrompt.Hide();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        pauseVeil.SetActive(false);

        Transform dpp = new GameObject("Damage Popup Pool").transform;
        dpp.parent = transform;
        damagePopupPoolTransform = dpp;
        damagePopupPool = new(CreateNewDamagePopup, 100);
    }

    DamagePopup CreateNewDamagePopup()
    {
        DamagePopup popup = Instantiate(damagePopupPrefab, damagePopupPoolTransform);
        popup.gameObject.name = $"DamagePopup_pooled_{numDamagePopups}";
        popup.gameObject.SetActive(false);
        popup.OnLifespanEnded += ReturnDamagePopup;

        numDamagePopups++;

        return popup;
    }

    DamagePopup GetDamagePopup()
    {
        DamagePopup popup = damagePopupPool.Get();
        popup.lifespanRemaining = popup.lifespan;
        popup.gameObject.SetActive(true);
        popup.transform.SetParent(transform);

        return popup;
    }

    void ReturnDamagePopup(DamagePopup popup)
    {
        popup.transform.SetParent(damagePopupPoolTransform);
        popup.gameObject.SetActive(false);
        damagePopupPool.Return(popup);
    }

    public void Clean()
    {
        // There will be invalid floating UI from the last scene, remove it
        foreach (FloatingUIElement element in gameObject.GetComponentsInChildren<FloatingUIElement>(true))
        {
            if (!element.IsValid())
            {
                Destroy(element.gameObject);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdEnableReminderIfNecessary()
    {
        if (GetOwningPlayer().traitsOwed > 0)
        {
            TargetTraitSelectionAvailable(true);
        }
    }

    [TargetRpc]
    public void TargetTraitSelectionAvailable(bool active)
    {
        SetTraitSelectionReminderVisible(active);
    }

    public void SetTraitSelectionReminderVisible(bool active)
    {
        traitSelectionReminder.SetActive(active);
    }


    [ClientRpc]
    public void RpcSetCamera(PlayerCamera playerCamera)
    {
        playerCamera.OnLookAtEntity += LookAtEntity;
    }


    void RegisterNpcs()
    {
        foreach (Entity worldEntity in FindObjectsByType<Entity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (worldEntity != null)
            {
                NonPlayerCharacter npc = worldEntity as NonPlayerCharacter;
                if (npc != null)
                {
                    RegisterNpc(npc);
                }
            }
        }
    }

    [Client]
    void LookAtEntity(Entity entity)
    {
        if ((entity is not ControlledEntity controlledEntity || controlledEntity != trackedPlayer.GetControlledEntity())
            && entity is not Boss
            && !entity.IsInvisible())
        {
            GetFloatingHealthBar(entity).Show();
        }
    }

    [Server]
    public static void ReportDamageInstance(HitInfo hitInfo)
    {
        if (hitInfo.entityHit == null)
        {
            return;
        }

        foreach (Player player in Player.GetPlayers())
        {
            player.GetUI().TargetReportDamageInstance(hitInfo);
        }
    }

    [Server]
    public static void ReportHealingInstance(Entity entityHealed, int healing, Entity healer)
    {
        foreach (Player player in Player.GetPlayers())
        {
            player.GetUI().TargetReportHealingInstance(entityHealed, healing, healer);
        }
    }

    // public static void AddProgressBar(HeldInteractionPrompt prompt)
    // {
    //     foreach (Player player in Player.GetPlayers())
    //     {
    //         player.GetUI().AddProgressBar(prompt);
    //     }
    // }

    [TargetRpc]
    public void TargetReportDamageInstance(HitInfo hitInfo)
    {
        if (isOwned)
        {
            DamagePopup damagePopup = GetDamagePopup();
            damagePopup.Initialize(hitInfo.hitPosition, hitInfo.hitResult, hitInfo.damageDealt, hitInfo.damageType, transform, critical: hitInfo.criticalHit);

            if (hitInfo.entityHit != null)
            {
                ControlledEntity asControlledEntity = hitInfo.entityHit as ControlledEntity;
                Boss asBoss = hitInfo.entityHit as Boss;
                if ((asControlledEntity == null || asControlledEntity != trackedPlayer.GetControlledEntity()) && asBoss == null)
                {
                    FloatingHealthBar fhb = GetFloatingHealthBar(hitInfo.entityHit);
                    fhb.Show();
                }
            }
        }
    }



    [TargetRpc]
    public void TargetReportHealingInstance(Entity healedEntity, int healing, Entity healer)
    {
        if (healedEntity == null)
        {
            // damage taker was deleted/unspawned since taking the damage
            return;
        }

        if (isOwned)
        {
            DamagePopup damagePopup = GetDamagePopup();
            damagePopup.Initialize(healedEntity.GetWorldPosCenter(), HitResult.Healed, healing, Element.Healing, transform);

            ControlledEntity asControlledEntity = healedEntity as ControlledEntity;
            Boss asBoss = healedEntity as Boss;
            if ((asControlledEntity == null || asControlledEntity != trackedPlayer.GetControlledEntity()) && asBoss == null)
            {
                FloatingHealthBar fhb = GetFloatingHealthBar(healedEntity);
                fhb.Show();
            }
        }
    }

    public ChatMenu GetChatMenu()
    {
        return chatMenu;
    }

    void RegisterNpc(NonPlayerCharacter npc)
    {
        Nameplate nameplate = Instantiate(nameplatePrefab, transform);
        nameplate.SetDisplayedName(npc.GetName());

        nameplate.SetOwner(npc);

        if (npc.gameObject.TryGetComponent(out DialogueAgent dialogueAgent))
        {
            if (dialogueAgent.HasPriorityTopic())
            {
                NpcExclamation npcExclamation = Instantiate(npcExclamationPrefab, transform);
                npcExclamation.SetOwner(npc);
                npcExclamation.SetOffset(new Vector3(0, 1.0F, 0));

                exclamations.Add(dialogueAgent, npcExclamation);
                DialogueAgent.OnTopicSpoken += ReevaluateExclamation;
            }
        }
    }

    void ReevaluateExclamation(DialogueAgent agent, Player player, DialogueTopic dialogueTopic)
    {
        if (!agent.HasPriorityTopic() && exclamations.ContainsKey(agent))
        {
            Destroy(exclamations[agent].gameObject);
            exclamations.Remove(agent);
        }
    }

    /// <summary>
    /// Returns the floating health bar for an entity, creates it if needed
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    FloatingHealthBar GetFloatingHealthBar(Entity entity)
    {
        if (!floatingHealthBars.ContainsKey(entity))
        {
            FloatingHealthBar fhb = Instantiate(floatingHealthBarPrefab, transform);
            fhb.SetOwner(entity);

            entity.OnEffectAdded += fhb.AddEffect;
            entity.OnEffectRemoved += fhb.RemoveEffect;

            floatingHealthBars.Add(entity, fhb);
        }

        return floatingHealthBars[entity];
    }

    public static void AddProgressBar(Progressable progressable)
    {
        PlayerUI playerUI = Player.GetLocalPlayer().GetUI();
        playerUI.AddProgressBarToMe(progressable);
    }

    void AddProgressBarToMe(Progressable progressable)
    {
        Debug.Log("Player ui received progress bar");
        FloatingProgressBar fpb = Instantiate(floatingProgressBarPrefab, transform);
        fpb.SetWorldPosition(progressable.transform.position);
        fpb.SetProgressable(progressable);
    }

    private void OnOwnerQuestStarted(Quest inst)
    {
        inst.OnQuestObjectiveStarted += (Objective obj) => OnOwnerObjectiveStarted(inst, obj);
        inst.OnQuestObjectiveCompleted += (Objective obj) => OnOwnerObjectiveCompleted(inst, obj);
    }

    private void OnOwnerQuestCompleted(Quest inst)
    {
        inst.OnQuestObjectiveStarted -= (Objective obj) => OnOwnerObjectiveStarted(inst, obj);
        inst.OnQuestObjectiveCompleted -= (Objective obj) => OnOwnerObjectiveCompleted(inst, obj);
    }

    private void OnOwnerObjectiveStarted(Quest quest, Objective obj)
    {
    }

    private void OnOwnerObjectiveCompleted(Quest quest, Objective obj)
    {
    }

    // Input processing for when a menu is open
    public void ProcessInput()
    {
        int previousMenuStackCount = openMenuStack.Count;

        if (chatMenu.IsActive())
        {
            return;
        }

        if (cancelInputAction.WasPressedThisFrame())
        {
            if (openMenuStack.Count > 0)
            {
                PopMenu();
            }
        }

        if (previousMenuStackCount == 0 && openMenuStack.Count == 0 && pauseMenuInputAction.WasPressedThisFrame())
        {
            OpenPauseMenu();
            return;
        }

        // Allow free cursor while tab is held. This may build into some kind of scoreboard eventually but for now its just for cursor control
        // TODO how do we do this with controller?
        if (Input.GetKey(KeyCode.Tab))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerEntity != null)
            {
                playerEntity.SetInGUI(true);
            }
        }
        else if (openMenuStack.Count == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerEntity != null)
            {
                playerEntity.SetInGUI(false);
            }
        }


        if (!traitSelectionMenu.IsOpen() && chooseTraitInputAction.WasPressedThisFrame()
            && playerEntity != null
            && !playerEntity.IsDead()
            && owningPlayer.traitsOwed > 0)
        {
            OpenAndPushMenu(traitSelectionMenu);
            CmdOpenTraitSelectionMenu();
        }

        if (Input.GetKeyDown(KeyCode.L)
            && playerEntity != null
            && !playerEntity.InGUI()
            && AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            OpenAndPushMenu(loadoutSelectionMenu);
            CmdOpenLoadoutMenu();
        }

        // Process input for focused (currently last) menu
        if (openMenuStack.Count > 0)
        {
            openMenuStack.Peek().ProcessInput();
        }

    }

    IEnumerator ClearPlayerGUIStatus()
    {
        yield return new WaitForEndOfFrame();

        if (playerEntity != null)
        {
            playerEntity.SetInGUI(false);
        }
    }

    // Setting the player instance this UI belongs to
    [Server]
    public void SetOwningPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogWarning("Attempted to set owning player to null");
            return;
        }

        owningPlayer = player;
        RpcSetTrackedPlayer(player);
    }

    [ClientRpc]
    public void RpcSetTrackedPlayer(Player player)
    {
        SetTrackedPlayer(player);
    }

    [Client]
    public void SetTrackedPlayer(Player player)
    {
        Debug.Log("Set tracked player to " + player.GetPlayerId());
        trackedPlayer = player;

        if (trackedPlayer == null)
        {
            Debug.LogWarning("UI player was set to null");
            return;
        }

        SetOwningEntity(trackedPlayer.GetControlledEntity());

        trackedPlayer.OnEntityChangedOnClient += SetOwningEntity;

        if (trackedPlayer.GetWraithForm() is PlayerWraith wraith && wraith != null)
        {
            wraithResourceWidget.SetTrackedEntity(wraith);
        }

        if (trackedPlayer.GetIdolForm() is IdolForm idol && idol != null)
        {
            idolResourceWidget.SetTrackedEntity(idol);
        }

        trackedPlayer.OnNewIdolFormOnClient += idolResourceWidget.SetTrackedEntity;
        trackedPlayer.OnNewWraithFormOnClient += wraithResourceWidget.SetTrackedEntity;
    }

    // Changing the entity a player controls, but not the player instance
    void SetOwningEntity(ControlledEntity newEntity)
    {
        playerEntity = newEntity;

        if (playerEntity == null)
        {
            return;
        }

        dodgeWidget.SetInfo("Dodge", "Perform a dodge, becoming briefly invulnerable");
        dodgeWidget.SetAvailable(newEntity.CanDodge());

        if (playerEntity is IdolForm idol)
        {
            ability1Widget.gameObject.SetActive(true);
            ability1Widget.SetIcon(idol.Ability1Icon);
            ability1Widget.SetInfo(idol.Ability1Name, idol.Ability1Description);
            ability1Widget.SetAvailable(idol.CanAbility1());
            idol.OnAbility1AvailabilityChange += ability1Widget.SetAvailable;
            idol.OnAbility1CooldownChange += ability1Widget.SetCooldownRemaining;

            ability2Widget.gameObject.SetActive(true);
            ability2Widget.SetIcon(idol.Ability2Icon);
            ability2Widget.SetInfo(idol.Ability2Name, idol.Ability2Description);
            ability2Widget.SetAvailable(idol.CanAbility2());
            idol.OnAbility2AvailabilityChange += ability2Widget.SetAvailable;
            idol.OnAbility2CooldownChange += ability2Widget.SetCooldownRemaining;

            ultimateWidget.gameObject.SetActive(true);
            ultimateWidget.SetIcon(idol.UltimateAbilityIcon);
            ultimateWidget.SetInfo(idol.UltimateAbilityName, idol.UltimateAbilityDescription);
            ultimateWidget.SetAvailable(idol.CanUltimate());
            idol.OnUltimateAvailabilityChange += ultimateWidget.SetAvailable;
            idol.OnUltimateAbilityCooldownChange += ultimateWidget.SetCooldownRemaining;
        }
        else
        {
            ability1Widget.gameObject.SetActive(false);
            ability2Widget.gameObject.SetActive(false);
            ultimateWidget.gameObject.SetActive(false);
        }

        newEntity.SetInGUI(openMenuStack.Count > 0);
    }


    public Player GetOwningPlayer()
    {
        return trackedPlayer;
    }


    /// <summary>
    /// Shows a spoken line in the dialogue menu. Use this to open the dialogue menu.
    /// </summary>
    /// <param name="speaker">Who is speaking</param>
    /// <param name="text">What they are saying</param>
    /// <param name="responseData">Response indices and response text</param>
    public void ShowDialogueTopic(DialogueAgent speaker, List<string> text, Dictionary<int, string> responseData)
    {
        if (!dialogueMenu.IsOpen())
        {
            OpenAndPushMenu(dialogueMenu);
        }

        dialogueMenu.SetDialogueTarget(speaker);
        dialogueMenu.SetDisplayedText(text[0]);

        if (text.Count > 1)
        {
            // Multiple lines in the topic being spoken, queue the remainder up
            dialogueMenu.SetQueuedText(text.GetRange(1, text.Count - 1));
        }

        dialogueMenu.SetResponseData(responseData);
    }

    /// <summary>
    /// Close the dialogue menu.
    /// </summary>
    public void CloseDialogueMenu()
    {
        CloseMenu(dialogueMenu);
    }

    // public void OpenJournalMenu()
    // {
    //     //OpenAndPushMenu(journalMenu);
    // }

    // public void OpenBackpackMenu()
    // {
    //     OpenAndPushMenu(backpackMenu);

    // }

    public void OpenPauseMenu()
    {
        OpenAndPushMenu(pauseMenu);
    }

    public void OpenSettingsMenu()
    {
        OpenAndPushMenu(settingsMenu);
    }

    [Command]
    public void CmdOpenLoadoutMenu()
    {
        if (!AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            TargetOpenLoadoutMenu();
        }
    }

    [TargetRpc]
    public void TargetOpenLoadoutMenu()
    {
        OpenAndPushMenu(loadoutSelectionMenu);
    }

    /// <summary>
    /// Server-only
    /// </summary>
    [Server]
    public void OpenSequenceStartMenu()
    {
        TargetOpenSequenceStartMenu();
    }

    [TargetRpc]
    void TargetOpenSequenceStartMenu()
    {
        OpenAndPushMenu(sequenceStartMenu);
    }

    // [TargetRpc]
    // public void TargetOpenBlacksmithMenu()
    // {
    //     OpenAndPushMenu(blacksmithMenu);
    // }


    [TargetRpc]
    public void TargetOpenShopMenu(Shopkeeper shopkeeper, ShopOfferingInfo[] offerings)
    {
        OpenAndPushMenu(shopMenu);

        shopMenu.SetShopkeeper(shopkeeper);
        shopMenu.SetOfferings(offerings);
    }


    [Command]
    public void CmdOpenTraitSelectionMenu()
    {
        if (GetOwningPlayer().pendingTraitOptions.Count == 0)
        {
            if (GetOwningPlayer().traitsOwed > 0)
            {
                // Traits owed, create options
                GetOwningPlayer().CreatePendingTraitOption();
            }
            else
            {
                // No pending, none owed, do nothing
                return;
            }
        }

        TraitInfo[] infos = GetOwningPlayer().pendingTraitOptions.ToArray();

        TargetOpenTraitSelectionMenu(infos);
    }

    [TargetRpc]
    public void TargetOpenTraitSelectionMenu(TraitInfo[] traits)
    {
        Debug.Log("target open traits");
        traitSelectionMenu.SetTraits(traits);
        //OpenAndPushMenu(traitSelectionMenu);
    }

    public void ShowBossHealthBar(Boss boss)
    {
        if (boss == null)
        {
            StartCoroutine(nameof(ShowBossHealthBarDelayed));
        }
        else
        {
            bossHealthBar.gameObject.SetActive(true);
            bossHealthBar.SetBoss(boss);
        }
    }

    IEnumerable ShowBossHealthBarDelayed()
    {
        while (Boss.FindBoss() == null)
        {
            yield return null;
        }


        bossHealthBar.gameObject.SetActive(true);
        bossHealthBar.SetBoss(Boss.FindBoss());
    }

    public void ClearBossHealthBar(Boss boss)
    {
        bossHealthBar.gameObject.SetActive(false);
    }

    public void ShowReticle()
    {
        reticle.gameObject.SetActive(true);
    }

    public void HideReticle()
    {
        reticle.gameObject.SetActive(false);
    }

    public Reticle GetReticle()
    {
        return reticle;
    }

    // public void LockOn(Entity lockOnTarget)
    // {
    //     lockOnTargetIndicator.SetWorldPositionTransform(lockOnTarget.transform);
    //     lockOnTargetIndicator.Show();
    // }

    // public void Unlock()
    // {
    //     lockOnTargetIndicator.Hide();
    //     lockOnTargetIndicator.SetWorldPositionTransform(null);
    // }

    void CloseMenu(Menu menu)
    {
        if (menu.IsOpen())
        {
            menu.Close();
        }

        UpdateMenuStack();
    }

    // Push a new menu onto the menu stack
    public void OpenAndPushMenu(Menu menu)
    {
        menu.Open();
        openMenuStack.Push(menu);

        if (InputManager.inputScheme == InputScheme.MouseAndKeyboard)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (playerEntity != null)
        {
            playerEntity.SetInGUI(true);
        }
    }

    // Pops the most recent menu
    void PopMenu()
    {
        // Close most recently opened menu. Loop until we find it because some of our menus may already be closed
        Menu top;
        do
        {
            top = openMenuStack.Pop();

            if (top.IsOpen())
            {
                top.Close();
            }
        }
        while (openMenuStack.Count > 0 && !openMenuStack.Peek().IsOpen());

        // Return normal controls if no menus open
        if (openMenuStack.Count == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(nameof(ClearPlayerGUIStatus));
        }
    }

    public void UpdateMenuStack()
    {
        Stack<Menu> newStack = new();
        foreach (Menu menu in openMenuStack)
        {
            if (menu.IsOpen())
            {
                newStack.Push(menu);
            }
        }

        openMenuStack = newStack;


        // Return normal controls if no menus open
        if (openMenuStack.Count == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (playerEntity != null)
            {
                StartCoroutine(nameof(ClearPlayerGUIStatus));
            }
        }
    }

    void OnDialougeAgentChatter(DialogueAgent dialogueAgent, string chatter)
    {
        ChatBubble chatBubble;
        if (!npcChatBubbles.ContainsKey(dialogueAgent))
        {
            chatBubble = Instantiate(npcChatterBubblePrefab, transform);
            chatBubble.SetOwner(dialogueAgent);

            npcChatBubbles.Add(dialogueAgent, chatBubble);
        }
        else
        {
            chatBubble = npcChatBubbles[dialogueAgent];
        }

        chatBubble.SetText(chatter);
    }

    void OnDialogueAgentChatterClear(DialogueAgent dialogueAgent)
    {
        if (npcChatBubbles.ContainsKey(dialogueAgent))
        {
            Destroy(npcChatBubbles[dialogueAgent].gameObject);
            npcChatBubbles.Remove(dialogueAgent);
        }
    }

    [Client]
    void FadeToBlack(bool faded, float lerpStrength = 1.0F)
    {
        fadedToBlack = faded;
        fadeToBlackLerpStrength = lerpStrength;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public abstract class IdolForm : ControlledEntity
{
    [Server]
    public static IdolForm CreateIdolForm(Player owningPlayer, IdolForm idolPrefab, Vector3 worldPosition, Quaternion worldRotation)
    {
        IdolForm inst = Instantiate(idolPrefab, worldPosition, worldRotation);
        NetworkServer.Spawn(inst.gameObject, owningPlayer.connectionToClient);

        return inst;
    }

    /*
    [field:Header("Passive")]
    [field:SerializeField] public string PassiveName {get; internal set;}
    [field:SerializeField] [field:TextArea(5,3)] public string PassiveDescription {get; internal set;}
    */

    [field: SerializeField] public IdolItemData Data { get; internal set; }

    [SerializeField] float deathTimeout = 60.0F;


    [field: Header("Ability 1")]
    [field: SerializeField] public string Ability1Name { get; internal set; }
    [field: SerializeField] public Sprite Ability1Icon { get; internal set; }
    [field: SerializeField] public int Ability1Cost { get; internal set; }
    [field: SerializeField] public float Ability1Cooldown { get; internal set; }
    [field: SerializeField][field: TextArea(5, 3)] public string Ability1Description { get; internal set; }


    [field: Header("Ability 2")]
    [field: SerializeField] public string Ability2Name { get; internal set; }
    [field: SerializeField] public Sprite Ability2Icon { get; internal set; }
    [field: SerializeField] public int Ability2Cost { get; internal set; }
    [field: SerializeField] public float Ability2Cooldown { get; internal set; }
    [field: SerializeField][field: TextArea(5, 3)] public string Ability2Description { get; internal set; }


    [field: Header("Ultimate Ability")]
    [field: SerializeField] public string UltimateAbilityName { get; internal set; }
    [field: SerializeField] public Sprite UltimateAbilityIcon { get; internal set; }
    [field: SerializeField] public int UltimateAbilityCost { get; internal set; }
    [field: SerializeField] public float UltimateAbilityCooldown { get; internal set; }
    [field: SerializeField][field: TextArea(5, 3)] public string UltimateAbilityDescription { get; internal set; }


    public Action<bool> OnAbility1AvailabilityChange;
    public Action<bool> OnAbility2AvailabilityChange;
    public Action<bool> OnUltimateAvailabilityChange;

    public Action<float> OnAbility1CooldownChange;
    public Action<float> OnAbility2CooldownChange;
    public Action<float> OnUltimateAbilityCooldownChange;


    // Commands to trigger Idol abilities
    [Command] void CmdAbility1() { Ability1(); }
    [Command] void CmdAbility2() { Ability2(); }
    [Command] void CmdUltimate() { UltimateAbility(); }

    [Command] void CmdReleaseAbility1() { ReleaseAbility1(); }
    [Command] void CmdReleaseAbility2() { ReleaseAbility2(); }
    [Command] void CmdReleaseUltimateAbility() { ReleaseUltimateAbility(); }


    public virtual bool CanAbility1() { return GetStat(Stats.CurrentEnergy) >= Ability1Cost && ability1CooldownRemaining <= 0; }
    public virtual bool CanAbility2() { return GetStat(Stats.CurrentEnergy) >= Ability2Cost && ability2CooldownRemaining <= 0; }
    public virtual bool CanUltimate() { return GetStat(Stats.CurrentEnergy) >= UltimateAbilityCost && ultimateAbilityCooldownRemaining <= 0; }

    [Server] protected virtual void Ability1() { }
    [Server] protected virtual void Ability2() { }
    [Server] protected virtual void UltimateAbility() { }
    [Server] protected virtual void ReleaseAbility1() { }
    [Server] protected virtual void ReleaseAbility2() { }
    [Server] protected virtual void ReleaseUltimateAbility() { }


    // Call these from Idol-overridden ability functions, to begin ability cooldowns as individually appropriate
    [Server]
    protected void CastedAbility1()
    {
        SetStat(Stats.CurrentEnergy, GetStat(Stats.CurrentEnergy) - Ability1Cost);
        ability1CooldownRemaining = Ability1Cooldown * GetStat(Stats.AbilityCooldownRatio, 1.0F);

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnAbility(this, Ability1Cost);
        }

    }

    [Server]
    protected void CastedAbility2()
    {
        SetStat(Stats.CurrentEnergy, GetStat(Stats.CurrentEnergy) - Ability2Cost);
        ability2CooldownRemaining = Ability2Cooldown * GetStat(Stats.AbilityCooldownRatio, 1.0F);

        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnAbility(this, Ability2Cost);
        }
    }

    [Server]
    protected void CastedUltimateAbility()
    {
        SetStat(Stats.CurrentEnergy, GetStat(Stats.CurrentEnergy) - UltimateAbilityCost);
        ultimateAbilityCooldownRemaining = UltimateAbilityCooldown * GetStat(Stats.AbilityCooldownRatio, 1.0F);

        Debug.Log(traitList.ToProcOrderList().Count);
        foreach (Trait trait in GetTraits().ToProcOrderList())
        {
            trait.OnAbility(this, UltimateAbilityCost);
        }
    }


    [SyncVar] float currentDeathTimeout = 0.0F;

    [SyncVar(hook = nameof(Ability1CooldownChanged))] float ability1CooldownRemaining = 0;
    [SyncVar(hook = nameof(Ability2CooldownChanged))] float ability2CooldownRemaining = 0;
    [SyncVar(hook = nameof(UtlimateAbilityCooldownChanged))] float ultimateAbilityCooldownRemaining = 0;


    public override bool HasSecondaryResource()
    {
        return true;
    }

    public override Color GetSecondaryResourceColor()
    {
        return ColorPalette.GetSecondaryColorForElement(Data.GetElement());
    }

    public override void Start()
    {
        base.Start();
    }


    // currently called by Player script on update
    public override void ProcessPeriodics()
    {
        if (isServer)
        {
            if (this != owningPlayer.GetControlledEntity())
            {
                if (currentDeathTimeout > 0.0F)
                {
                    // timeout regen
                    SetStat(Stats.CurrentHealth, GetMaxHealth() * ((deathTimeout - currentDeathTimeout) / deathTimeout));
                    currentDeathTimeout -= Time.deltaTime;
                }
                else
                {
                    ProcessHealthRegen();
                }

                if (GetStat(Stats.CurrentHealth) > GetStat(Stats.MaxHealth))
                {
                    SetStat(Stats.CurrentHealth, GetStat(Stats.MaxHealth));
                }
            }

            SetStat(Stats.CurrentEnergy, GetStat(Stats.CurrentEnergy) + (GetStat(Stats.EnergyRegen) * Time.deltaTime));
            if (GetStat(Stats.CurrentEnergy) > GetStat(Stats.MaxEnergy))
            {
                SetStat(Stats.CurrentEnergy, GetStat(Stats.MaxEnergy));
            }
            else if (GetStat(Stats.CurrentEnergy) < 0)
            {
                SetStat(Stats.CurrentEnergy, 0);
            }

            // Process cooldowns
            if (ability1CooldownRemaining > 0) ability1CooldownRemaining = Mathf.Clamp(ability1CooldownRemaining - Time.deltaTime, 0, Mathf.Infinity);
            if (ability2CooldownRemaining > 0) ability2CooldownRemaining = Mathf.Clamp(ability2CooldownRemaining - Time.deltaTime, 0, Mathf.Infinity);
            if (ultimateAbilityCooldownRemaining > 0) ultimateAbilityCooldownRemaining = Mathf.Clamp(ultimateAbilityCooldownRemaining - Time.deltaTime, 0, Mathf.Infinity);

        }


        OnAbility1AvailabilityChange?.Invoke(CanAbility1());

        OnAbility2AvailabilityChange?.Invoke(CanAbility2());

        OnUltimateAvailabilityChange?.Invoke(CanUltimate());
    }


    public override void ProcessInput()
    {
        base.ProcessInput();

        PlayerInputData inputData = PlayerInput.Input;

        if (inputData != null && !inGUI && !IsStunned())
        {
            if (inputData.ability1)
            {
                CmdAbility1();
            }

            if (inputData.releaseAbility1)
            {
                CmdReleaseAbility1();
            }

            if (inputData.ability2)
            {
                CmdAbility2();
            }

            if (inputData.releaseAbility2)
            {
                CmdReleaseAbility2();
            }

            if (inputData.ultimateAbility)
            {
                CmdUltimate();
            }

            if (inputData.releaseUltimateAbility)
            {
                CmdReleaseUltimateAbility();
            }
        }
    }

    [Command]
    protected override void CmdTransform()
    {
        base.CmdTransform();
        GetOwningPlayer().TransformIntoWraith();
    }

    [ServerCallback]
    public override void TeardownEntity()
    {
        GetOwningPlayer().TransformIntoWraith();

        currentDeathTimeout = deathTimeout;

        isDead = false; // reset isDead, because the Idol needs to be reused
    }

    public float GetDeathTimeout()
    {
        return currentDeathTimeout / 30.0F;
    }

    public void SetDeathTimeout(float timeout)
    {
        currentDeathTimeout = timeout;
    }

    void Ability1CooldownChanged(float previous, float current)
    {
        OnAbility1CooldownChange?.Invoke(current);
    }

    void Ability2CooldownChanged(float previous, float current)
    {
        OnAbility2CooldownChange?.Invoke(current);
    }

    void UtlimateAbilityCooldownChanged(float previous, float current)
    {
        OnUltimateAbilityCooldownChange?.Invoke(current);
    }

    public override float GetAimAssistMaxDistance()
    {
        return MELEE_AIM_ASSIST_MAX_DEFAULT; // Most Idols are melee
    }

    public override float GetHealthOrbsOnKillMult()
    {
        return MELEE_HEALTH_ORBS_ON_KILL_MULT;
    }

    public override void FootstepSound()
    {
        AudioManager.Singleton.PlayOneShot(AetherdaleData.GetAetherdaleData().soundData.footsteps.dirtMedium, transform.position);
    }
}

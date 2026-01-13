using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public abstract class Boss : StatefulCombatEntity, IOnLocalPlayerReadyTarget
{
    public Dictionary<int, float> playerDifficultyModifiers = new()
    {
        {0, 1.0F},
        {1, 1.0F},
        {2, 2.5F},
        {3, 4.0F},
        {4, 6.0F}
    };

    [SerializeField] BossName bossName;

    [SerializeField] List<BossPhase> phases;

    protected BossPhase currentPhase;
    
    List<Player> playersInEncounter = new(); // players in combat with this instance of a boss
    protected int currentPhaseIndex = 0;

    public delegate void BossAction(Boss boss);
    public event BossAction OnBossDeath;

    public static Action<Boss> OnBossEncounterStart;
    public static Action<Boss> OnBossEncounterEnd;

    public static Boss FindBoss()
    {
        Boss[] bosses = FindObjectsByType<Boss>(FindObjectsSortMode.None);
        if (bosses.Length > 0)
        {
            return bosses[0];
        }

        return null;
    }


    public override void Start()
    {
        base.Start();

        if (isServer)
        {
            if (phases != null && phases.Count > 0)
            {
                ChangePhase(0);
            }
            OnStatChanged += StatChanged;
        }
    }

    
    public void OnLocalPlayerReady(Player player)
    {
        if (aiEnabled)
        {
            OnBossEncounterStart?.Invoke(this);
        }
    }


    public void SetPlayerNumberScaling(int numPlayers)
    {
        float playerMod;
        if (numPlayers > 4)
        {
            playerMod = playerDifficultyModifiers[4];
        }
        else
        {
            playerMod = playerDifficultyModifiers[numPlayers];
        }
        
        float healthPerc = GetHealthRatio();

        SetStat(Stats.MaxHealth, (int) (baseHealth * playerMod));
        SetStat(Stats.CurrentHealth, (int) (GetStat(Stats.MaxHealth) * healthPerc));
    }

    public void SetAIEnabled(bool aiEnabled)
    {
        this.aiEnabled = aiEnabled;

        if (aiEnabled)
        {
            OnBossEncounterStart?.Invoke(this);
        }
    }

    public override void Move(Vector3 magnitude)
    {
        navMeshAgent.Warp(transform.position + magnitude);
    }

    public override Vector3 GetVelocity()
    {
        return navMeshAgent.velocity;
    }

    [Server]
    public void StatChanged(string statName, float value)
    {
        if (statName == "CurrentHealth" || statName == "MaxHealth")
        {
            float percentHealth = GetHealthRatio() * 100;
            int nextPhaseIndex = currentPhaseIndex + 1;

            if (phases.Count > nextPhaseIndex)
            {
                BossPhase nextPhase = phases[nextPhaseIndex];

                if (percentHealth <= nextPhase.startLifePercent)
                {
                    ChangePhase(nextPhaseIndex);
                }
            }
        }
    }

    [Server]
    protected void ChangePhase(int nextPhaseIndex)
    {
        currentPhase = phases[nextPhaseIndex];
        currentPhaseIndex = nextPhaseIndex;

        currentPhase.OnEnter();
    }


    public override void Die()
    {
        OnBossDeath?.Invoke(this);

        OnBossEncounterEnd?.Invoke(this);

        base.Die();
    }

    public BossName GetBossName()
    {
        return bossName;
    }

    [System.Serializable]
    protected class BossPhase
    {
        public string phase;
        public int startLifePercent;
        
        public void OnEnter()
        {
        }

    }
}


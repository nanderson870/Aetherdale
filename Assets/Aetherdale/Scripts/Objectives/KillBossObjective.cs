using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillBossObjective : Objective
{
    BossName bossName;

    public KillBossObjective(KillBossObjectiveData objectiveData) : base(objectiveData)
    {
        bossName = objectiveData.GetBossName();
    }

    public KillBossObjective(BossName bossName) : base($"Defeat {bossName}", 1)
    {
        this.bossName = bossName;
    }

    public void ProgressObjective(Boss boss)
    {
        if (boss.GetBossName() == bossName)
        {
            ProgressObjective();
        }
    }

    public override void RegisterCallbacks(Player owningPlayer)
    {
    }
    
    public override void UnregisterCallbacks(Player owningPlayer)
    {
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Kill Boss Objective", menuName = "Aetherdale/Objective Data/Kill Boss", order = 0)]
public class KillBossObjectiveData : ObjectiveData
{
    [Header("Required Data")]
    [SerializeField] BossName boss;

    public override Objective GetInstance()
    {
        return new KillBossObjective(this);
    }

    public BossName GetBossName()
    {
        return boss;
    }
}

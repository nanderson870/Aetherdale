using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum HitResult
{
    Missed=0,
    Hit=1,
    Blocked=2,
    Absorbed=3,
    Dodged=4,
    Healed=5,
}

public class HitInfo
{
    public Entity entityHit = null;
    public Entity damageDealer = null;
    public HitResult hitResult = HitResult.Missed;
    public HitType hitType = HitType.None;
    public bool criticalHit = false;
    public bool statusProc = false;
    public int damageDealt = 0;
    public int premitigationDamage = 0;
    public Element damageType = Element.Physical;
    public bool killedTarget = false;
    public bool staggeredTarget = false;
    public bool firstHitOnTarget = false;
    public Vector3 hitPosition = new();
    public int originEffectInstanceId = -1;

}

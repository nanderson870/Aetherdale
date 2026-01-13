

using UnityEngine;

public interface IWeaponBehaviourWielder
{
    GameObject gameObject { get; }
    Transform transform { get; }
    Transform weaponTransform { get; }
    bool sprinting { get; }
    public int GetAttackDamage();
    public Vector3 GetAimedPosition();

    public void Attack(Entity target = null);

    public void EquipWeapon(WeaponData weaponData, bool dropPrevious = false);
    public void EquipWeaponBehaviour(WeaponBehaviour weaponBehaviour, bool dropPrevious);
    public WeaponData GetEquippedWeaponData();

    void ActivateRig(string v);
    void DeactivateRig(string v);

    public float GetStat(string statName, float defaultValue = 0);
    void RpcSetAnimatorBool(string v1, bool v2);

    void TargetEnterAimMode(bool includeEntities);
    void TargetExitAimMode();

    void PlayAnimation(string name, float normalizedTransitionDuration);

    public void StartWeaponHit(int damage, Element damageType, HitType hitType, int impact);
    public void EndWeaponHit();
}
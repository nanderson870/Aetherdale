using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Aetherdale/Item Data/Weapon", order = 0)]
public class WeaponData : ItemData, IShopOffering
{    
    public static readonly LinearEquation WEAPON_SHOP_COST = new LinearEquation(20, 30.0F);
    public static readonly PolynomialEquation WEAPON_BASE_DAMAGE = new(0.05F, 1.3F, 0.33F, 12.0F); // Weapon base damage
    public static readonly PolynomialEquation WEAPON_RARITY_BONUS_DAMAGE = new(0.05F, 1.0F, 0, 1.0F); // Weapon bonus per rarity
    public static readonly LinearEquation WEAPON_AETHER_COST = new(0.33F, 0); // Base weapon cost, input is level
    public static readonly LinearEquation WEAPON_UPGRADE_AETHER_COST_MULT = new(0.5F, 1); // Upgrade cost mult, input is tier


    // 1h sword is 1.0, other weapons balanced around 1h sword
    const float DAMAGE_MULT_1H_SWORD = 1F;
    const float DAMAGE_MULT_1H_AXE = 1.5F;
    const float DAMAGE_MULT_2H_SWORD = 2.5F;
    const float DAMAGE_MULT_SPEAR = 1.25F;
    const float DAMAGE_MULT_CROSSBOW = 0.75F;

    const float ATTACK_SPEED_1H_SWORD = 1.3F; // mult on animation
    const float ATTACK_SPEED_1H_AXE = 1.0F;
    const float ATTACK_SPEED_2H_SWORD = 0.75F;
    const float ATTACK_SPEED_SPEAR = 1.75F;
    const float ATTACK_SPEED_CROSSBOW = 2.0F;


    const int IMPACT_1H_SWORD = 50;
    const int IMPACT_1H_AXE = 150;
    const int IMPACT_2H_SWORD = 100;
    const int IMPACT_CROSSBOW = 50;


    const float RANGE_1H_SWORD = 4F;
    const float RANGE_1H_AXE = 3F;
    const float RANGE_2H_SWORD = 6F;
    const float RANGE_SPEAR = 8F;

    [SerializeField] GameObject heldWeapon;
    [SerializeField] int weaponLevel = 1;
    [SerializeField] bool selectableFromHub = false;
    [SerializeField] bool appearsByLevel = true;
    [SerializeField] WeaponType weaponType;
    [SerializeField] Rarity baseRarity;
    [SerializeField] float attackSpeed = 0;
    [SerializeField] float range = 0;
    [SerializeField] Element damageType;
    [SerializeField] List<SplitDamageType> additionalDamage;
    [SerializeField] List<Effect> appliedEffects = new();
    [SerializeField] Projectile projectile;

    [System.Serializable]
    public class SplitDamageType
    {
        public Element element;
        public float portionOfOriginal;
    }
    

    #if UNITY_EDITOR
    [Header("Data Preview Stats")]
    public int baseDamageEditorOnly;
    public string forgeCurrencyCostEditorOnly;
    public string forgePrimaryCostEditorOnly;
    #endif

    public override void OnValidate()
    {
        base.OnValidate();

        #if UNITY_EDITOR
            baseDamageEditorOnly = GetDamage(baseRarity);
        #endif
    }

    public override GameObject GetMesh()
    {
        return heldWeapon;
    }

    public override Pickup CreatePickup(Vector3 position, Quaternion rotation)
    {
        WeaponBehaviourPickupWrapper wbpw = Instantiate(AetherdaleData.GetAetherdaleData().weaponBehaviourPickupWrapperPrefab, position, rotation);

        WeaponBehaviour weaponBehaviour = Instantiate(heldWeapon.GetComponent<WeaponBehaviour>(), wbpw.transform);
        wbpw.weaponBehaviour = weaponBehaviour;

        NetworkServer.Spawn(wbpw.gameObject);
        NetworkServer.Spawn(weaponBehaviour.gameObject);

        return wbpw;
    }

    
    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return "Weapon stat descriptions currently being re-worked";
        // return string.Join(Environment.NewLine,
        //     $"Level {data.GetWeaponLevel()} {data.GetWeaponTypeString()}",
        //     $"",
        //     $"{GetDamage()} {GetDamageType()} damage"
        // );
    }


    //[Obsolete("Add out parameter for additional damage types")]
    public int GetDamage(Rarity rarity)
    {
        int baseDamage = CalculateBaseDamage();

        int rarityBonus = CalculateRarityBonusDamage(rarity);

        return baseDamage + rarityBonus;
    }
    

    public int GetDamage(Rarity rarity, out Dictionary<Element, int> additionalDamageTypes)
    {
        int baseDamage = CalculateBaseDamage();

        int rarityBonus = CalculateRarityBonusDamage(rarity);

        int total = baseDamage + rarityBonus;

        additionalDamageTypes = new();
        foreach (SplitDamageType split in additionalDamage)
        {
            additionalDamageTypes[split.element] = (int)(total * split.portionOfOriginal);
        }

        return total;
    }

    public Element GetDamageType()
    {
        return damageType;
    }

    public float GetAttackSpeed()
    {
        if (attackSpeed > 0)
        {
            return attackSpeed;
        }

        // Else default to the weapon type speed
        return weaponType switch
        {
            WeaponType.Axe1H => ATTACK_SPEED_1H_AXE,
            WeaponType.Sword2H => 1.0F,//ATTACK_SPEED_2H_SWORD,
            WeaponType.Sword1H => 2.0F,//ATTACK_SPEED_1H_SWORD,
            WeaponType.Spear => 1.0F,//ATTACK_SPEED_SPEAR,
            WeaponType.Crossbow => ATTACK_SPEED_CROSSBOW,
            _ => 1.0F,
        };
    }

    public float GetRange()
    {
        if (range <= 0)
        {
            return GetDefaultRange();
        }
        else
        {
            return range;
        }
    }

    float GetDefaultRange()
    {
        return weaponType switch
        {
            WeaponType.Axe1H =>    RANGE_1H_AXE,
            WeaponType.Sword2H =>  RANGE_2H_SWORD,
            WeaponType.Sword1H =>  RANGE_1H_SWORD,
            WeaponType.Spear =>    RANGE_SPEAR,
            _ => 0F,
        };
    }

    public int CalculateBaseDamage()
    {
        // Take base damage for level of the item first
        float baseDamage = WEAPON_BASE_DAMAGE.Calculate(weaponLevel);

        baseDamage = AdjustDamageForWeaponType(baseDamage);

        return (int) baseDamage;
    }
    
    public int CalculateRarityBonusDamage(Rarity rarity)
    {
        float bonusDamagePerRarity = WEAPON_RARITY_BONUS_DAMAGE.Calculate(weaponLevel);

        bonusDamagePerRarity = AdjustDamageForWeaponType(bonusDamagePerRarity);

        return (int) (bonusDamagePerRarity * (int) rarity);
    }

    public int GetImpact()
    {
        return weaponType switch
        {
            WeaponType.Axe1H => IMPACT_1H_AXE,
            WeaponType.Sword2H => IMPACT_2H_SWORD,
            WeaponType.Sword1H => IMPACT_1H_SWORD,
            WeaponType.Crossbow => IMPACT_CROSSBOW,
            _ => 100,
        };
    }

    public int GetWeaponLevel()
    {
        return weaponLevel;
    }

    public bool SelectableFromHub()
    {
        return selectableFromHub;
    }

    public bool AppearsByLevel()
    {
        return appearsByLevel;
    }

    public WeaponType GetWeaponType()
    {
        return weaponType;
    }

    public override Sprite GetIcon()
    {
        if (icon == null)
        {
            switch (weaponType)
            {
                case WeaponType.Axe1H:
                    return AetherdaleData.GetAetherdaleData().defaultAxeIcon;

                case WeaponType.Sword1H:
                    return AetherdaleData.GetAetherdaleData().defaultSwordIcon;
                case WeaponType.Spear:
                    return AetherdaleData.GetAetherdaleData().defaultSpearIcon;
                case WeaponType.Crossbow:
                    return AetherdaleData.GetAetherdaleData().defaultCrossbowIcon;

            }

        }

        return icon;
    }

    public string GetWeaponTypeString()
    {
        switch (weaponType)
        {
            case WeaponType.Sword1H:
                return "Sword";

            case WeaponType.Sword2H:
                return "Greatsword";

            case WeaponType.Spear:
                return "Spear";
                
            case WeaponType.Axe1H:
                return "Axe";

            case WeaponType.Crossbow:
                return "Crossbow";

            default:
                return "Weapon";
        }
    }

    float AdjustDamageForWeaponType(float initialDamage)
    {
        // Determine it's adjusted value based on the weapon type
        switch (weaponType)
        {
            case WeaponType.Sword1H:
                initialDamage *= DAMAGE_MULT_1H_SWORD;
                break;

            case WeaponType.Axe1H:
                initialDamage *= DAMAGE_MULT_1H_AXE;
                break;

            case WeaponType.Sword2H:
                initialDamage *= DAMAGE_MULT_2H_SWORD;
                break;

            case WeaponType.Spear:
                initialDamage *= DAMAGE_MULT_SPEAR;
                break;
            
            case WeaponType.Crossbow:
                initialDamage *= DAMAGE_MULT_CROSSBOW;
                break;

            default:
                // No multiplier (1h sword, or undefined)
                break;
        }

        return initialDamage;
    }

    
    public float GetAttackMovespeedMult()
    {
        // Determine it's adjusted value based on the weapon type
        switch (weaponType)
        {
            case WeaponType.Sword1H:
                return 0.6F;

            case WeaponType.Axe1H:
                return 0.4F;

            case WeaponType.Sword2H:
                return 0.6F;

            case WeaponType.Spear:
                return 0.6F;
            
            case WeaponType.Crossbow:
                return 1.0F;

            default:
                return 0.6F;
        }
    }

    public int CalculateForgingAetherCost()
    {
        int cost = (int) WEAPON_AETHER_COST.Calculate(weaponLevel);
        if (cost < 1)
        {
            cost = 1;
        }

        return cost;
    }

    public int CalculateUpgradeAetherCost(Rarity rarityTier)
    {
        return (int) (CalculateForgingAetherCost() * WEAPON_UPGRADE_AETHER_COST_MULT.Calculate((int) rarityTier));
    }

    public Projectile GetProjectile()
    {
        return projectile;
    }

    public void GiveToPlayer(Player player)
    {
        player.SetSequenceWeapon(this);
    }

    public int GetShopCost()
    {
        int unrounded = (int) WEAPON_SHOP_COST.Calculate(GetWeaponLevel());
        int remainder = unrounded % 5;

        int final = unrounded - remainder;

        return final;
    }

    public bool PlayerMeetsRequirements(Player player)
    {
        return true;
    }

    public ShopOfferingInfo GetInfo()
    {
        return new()
        {
            type = ShopOfferingType.Item,
            name = GetName(),
            typeName = nameof(WeaponData),
            statsDescription = GetStatsDescription(),
            description = GetDescription(),
            goldCost = GetShopCost()
        };;
    }

    public GameObject GetPreviewPrefab()
    {
        return GetMesh();
    }
}


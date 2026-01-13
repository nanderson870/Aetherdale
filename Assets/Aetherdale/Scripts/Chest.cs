using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using FMODUnity;
using System.Linq;

public class Chest : NetworkBehaviour, Damageable
{
    public enum ChestTierName
    {
        Iron,
        Silver,
        Gold,
        Platinum
    }

    [SerializeField] ChestTierName chestTier;
    [SerializeField] int durability = 50;
    [SerializeField] Transform itemSpawnTransform;

    [SerializeField] List<Pickup> consumablePickups = new();

    [SerializeField] EventReference hitSound;
    [SerializeField] EventReference openSound;


    [SerializeField] [Range(1, 100)] int level = 1;

    readonly LinearEquation GOLD_PER_LEVEL = new(8, 20);
    const float GOLD_VARIATION_LIMIT = 0.1F; // gold received can be this ratio lower or higher than 1x

    const float BASE_AETHER_CHANCE = 0; // Percent chance to receive an Aether per chest

    NetworkAnimator networkAnimator;

    [SyncVar] bool opened = false;

    ChestTier tier;

    public static ChestTier GetChestTier(ChestTierName name)
    {
        return name switch
        {
            ChestTierName.Iron => new ChestTierIron(),
            ChestTierName.Silver => new ChestTierSilver(),
            ChestTierName.Gold => new ChestTierGold(),
            ChestTierName.Platinum => new ChestTierPlatinum(),
            _ => throw new System.NotImplementedException($"Invalid Chest Tier Name {name}"),
        };
    }

    public void Start()
    {
        networkAnimator = GetComponent<NetworkAnimator>();

        tier = GetChestTier(chestTier);
    }


    // Either this or DropWeapon's philosophy must be eliminated and merged into the other
    void DropItem(ItemData itemData, int quantity)
    {
        Vector3 pos, velocity;
        GetDropPositionAndVelocity(out pos, out velocity);
        
        LootItem.DropLootItems(itemData, quantity, pos, initialVelocity:velocity);
    }

    void DropWeapon(WeaponData weaponData)
    {
        Vector3 pos, velocity;
        GetDropPositionAndVelocity(out pos, out velocity);

        Pickup pickup = Pickup.Create(weaponData, pos, 1, true);

        pickup.GetComponent<Rigidbody>().linearVelocity = velocity;

    } 

    void DropPickup(Pickup pickup, int quantity)
    {
        Vector3 pos, velocity;
        GetDropPositionAndVelocity(out pos, out velocity);

        Pickup pickupInstance = Pickup.Create(pickup, pos, quantity, true);

        pickupInstance.GetComponent<Rigidbody>().linearVelocity = velocity;
    }

    private void GetDropPositionAndVelocity(out Vector3 pos, out Vector3 velocity)
    {
        Vector2 horizOffset = UnityEngine.Random.insideUnitCircle;
        horizOffset.y = Mathf.Abs(horizOffset.y) + 0.1F;
        horizOffset.y = Mathf.Clamp(horizOffset.y, -.9F, .9F);

        pos = itemSpawnTransform.position + new Vector3(horizOffset.x, 0, horizOffset.y);
        float verticalVelocity = UnityEngine.Random.Range(1F, 7F);
        velocity = itemSpawnTransform.TransformVector(new(horizOffset.x, verticalVelocity, horizOffset.y));
    }

    int GetAmountOfGold()
    {
        int baseGold = (int) GOLD_PER_LEVEL.Calculate(level);

        return (int) UnityEngine.Random.Range(
            baseGold * (1 - GOLD_VARIATION_LIMIT),
            baseGold * (1 + GOLD_VARIATION_LIMIT)
        );
    }

    public void Open()
    {
        opened = true;

        networkAnimator.SetTrigger("Open");

        DropItem(AetherdaleData.GetAetherdaleData().goldCoinsItem, GetAmountOfGold());

        foreach (Pickup pickup in tier.GetPickups(this))
        {
            DropPickup(pickup, 1);
        }

        foreach (WeaponData weaponData in tier.GetWeapons(this))
        {
            DropWeapon(weaponData);
        }
    }

    [ClientRpc]
    void RpcOpened()
    {
        if (!openSound.IsNull)
        {
            AudioManager.Singleton.PlayOneShot(openSound, transform.position);
        }

    }

    #region Damageable Interface
    public HitInfo Damage(int damage, Element damageType, HitType hitType, Entity damageDealer = null, int impact = 0, bool forceCritical = false, bool forceStatus = false, int originEffectInstanceId = 0, HitboxHitData hitboxHitData = null, bool allowHitSound = true, bool scaleTick = true)
    {
        if (opened)
        {
            return new();
        }

        RpcDamaged();
        // Deduct durability
        durability -= damage;

        if (durability <= 0)
        {
            Open();
        }
        else
        {
            networkAnimator.SetTrigger("Hit");
        }

        return new();
    }

    [ClientRpc]
    void RpcDamaged()
    {
        //Play noise
        AudioManager.Singleton.PlayOneShot(hitSound, transform.position);
    }

    public Entity GetDamageableEntity()
    {
        return null;
    }

    public int GetDamageablePriority()
    {
        return 0;
    }

    public bool IsInvulnerable()
    {
        return durability <= 0;
    }

    public bool DamageableColliderDistanceMatters()
    {
        return false;
    }

    #endregion

    #region Chest Tier Definitions
    public abstract class ChestTier
    {
        public virtual List<Pickup> GetPickups(Chest chest)
        {
            List<Pickup> pickups = new();
            if (Random.Range(1, 100) <= GetConsumableChance() && chest.consumablePickups.Count > 0)
            {
                pickups.Add(chest.consumablePickups[Random.Range(0, chest.consumablePickups.Count)]);
            }

            if (Random.Range(1, 100) <= GetTraitTomeChance())
            {
                pickups.Add(AetherdaleData.GetAetherdaleData().traitTomePrefab);
            }


            int healingOrbsToDrop = Random.Range(GetMinHealthOrbs(), GetMaxHealthOrbs() + 1);
            for (int i = 0; i < healingOrbsToDrop; i++)
            {
                pickups.Add(AetherdaleData.GetAetherdaleData().healingOrbPickup);
            }

            return pickups;
        }

        public virtual List<WeaponData> GetWeapons(Chest chest)
        {
            List<WeaponData> weapons = new();
            if (Random.Range(1, 100) <= GetWeaponChance())
            {
                List<WeaponData> weaponDatas = ItemManager.GetAllWeapons()
                    .Where(weaponData => weaponData.GetLootItem() != null)
                    .Where(weaponData => (chest.level - weaponData.GetWeaponLevel()) <= 3)
                    .ToList();

                WeaponData weaponData = weaponDatas[Random.Range(0, weaponDatas.Count())];

                weapons.Add(weaponData);
            }

            return weapons;
        }

        public abstract int GetConsumableChance();
        public abstract int GetTraitTomeChance();
        public abstract int GetMinHealthOrbs();
        public abstract int GetMaxHealthOrbs();
        public abstract int GetWeaponChance();
    }

    public class ChestTierIron : ChestTier
    {
        public override int GetConsumableChance() => 15;
        public override int GetTraitTomeChance() => 25;
        public override int GetMinHealthOrbs() => 8;
        public override int GetMaxHealthOrbs() => 16;
        public override int GetWeaponChance() => 0;
    }

    public class ChestTierSilver : ChestTier
    {
        public override int GetConsumableChance() => 50;
        public override int GetTraitTomeChance() => 100;
        public override int GetMinHealthOrbs() => 10;
        public override int GetMaxHealthOrbs() => 18;
        public override int GetWeaponChance() => 50;
    }

    public class ChestTierGold : ChestTier
    {
        public override int GetConsumableChance() => 70;
        public override int GetTraitTomeChance() => 100;
        public override int GetMinHealthOrbs() => 20;
        public override int GetMaxHealthOrbs() => 25;
        public override int GetWeaponChance() => 100;
    }

    public class ChestTierPlatinum : ChestTier
    {
        public override int GetConsumableChance() => 100;
        public override int GetTraitTomeChance() => 100;
        public override int GetMinHealthOrbs() => 36;
        public override int GetMaxHealthOrbs() => 48;
        public override int GetWeaponChance() => 50;
    }

    #endregion
}



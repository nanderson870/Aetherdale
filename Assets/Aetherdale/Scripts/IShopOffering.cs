
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopOffering
{
    public static IShopOffering GetRandomLevelledShopOffering(int level, int weaponWeight = 20, int traitWeight = 20, int consumableWeight = 20)
    {
        const int WEAPON = 0;
        const int TRAIT = 1;
        const int CONSUMABLE = 2;

        List<Tuple<float, int>> offerings = new()
        {
            /* probability, outcome for roulette random */
            new(weaponWeight, WEAPON),
            new(traitWeight, TRAIT),
            new(consumableWeight, CONSUMABLE)
        };

        int roll = Misc.RouletteRandom(offerings);
        return roll switch
        {
            WEAPON => CreateWeaponOffering(level),
            TRAIT => Trait.GetRandomTraitAccountingForRarity(),
            CONSUMABLE => CreateConsumableOffering(level),
            _ => null,
        };
    }

    public static WeaponData CreateWeaponOffering(int itemLevel, int levelRange=5)
    {
        List<WeaponData> potentialWeapons = ItemManager.GetAllWeapons().Where(
            weapon => 
                weapon.AppearsByLevel()
                && weapon.GetWeaponLevel() >= (itemLevel - levelRange) 
                && weapon.GetWeaponLevel() <= (itemLevel + levelRange)
        ).ToList();

        WeaponData chosenWeapon = potentialWeapons[UnityEngine.Random.Range(0, potentialWeapons.Count())];

        return chosenWeapon;
    }

    public static Trait CreateTraitOffering(int level)
    {
        return Trait.GetRandomTraitAccountingForRarityAndRequirements(
            trait =>
            {
                foreach (Player player in Player.GetPlayers())
                {
                    if (!trait.PlayerMeetsRequirements(player))
                    {
                        return false;
                    }
                }

                return true;
            }
        );
    }

    public static Consumable CreateConsumableOffering(int level)
    {
        return Misc.RouletteRandom(
            new List<Tuple<float, Consumable>> ()
            {
                new(10, new BlazeBomb()),
                new(10, new BrindleberryMuffin())
            }
        );
    }

    
    public static IShopOffering ShopOfferingFromInfo(ShopOfferingInfo offering)
    {
        IShopOffering ret;
        if (offering.type == ShopOfferingType.Trait)
        {
            Trait newTrait = (Trait) Activator.CreateInstance(Type.GetType(offering.name.Replace(" ", "")));

            ret = newTrait;
        }
        else if (offering.type == ShopOfferingType.Item)
        {
            ItemData itemData = ItemManager.LookupItemDataByName(offering.name);

            if (itemData is WeaponData weaponData)
            {
                ret = weaponData;
            }
            else 
            {
                ret = new Item(itemData);
            }
        }
        else
        {
            IShopOffering createdOffering = (IShopOffering) Activator.CreateInstance(Type.GetType(offering.typeName));

            ret = createdOffering;
        }

        return ret;
    }

}

public interface IShopOffering
{
    /// <summary>
    /// Used to give this to a player during a run
    /// </summary>
    /// <param name="player"></param>
    public void GiveToPlayer(Player player);
    public Sprite GetIcon();
    public string GetName();
    public string GetStatsDescription(Player targetPlayer = null);
    public string GetDescription();
    public int GetShopCost();
    public bool PlayerMeetsRequirements(Player player);
    public ShopOfferingInfo GetInfo();
    public GameObject GetPreviewPrefab();
    public Rarity GetRarity();
}

public enum ShopOfferingType
{
    Trait = 0,
    Item = 1,
    Consumable = 2,
    Weapon = 3,
    None = 99,
}

/// <summary>
/// For RPC transfer of Shop Offerings
/// </summary>
public class ShopOfferingInfo
{
    public ShopOfferingType type;
    public string name;
    public string typeName;
    public string statsDescription;
    public string description;
    public int goldCost = 9999;
}

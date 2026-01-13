

using UnityEngine;

public class BrindleberryMuffin : Consumable, IShopOffering
{
    public const float MUFFIN_EATING_DURATION = 0.5F;
    public static readonly LinearEquation SHOP_COST = new (10.0F, 40);

    float eatStartTime;

    public override string GetDescription()
    {
        return "A freshly baked muffin with orange berries. It's sweet and filling, but has a mildly bitter aftertaste.";
    }

    public override Sprite GetIcon()
    {
        return AetherdaleData.GetAetherdaleData().brindleberryMuffinIcon;
    }

    public override string GetName()
    {
        return "Brindleberry Muffin";
    }

    public override int GetShopCost()
    {
        return (int) SHOP_COST.Calculate(AreaSequencer.GetAreaSequencer().GetNextAreaLevel());
    }

    public override ShopOfferingInfo GetInfo()
    {
        return new()
        {
            type = ShopOfferingType.Consumable,
            name = GetName(),
            typeName = nameof(BrindleberryMuffin),
            statsDescription = GetStatsDescription(),
            description = GetDescription(),
            goldCost = GetShopCost()
        };
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return "Restores you, and your Idol form, to full health.";
    }

    public override void GiveToPlayer(Player player)
    {
        player.GetInventory().DefensiveConsumableSlot.SetConsumable(this);
    }

    public override void Use(PlayerWraith playerWraith)
    {
        eatStartTime = Time.time;

        playerWraith.RpcSetAnimatorBool("eating", true);

        playerWraith.RpcHideHeldWeapon();
        playerWraith.RpcHoldBrindleberryMuffin();
    }

    public override void Update(PlayerWraith playerWraith)
    {
        if (Time.time - eatStartTime >= MUFFIN_EATING_DURATION)
        {
            playerWraith.RpcSetAnimatorBool("eating", false);
            
            playerWraith.RpcRemoveHeldObject();
            playerWraith.RpcShowHeldWeapon();

            OnConsumed?.Invoke();
            playerWraith.GetOwningPlayer().Restore(); 
        }
    }

    public override void Release(PlayerWraith playerWraith)
    {
        if (Time.time - eatStartTime < MUFFIN_EATING_DURATION)
        {
            playerWraith.RpcSetAnimatorBool("eating", false);

            playerWraith.RpcRemoveHeldObject();
            playerWraith.RpcShowHeldWeapon();
        }
    }

    public override ConsumableSlotType GetSlot()
    {
        return ConsumableSlotType.Defensive;
    }

    public override int GetMaxCount()
    {
        return 3;
    }

    public override GameObject GetPreviewPrefab()
    {
        return AetherdaleData.GetAetherdaleData().brindeberryMuffinHeldPrefab;
    }
}
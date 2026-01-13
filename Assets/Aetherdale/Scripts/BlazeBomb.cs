using Mirror;
using UnityEngine;

public class BlazeBomb : Consumable
{
    public static readonly LinearEquation SHOP_COST = new (5, 100);

    public const float VELOCITY = 25.0F;
    public const int DAMAGE_BASE = 125; // mutliplied by scaled entity health multiplier

    public const int MAX_BLAZE_BOMBS = 3;

    Vector3 velocityAdjustment = new(0, 3.5F, 0);

    public override void Use(PlayerWraith playerWraith)
    {
        playerWraith.SetAnimatorTrigger("EnterConsumableThrow");
        playerWraith.SetRotationTrackCamera(true);

        playerWraith.TargetEnterAimMode(false);
        playerWraith.GetOwningPlayer().GetUI().ShowReticle();

        playerWraith.RpcHideHeldWeapon();
        playerWraith.RpcHoldBlazeBomb();
    }

    public override void Update(PlayerWraith playerWraith)
    {
    }

    public override void Release(PlayerWraith playerWraith)
    {
        OnConsumed?.Invoke();

        playerWraith.SetAnimatorTrigger("ExitConsumableThrow");
        playerWraith.SetRotationTrackCamera(false);
        

        playerWraith.RpcRemoveHeldObject();
        playerWraith.RpcShowHeldWeapon();

        // TODO need to get this happening only at correct throw point
        int damage = DAMAGE_BASE;
        if (AreaSequencer.GetAreaSequencer().IsSequenceRunning())
        {
            damage = (int) (DAMAGE_BASE * Equation.ENTITY_HEALTH_SCALING.Calculate(AreaSequencer.GetAreaSequencer().GetAreaLevel()));
        }

        PlayerCamera camera = playerWraith.GetCamera();
        Quaternion aimDirection = camera.transform.rotation;
        
        Projectile bombProjectile = Projectile.Create(AetherdaleData.GetAetherdaleData().blazeBombProjectile, playerWraith.GetWorldPosCenter(), aimDirection, playerWraith.gameObject, (aimDirection * new Vector3(0, 0, VELOCITY)) + velocityAdjustment);
        bombProjectile.OnExplosionCreated += (AreaOfEffect.AOEProperties properties) => {properties.damage = damage;};

        playerWraith.TargetExitAimMode();
        //playerWraith.GetOwningPlayer().GetUI().HideReticle();
    }

    [Server]
    public override void GiveToPlayer(Player player)
    {
        player.GetInventory().OffensiveConsumableSlot.SetConsumable(this);
    }

    public override Sprite GetIcon()
    {
        return AetherdaleData.GetAetherdaleData().blazeBombIcon;
    }

    public override string GetName()
    {
        return "Blazebomb";
    }

    public override string GetStatsDescription(Player targetPlayer = null)
    {
        return $"Throw to create an explosion dealing {DAMAGE_BASE} fire damage.";
    }

    public override string GetDescription()
    {
        return "The blazebomb is an explosive monument to human science. Devastating fire aether rages against a spherical iron prison - it just needs a spark to escape.";
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
            typeName = nameof(BlazeBomb),
            statsDescription = GetStatsDescription(),
            description = GetDescription(),
            goldCost = GetShopCost()
        };;
    }

    public override GameObject GetPreviewPrefab()
    {
        return AetherdaleData.GetAetherdaleData().blazeBombHeldPrefab;
    }

    public override int GetMaxCount()
    {
        return MAX_BLAZE_BOMBS;
    }

    public override ConsumableSlotType GetSlot()
    {
        return ConsumableSlotType.Offensive;
    }
}
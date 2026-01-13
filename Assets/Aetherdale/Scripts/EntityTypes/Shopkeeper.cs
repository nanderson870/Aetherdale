using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Shopkeeper : NonPlayerCharacter
{
    public const int BASE_NUMBER_OF_OFFERINGS = 6;

    IShopOffering[] offerings;


    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        
        if (isServer)
        {
            DetermineOfferings(Player.GetLocalPlayer(), AreaSequencer.GetAreaSequencer().GetNextAreaLevel());
        }
    }

    void DetermineOfferings(Player player, int playerLevel)
    {
        offerings = new IShopOffering[BASE_NUMBER_OF_OFFERINGS];

        //offerings[0] = ShopOffering.CreateWeaponOffering(playerLevel);
        //offerings[1] = ShopOffering.CreateTraitOffering(playerLevel);
        //offerings[2] = ShopOffering.CreateTraitOffering(playerLevel);
        //offerings[3] = ShopOffering.CreateTraitOffering(playerLevel);
        //offerings[4] = new BrindleberryMuffin();
        //offerings[5] = new BlazeBomb();
    }

    public override void Interact(ControlledEntity interactingEntity)
    {
        //ShopOfferingInfo[] infos = new ShopOfferingInfo[offerings.Length];
        //for (int i = 0; i < infos.Length; i++)
        //{
        //    infos[i] = offerings[i].GetInfo();
        //}
//
        //interactingEntity.GetOwningPlayer().GetUI().TargetOpenShopMenu(this, infos);
    }

    public override string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Shop";
    }

    public override bool IsInteractable(ControlledEntity interactingEntity)
    {
        return false; //true; //No use for shopkeeper right now
    }

    [Command(requiresAuthority = false)]
    public void CmdPurchase(Player player, int offeringIndex)
    {
        IShopOffering offering = offerings[offeringIndex];

        if (player.GetInventory().GetGold() < offering.GetShopCost())
        {
            return;
        }

        player.GetInventory().RemoveGold(offering.GetShopCost());
        offering.GiveToPlayer(player);
    }
}




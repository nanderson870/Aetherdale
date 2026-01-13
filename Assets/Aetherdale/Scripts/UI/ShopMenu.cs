using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Aetherdale;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopMenu : Menu
{
    [SerializeField] Transform offeringsGroup;

    [SerializeField] ItemDescriptionPanel selectedOfferingPanel;

    [SerializeField] ItemSlot itemSlotPrefab;
    [SerializeField] Button buyButton;

    [SerializeField] TextMeshProUGUI buyButtonLabelTMP;
    [SerializeField] TextMeshProUGUI buyCostTMP;

    [SerializeField] EventReference buySound;

    
    List<ShopOfferingInfo> shopOfferingInfos;
    Shopkeeper currentShopKeeper;
    ItemSlot selectedSlot;


    public override void Open()
    {
        foreach (Transform child in offeringsGroup)
        {
            Destroy(child.gameObject);
        }

        base.Open();

        SetBuyButtonLabels(InputManager.inputScheme);
        InputManager.OnInputSchemeChanged += SetBuyButtonLabels;
    }

    public override void Update()
    {
        base.Update();

        if (selectedSlot != null)
        {
            buyButton.interactable = GetOwningPlayer().GetInventory().GetGold() >= selectedSlot.GetShopOffering().goldCost 
                && ConditionsMetForOffering(selectedSlot.GetShopOffering());
        }
    }


    public override void Close()
    {
        base.Close();
    }
    
    public override void ProcessInput()
    {
        if (InputSystem.actions.FindAction("MenuAction1").WasPerformedThisFrame())
        {
            OnClickPurchase();
        }
    }

    public void SetShopkeeper(Shopkeeper shopkeeper)
    {
        currentShopKeeper = shopkeeper;
    }

    public void SetOfferings(ShopOfferingInfo[] offerings)
    {
        shopOfferingInfos = new ShopOfferingInfo[offerings.Length].ToList();
        for (int i = 0; i < offerings.Length; i++)
        {
            shopOfferingInfos[i] = offerings[i];
            ItemSlot slot = Instantiate(itemSlotPrefab, offeringsGroup);

            slot.SetOffering(offerings[i]);
            slot.OnPressed += SelectSlot;
            slot.SetShowFrameOnHover(true);
        }

        // Selecting the first item needs to occur in one frame, rather than instantaneously
        // Who knows why
        StartCoroutine(SelectFirst());
    }

    IEnumerator SelectFirst()
    {
        yield return null;

        SelectSlot(offeringsGroup.GetChild(0).GetComponent<ItemSlot>());
        EventSystem.current.SetSelectedGameObject(offeringsGroup.GetChild(0).gameObject);
    }

    void SelectSlot(ItemSlot newSelectedSlot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = newSelectedSlot;
        selectedSlot.SetSelected(true);

        ShopOfferingInfo shopOfferingInfo = selectedSlot.GetShopOffering();
        selectedOfferingPanel.SetShopOffering(shopOfferingInfo);

        buyCostTMP.text = $"{shopOfferingInfo.goldCost}<sprite name=\"Gold\">";
    }

    public void OnClickPurchase()
    {
        ShopOfferingInfo shopOfferingInfo = selectedSlot.GetShopOffering();
        if (GetOwningPlayer().GetInventory().GetGold() < shopOfferingInfo.goldCost)
        {
            return;
        }

        AudioManager.Singleton.PlayOneShot(buySound);
        currentShopKeeper.CmdPurchase(GetOwningPlayer(), shopOfferingInfos.IndexOf(shopOfferingInfo));
    }

    bool ConditionsMetForOffering(ShopOfferingInfo offering)
    {
        if (offering.type == ShopOfferingType.Trait)
        {
            return true;
        }
        else if (offering.type == ShopOfferingType.Item)
        {
            return true;
        }
        else if (offering.type == ShopOfferingType.Weapon)
        {
            return true;
        }
        else if (offering.type == ShopOfferingType.Consumable)
        {
            Type type = Type.GetType(offering.typeName);
            if (type == null)
            {
                Debug.LogError("offering type not found");
                return false;
            }

            IShopOffering createdOffering = (IShopOffering)Activator.CreateInstance(type);

            return createdOffering.PlayerMeetsRequirements(GetOwningPlayer());
        }
        else
        {
            return false;
        }
    }

    
    void SetBuyButtonLabels(InputScheme newInputScheme)
    {
        switch (InputManager.platform)
        {
            case Aetherdale.Platform.PC:
                buyButtonLabelTMP.text = $"BUY:";
                break;

            case  Aetherdale.Platform.Playstation:
                buyButtonLabelTMP.text = $"BUY (<sprite name=\"PlaystationSquare\">)";
                break;

            case  Aetherdale.Platform.Xbox:
                buyButtonLabelTMP.text = $"BUY (<sprite name=\"XboxX\">)";
                break;

        }
    }

}

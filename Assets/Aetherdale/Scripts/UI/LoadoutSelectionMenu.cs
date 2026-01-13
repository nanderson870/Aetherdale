using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LoadoutSelectionMenu : Menu
{
    [SerializeField] ItemSlot itemSlotPrefab;

    [SerializeField] LoadoutSelectionPanel idolPanel;
    [SerializeField] LoadoutSelectionPanel weaponPanel;
    [SerializeField] LoadoutSelectionPanel trinketPanel;

    [SerializeField] Transform idolAbilitiesTransform;
    [SerializeField] CooldownWidget[] idolAbilities;

    
    InputAction uiNavigationInputAction;


    public ItemSlot GetItemSlotPrefab()
    {
        return itemSlotPrefab;
    }

    public void Start()
    {
        uiNavigationInputAction = InputSystem.actions.FindAction("Navigate");

        idolAbilities[0].SetTooltipDisplayMode(TooltipDisplayMode.Fixed);
        idolAbilities[1].SetTooltipDisplayMode(TooltipDisplayMode.Fixed);
        idolAbilities[2].SetTooltipDisplayMode(TooltipDisplayMode.Fixed);
    }

    public override void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null && uiNavigationInputAction.ReadValue<Vector2>() != Vector2.zero)
        {
            idolPanel.SelectPanel();
        }
    }

    public override void Open()
    {
        base.Open();

        idolAbilitiesTransform.gameObject.SetActive(false);

        Player player = GetOwningUI().GetOwningPlayer();

        List<IShopOffering> idols = player.GetPlayerData().GetIdolsAsItems().Select(x => (IShopOffering) x).ToList();
        idolPanel.SetData(idols, player.GetPlayerData().EquippedIdol);
        if (player.GetPlayerData().EquippedIdol != null)
        {
            SetIdolGUIData(player.GetPlayerData().EquippedIdol);
        }


// #if UNITY_EDITOR
//         List<IShopOffering> weapons = ItemManager.GetAllWeapons().Select(x => (IShopOffering) x).ToList();
// #else
        List<IShopOffering> weapons = ItemManager.GetAllWeapons()
            .Where(x => x.SelectableFromHub() && player.GetPlayerData().GetWeapon(x.GetItemID()) != null)
            .Select(x => (IShopOffering) x).ToList();
// #endif

        weaponPanel.SetData(weapons, player.GetPlayerData().EquippedWeapon);

        List<IShopOffering> trinkets = player.GetPlayerData().GetTrinkets().Select(x => (IShopOffering) x).ToList();
        trinketPanel.SetData(trinkets, player.GetPlayerData().EquippedTrinket);

        // Callbacks for selecting loadout items
        idolPanel.OnLoadoutItemSelected += IdolItemSelected;
        weaponPanel.OnLoadoutItemSelected += WeaponItemSelected;
        trinketPanel.OnLoadoutItemSelected += TrinketItemSelected;
    }

    public override void Close()
    {
        base.Close();

        idolPanel.ClearData();
        weaponPanel.ClearData();
        trinketPanel.ClearData();

        idolPanel.OnLoadoutItemSelected -= IdolItemSelected;
        weaponPanel.OnLoadoutItemSelected -= WeaponItemSelected;
        trinketPanel.OnLoadoutItemSelected -= TrinketItemSelected;
    }

    public void IdolItemSelected(IShopOffering item)
    {
        if (GetOwningUI().isOwned)
        {
            GetOwningUI().GetOwningPlayer().SetIdol(((Item) item).GetItemID());

            SetIdolGUIData(new((IdolItemData) ItemManager.LookupItemData(((Item) item).GetItemID())));
        }
    }

    void SetIdolGUIData(IdolItem idol)
    {
        IdolForm associatedForm = idol.GetAssociatedForm();

        idolAbilitiesTransform.gameObject.SetActive(true);

        idolAbilities[0].SetIcon(associatedForm.Ability1Icon);
        idolAbilities[0].SetInfo(associatedForm.Ability1Name, associatedForm.Ability1Description);
        idolAbilities[0].gameObject.SetActive(true);

        idolAbilities[1].SetIcon(associatedForm.Ability2Icon);
        idolAbilities[1].SetInfo(associatedForm.Ability2Name, associatedForm.Ability2Description);
        idolAbilities[1].gameObject.SetActive(true);

        idolAbilities[2].SetIcon(associatedForm.UltimateAbilityIcon);
        idolAbilities[2].SetInfo(associatedForm.UltimateAbilityName, associatedForm.UltimateAbilityDescription);
        idolAbilities[2].gameObject.SetActive(true);
    }

    public void WeaponItemSelected(IShopOffering item)
    {
        if (GetOwningUI().isOwned)
        {
            if (item is WeaponData weapon)
            {
                GetOwningUI().GetOwningPlayer().SetWraithWeapon(weapon, true);
            }
        }
    }

    public void TrinketItemSelected(IShopOffering item)
    {
        if (GetOwningUI().isOwned)
        {
            // This is bullshit
            Trinket trinket = new((TrinketData)ItemManager.LookupItemData(((Item)item).GetItemID()));
            if (trinket != null)
            {
                GetOwningUI().GetOwningPlayer().SetTrinket(trinket);
            }
        }
    }
}
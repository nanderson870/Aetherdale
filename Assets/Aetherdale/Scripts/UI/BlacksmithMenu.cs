using System;
using System.Collections.Generic;
using Aetherdale;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[Obsolete]
public class BlacksmithMenu : Menu
{
    // [SerializeField] Sprite defaultItemIcon;
    // [SerializeField] ItemSlot itemSlotPrefab;

    // [SerializeField] Transform weaponSelectGroup;

    // [Header("Selected Item UI")]
    // [SerializeField] Image selectedItemIcon;
    // [SerializeField] ItemDescriptionPanel weaponPanel;
    // [SerializeField] Button forgeButton;
    // [SerializeField] TextMeshProUGUI forgeLabelTMP;
    // [SerializeField] TextMeshProUGUI forgeButtonCostTMP;
    // [SerializeField] Button upgradeButton;
    // [SerializeField] TextMeshProUGUI upgradeLabelTMP;
    // [SerializeField] TextMeshProUGUI upgradeButtonCostTMP;
    // [SerializeField] GameObject dismantleConfirmationPopup;
    // [SerializeField] TextMeshProUGUI dismantleConfirmationItemName;
    // [SerializeField] Transform dismantleItemsGroup;


    // [Header("Inventory Items UI")]
    // [SerializeField] Transform inventoryItemsGroup;

    // List<WeaponData> weapons = new();
    // int selectedWeaponIndex = -1;

    // public override void Open()
    // {
    //     base.Open();

    //     RefreshData();

    //     firstSelectedObject = weaponSelectGroup.GetChild(0).gameObject;
    //     EventSystem.current.SetSelectedGameObject(weaponSelectGroup.GetChild(0).gameObject);

    //     InputManager.OnInputSchemeChanged += SetForgeAndUpgradeLabels;
    // }

    // public override void Close()
    // {
    //     base.Close();

    //     if (selectedWeaponIndex > 0)
    //     {
    //         if (GetOwningPlayer().GetPlayerData().GetWeapon(weapons[selectedWeaponIndex].GetItemID()) != null)
    //         {
    //             GetOwningUI().GetOwningPlayer().SetWraithWeapon(weapons[selectedWeaponIndex], true);
    //         }
    //     }

    //     foreach (Transform child in inventoryItemsGroup)
    //     {
    //         Destroy(child.gameObject);
    //     }

    //     foreach (Transform child in weaponSelectGroup)
    //     {
    //         Destroy(child.gameObject);
    //     }

    //     Tooltip.Hide();

    //     firstSelectedObject = null;

    //     InputManager.OnInputSchemeChanged -= SetForgeAndUpgradeLabels;
    // }

    // public override void ProcessInput()
    // {
    //     if (InputSystem.actions.FindAction("MenuAction1").WasPerformedThisFrame())
    //     {
    //         if (GetOwningUI().GetOwningPlayer().GetPlayerData().HasItem(weapons[selectedWeaponIndex].GetItemID()))
    //         {
    //             UpgradeSelectedWeapon();
    //         }
    //         // else
    //         // {
    //         //     ForgeSelectedWeapon();
    //         // }
    //     }
    // }

    // void SelectWeapon(WeaponData weapon)
    // {
    //     if (selectedWeaponIndex >= 0)
    //     {
    //         weaponSelectGroup.GetChild(selectedWeaponIndex).GetComponent<ItemSlot>().SetSelected(false);
    //     }

    //     weaponPanel.SetWeapon(weapon);
    //     if (GetOwningPlayer().GetPlayerData().GetWeapon(weapon.GetItemID()) != null)
    //     {
    //         selectedWeaponIndex = weapons.IndexOf(weapon);

    //         forgeButton.gameObject.SetActive(false);
    //         upgradeButton.gameObject.SetActive(true);

    //         // Set basic weapon info
    //         Sprite itemIcon = weapon.GetIcon();
    //         selectedItemIcon.sprite = itemIcon == null ? defaultItemIcon : itemIcon;

    //         if (selectedWeaponIndex >= 0)
    //         {
    //             WeaponData selectedWeapon = weapons[selectedWeaponIndex];

    //             // Set up upgrade submenu based on whether we can upgrade
    //             if (selectedWeapon.GetRarity() < Rarity.Legendary) // for now all but Legendary items can be upgraded
    //             {
    //                 upgradeButton.gameObject.SetActive(true);

    //                 int upgradeCost = selectedWeapon.CalculateUpgradeAetherCost(selectedWeapon.GetRarity());
    //                 SetUpgradeButtonCostDisplayed(upgradeCost);

    //                 upgradeButton.interactable = GetOwningUI().GetOwningPlayer().GetPlayerData().AetherCount >= upgradeCost;
    //             }
    //             else
    //             {
    //                 upgradeButton.gameObject.SetActive(false);
    //             }
    //         }
    //     }
    //     else
    //     {
    //         selectedWeaponIndex = weapons.IndexOf(weapon);

    //         forgeButton.gameObject.SetActive(true);
    //         upgradeButton.gameObject.SetActive(false);

    //         //forgeButton.interactable = GetOwningUI().GetOwningPlayer().GetPlayerData().Level >= weapon.GetWeaponData().GetWeaponLevel();

    //         Sprite itemIcon = weapon.GetIcon();
    //         selectedItemIcon.sprite = itemIcon == null ? defaultItemIcon : itemIcon;

    //         SetForgingButtonCostDisplayed(weapon.GetUpgradeCost());
    //     }

    //     if (selectedWeaponIndex >= 0)
    //     {
    //         weaponSelectGroup.GetChild(selectedWeaponIndex).GetComponent<ItemSlot>().SetSelected(true);
    //     }
    // }

    // void SetForgeAndUpgradeLabels(InputScheme newInputScheme)
    // {
    //     switch (InputManager.platform)
    //     {
    //         case Platform.PC:
    //             upgradeLabelTMP.text = $"UPGRADE:";
    //             forgeLabelTMP.text = $"FORGE:";
    //             break;

    //         case Platform.Playstation:
    //             upgradeLabelTMP.text = $"UPGRADE (<sprite name=\"PlaystationSquare\">)";
    //             forgeLabelTMP.text = $"FORGE (<sprite name=\"PlaystationSquare\">)";
    //             break;

    //         case Platform.Xbox:
    //             upgradeLabelTMP.text = $"FORGE (<sprite name=\"XboxX\">)";
    //             forgeLabelTMP.text = $"FORGE (<sprite name=\"XboxX\">)";
    //             break;

    //     }
    // }

    // void SetUpgradeButtonCostDisplayed(int cost)
    // {
    //     upgradeButtonCostTMP.text = $"{cost} <sprite name=\"Aether\">";
    // }

    // void SetForgingButtonCostDisplayed(int cost)
    // {
    //     forgeButtonCostTMP.text = $"{cost} <sprite name=\"Aether\">";
    // }

    // public void UpgradeSelectedWeapon()
    // {
    //     if (selectedWeaponIndex < 0 || !GetOwningUI().GetOwningPlayer().GetPlayerData().HasItem(weapons[selectedWeaponIndex].GetItemID()))
    //     {
    //         return;
    //     }

    //     Weapon selectedWeapon = weapons[selectedWeaponIndex];

    //     int cost = selectedWeapon.GetUpgradeCost();
    //     if (GetOwningUI().GetOwningPlayer().GetPlayerData().AetherCount < cost)
    //     {
    //         return;
    //     }

    //     if (GetOwningUI().GetOwningPlayer().GetPlayerData().TryRemoveAether(cost))
    //     {
    //         selectedWeapon.SetRarity(selectedWeapon.GetRarity() + 1);

    //         RefreshData();
    //     }
    // }

    // // public void ForgeSelectedWeapon()
    // // {
    // //     Weapon selectedWeapon = weapons[selectedWeaponIndex];
    // //     if (GetOwningUI().GetOwningPlayer().GetPlayerData().HasItem(selectedWeapon.GetItemID()))
    // //     {
    // //         Debug.LogError("Tried to forge a weapon player already has");
    // //         return;
    // //     }

    // //     int cost = selectedWeapon.GetForgingCost();
    // //     if (GetOwningUI().GetOwningPlayer().GetPlayerData().TryRemoveAether(cost))
    // //     {
    // //         GetOwningUI().GetOwningPlayer().GetPlayerData().AddAccountItem(selectedWeapon.GetItemID(), 1);
    // //         RefreshData();
    // //     }
    // // }

    // void RefreshData()
    // {
    //     weapons.Clear();
    //     foreach (Transform child in inventoryItemsGroup)
    //     {
    //         Destroy(child.gameObject);
    //     }

    //     // Refresh selection item icons
    //     foreach (Transform child in weaponSelectGroup)
    //     {
    //         Destroy(child.gameObject);
    //         //child.gameObject.GetComponent<ItemSlot>().Refresh();
    //     }


    //     List<WeaponData> weaponDatas = ItemManager.GetAllWeapons();
    //     weaponDatas.Sort(delegate(WeaponData wd1, WeaponData wd2)
    //     {
    //         int w1Level = wd1.GetWeaponLevel();
    //         int w2Level = wd2.GetWeaponLevel();

    //         return w1Level.CompareTo(w2Level);
    //     });

    //     foreach (WeaponData weaponData in weaponDatas)
    //     {
    //         ItemSlot itemSlot = Instantiate(itemSlotPrefab, weaponSelectGroup);

    //         Weapon weapon;

    //         if (GetOwningPlayer().GetPlayerData().GetWeapon(weaponData.GetItemID()) is Weapon ownedWeapon)
    //         {
    //             weapon = ownedWeapon;
    //             itemSlot.SetItem(ownedWeapon);
    //         }
    //         else
    //         {
    //             weapon = new Weapon(weaponData);
    //             itemSlot.SetItem(weapon, unknown:true);
    //         }

    //         itemSlot.OnItemPressed += (Item weapon) => {SelectWeapon((Weapon) weapon);};
    //         itemSlot.SetShowFrameOnHover(true);

    //         weapons.Add(weapon);
    //     }

    //     if (selectedWeaponIndex < 0)
    //     {
    //         SelectWeapon(weapons[0]);
    //     }
    //     else
    //     {
    //         SelectWeapon(weapons[selectedWeaponIndex]);
    //     }

    //     SetForgeAndUpgradeLabels(InputManager.inputScheme);

    // }

    // List<Weapon> GetOwnedWeapons() 
    // {
    //     List<Weapon> ownedWeapons = new();
    //     foreach (Weapon weapon in weapons)
    //     {
    //         if (GetOwningPlayer().GetPlayerData().GetWeapon(weapon.GetItemID()) != null)
    //         {
    //             ownedWeapons.Add(weapon);
    //         }
    //     }

    //     return ownedWeapons;
    // }

}

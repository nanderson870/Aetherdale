using TMPro;
using UnityEngine;

public class ItemDescriptionPanel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI itemNameTMP;
    [SerializeField] TextMeshProUGUI itemStatsTMP;
    [SerializeField] TextMeshProUGUI itemDescriptionTMP;

    public void SetItem(Item item)
    {
        if (item == null)
        {
            itemNameTMP.text = "None";

            itemStatsTMP.gameObject.SetActive(false);
            itemDescriptionTMP.gameObject.SetActive(false);
        }
        else
        {
            itemNameTMP.text = item.GetName();

            itemStatsTMP.text = item.GetStatsDescription();
            itemStatsTMP.gameObject.SetActive(itemStatsTMP.text != "");

            itemDescriptionTMP.text = item.GetDescription();
            itemDescriptionTMP.gameObject.SetActive(itemDescriptionTMP.text != "");
        }
    }

    public void SetWeapon(WeaponData weapon)
    {
        itemNameTMP.text = weapon.GetName();
        itemStatsTMP.text = weapon.GetStatsDescription();

        if (Player.GetLocalPlayer().GetPlayerData().GetWeapon(weapon.GetItemID()) != null)
        {
            itemDescriptionTMP.text = weapon.GetDescription();
        }
        else
        {
            itemDescriptionTMP.text = weapon.GetUnlockHint();
        }

    }

    public void SetShopOffering(ShopOfferingInfo shopOffering)
    {
        itemNameTMP.text = shopOffering.name;
        itemStatsTMP.text = shopOffering.statsDescription;
        itemDescriptionTMP.text = shopOffering.description;
    }

    public void SetFields(string title, string description, string flavorDescription = "")
    {
        itemNameTMP.text = title;
        itemStatsTMP.text = description;
        itemDescriptionTMP.text = flavorDescription;
    }
}

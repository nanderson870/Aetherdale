using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoadoutSelectionPanel : MonoBehaviour
{
    [SerializeField] Sprite noSelectionIcon;

    [SerializeField] ItemDescriptionPanel itemDescriptionPanel;

    [SerializeField] GameObject availableOptionsGroup;

    public delegate void LoadoutSelectionAction(IShopOffering selectedItem);
    public event LoadoutSelectionAction OnLoadoutItemSelected;

    ItemSlot noneSlot;

    ItemSlot selectedSlot;

    void Start()
    {
    }

    public void SetData(List<IShopOffering> optionItems, IShopOffering preselected = null)
    {
        ClearData();
        noneSlot = Instantiate(GetComponentInParent<LoadoutSelectionMenu>().GetItemSlotPrefab(), availableOptionsGroup.transform);
        noneSlot.SetIcon(AetherdaleData.GetAetherdaleData().noSelectionIcon);
        noneSlot.SetIconColor(Color.red);
        noneSlot.SetBorderColor(ColorPalette.GetColorForRarity(Rarity.Common));

        noneSlot.OnPressed += SelectSlot;
        
        ItemSlot itemSlotPrefab = GetComponentInParent<LoadoutSelectionMenu>().GetItemSlotPrefab();

        foreach (IShopOffering optionItem in optionItems)
        {
            ItemSlot itemSlot = Instantiate(itemSlotPrefab, availableOptionsGroup.transform);
            itemSlot.SetOffering(optionItem.GetInfo());
            itemSlot.SetQuantityVisible(false);
            
            itemSlot.OnPressed += SelectSlot;
            itemSlot.SetShowFrameOnHover(true);

            if (preselected != null && preselected.GetName() == optionItem.GetName())
            {
                SelectSlot(itemSlot);
            }
        }

        noneSlot.transform.SetAsFirstSibling();
    }

    public void ClearData()
    {
        foreach (Transform itemSlotTransform in availableOptionsGroup.transform)
        {
            Destroy(itemSlotTransform.gameObject);
        }
    }

    void SelectSlot(ItemSlot selectedSlot)
    {
        if (this.selectedSlot != null)
        {
            this.selectedSlot.SetSelected(false);
        }

        this.selectedSlot = selectedSlot;

        this.selectedSlot.SetSelected(true);

        itemDescriptionPanel.SetShopOffering(selectedSlot.GetShopOffering());

        OnLoadoutItemSelected?.Invoke(ShopOffering.ShopOfferingFromInfo(selectedSlot.GetShopOffering()));
    }

    public void SelectPanel()
    {
        EventSystem.current.SetSelectedGameObject(availableOptionsGroup.transform.GetChild(0).gameObject);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private Item slotItem;
    private ShopOfferingInfo slotOffering;

    [SerializeField] Sprite defaultItemIcon;


    [SerializeField] Image itemBorder;

    [SerializeField] Image itemIcon;

    [SerializeField] TextMeshProUGUI quantityTMP;

    [SerializeField] Image selectedFrame;
    [SerializeField] Image hoveredFrame;

    public Action<ItemSlot> OnPressed;
    public Action<Item> OnItemPressed;
    public Action<ShopOfferingInfo> OnShopOfferingPressed;

    Color defaultSlotColor;

    bool unknown = false;

    bool showsTooltip = false;

    bool showFrameOnHover = false;

    TooltipDisplayMode tooltipDisplayMode = TooltipDisplayMode.Fixed;

    public void Start()
    {
        defaultSlotColor = itemIcon.color;

        quantityTMP.outlineWidth = .5F;
        quantityTMP.outlineColor = Color.black;
        //quantityTMP.outlineWidth = 0.2F;
    }

    public void OnButtonPress()
    {
        OnPressed?.Invoke(this);

        if (slotItem != null)
        {
            OnItemPressed?.Invoke(slotItem);
        }
        
        if (slotOffering != null)
        {
            OnShopOfferingPressed?.Invoke(slotOffering);
        }
    }

    public void SetItem(Item item, bool unknown = false)
    {
        slotItem = item;

        Sprite icon = slotItem.GetIcon();

        quantityTMP.text = item.GetQuantity().ToString();

        SetIcon(icon);
        SetBorderColor(ColorPalette.GetColorForRarity(item.GetRarity()));

        if (unknown)
        {
            this.unknown = true;
            itemIcon.color = Color.black;
        }
    }

    public Item GetItem()
    {
        return slotItem;
    }

    public void SetOffering(ShopOfferingInfo offering, bool unknown = false)
    {
        slotItem = null;
        slotOffering = offering;

        Sprite icon;
        if (offering.type == ShopOfferingType.Trait)
        {
            Trait newTrait = (Trait)Activator.CreateInstance(Type.GetType(offering.name.Replace(" ", "")));
            icon = newTrait.GetIcon();
        }
        else if (offering.type == ShopOfferingType.Item)
        {
            ItemData itemData = ItemManager.LookupItemDataByName(offering.name);
            icon = itemData.GetIcon();
        }
        else if (offering.type == ShopOfferingType.Weapon)
        {
            icon = ItemManager.LookupItemDataByName(offering.name).GetIcon();
        }
        else if (offering.type == ShopOfferingType.Consumable)
        {
            IShopOffering createdOffering = (IShopOffering)Activator.CreateInstance(Type.GetType(offering.typeName));
            icon = createdOffering.GetIcon();
        }
        else
        {
            throw new Exception("Invalid offering type");
        }

        SetIcon(icon);

        if (unknown)
        {
            this.unknown = true;
            itemIcon.color = Color.black;
        }
    }

    public ShopOfferingInfo GetShopOffering()
    {
        return slotOffering;
    }


    public void Refresh()
    {
        if (slotItem != null)
        {
            SetItem(slotItem, unknown);
        }
        else if (slotOffering != null)
        {
            SetOffering(slotOffering, unknown);
        }
    }

    public void SetQuantityVisible(bool visible)
    {
        quantityTMP.gameObject.SetActive(visible);
    }

    public void Clear()
    {
        slotItem = null;
    }

    public void SetIcon(Sprite newIcon)
    {
        if (newIcon == null)
        {
            itemIcon.sprite = defaultItemIcon;
        }
        else
        {
            itemIcon.sprite = newIcon;
            itemIcon.color = Color.white;
        }
    }

    public void SetIconColor(Color color)
    {
        itemIcon.color = color;
    }

    public void SetBorderColor(Color color)
    {
        itemBorder.color = color;
    }

    public void SetDisplayedQuantity(int quantity)
    {
        quantityTMP.text = quantity.ToString();
    }

    public void EnableTooltip(TooltipDisplayMode tooltipDisplayMode)
    {
        showsTooltip = true;
        this.tooltipDisplayMode = tooltipDisplayMode;
    }

    public void SetShowFrameOnHover(bool showFrameOnHover)
    {
        this.showFrameOnHover = showFrameOnHover;
    }

    public void SetSelected(bool selected)
    {
        selectedFrame.gameObject.SetActive(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showsTooltip)
        {
            Tooltip.SetDisplayMode(TooltipDisplayMode.Fixed);
            Tooltip.Show(gameObject, slotItem);
        }

        if (showFrameOnHover)
        {
            hoveredFrame.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (showsTooltip)
        {
            Tooltip.Hide();
        }

        hoveredFrame.gameObject.SetActive(false);
    }

	public void OnSelect (BaseEventData eventData) 
	{
        if (showFrameOnHover)
        {
            hoveredFrame.gameObject.SetActive(true);
        }
	}

	public void OnDeselect (BaseEventData eventData) 
	{
        hoveredFrame.gameObject.SetActive(false);
	}

}

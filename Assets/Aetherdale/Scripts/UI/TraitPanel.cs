using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TraitPanel : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{

    [SerializeField][FormerlySerializedAs("backPanel")] Image rarityPanel;
    [SerializeField] Image traitIcon;
    [SerializeField] TextMeshProUGUI traitNameTMPro;
    [SerializeField] TextMeshProUGUI traitDescriptionTMPro;
    [SerializeField] TextMeshProUGUI rarityText;
    [SerializeField] Image hoverFrame;

    public Action OnClick;

    public TraitInfo traitInfo;
    public void SetTrait(TraitInfo trait)
    {
        traitInfo = trait;
        rarityPanel.color = ColorPalette.GetColorForRarity(trait.rarity);
        rarityText.color = ColorPalette.GetColorForRarity(trait.rarity);
        traitIcon.sprite = trait.ToTrait().GetIcon();
        traitIcon.color = ColorPalette.GetColorForRarity(trait.rarity);
        traitNameTMPro.text = trait.traitName;
        traitDescriptionTMPro.text = trait.traitDescription;
        rarityText.text = trait.rarity.ToString();
    }

    public void SetHovered(bool hovered)
    {
        hoverFrame.gameObject.SetActive(hovered);
    }

    public void OnSelect(BaseEventData eventData)
    {
        GetComponentInParent<TraitSelectionMenu>().HoverPanel(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponentInParent<TraitSelectionMenu>().HoverPanel(this);
    }

    public void Click()
    {
        OnClick?.Invoke();
    }

}

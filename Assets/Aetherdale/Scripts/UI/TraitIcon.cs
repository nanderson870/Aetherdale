using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TraitIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Trait trait;
    [SerializeField] Image image;
    [SerializeField] TextMeshProUGUI countTMP;
    
    void Awake()
    {
        countTMP.outlineWidth = 1;
    }

    public void SetTrait(Trait trait)
    {
        image.sprite = trait.GetSpriteIcon();

        countTMP.text = trait.GetNumberOfStacks().ToString();

        this.trait = trait;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Tooltip.SetDisplayMode(TooltipDisplayMode.Fixed);
        Tooltip.Show(gameObject, trait.GetInfo());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.Hide();
    }
}

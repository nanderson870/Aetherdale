using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PostAreaMenu : Menu
{
    [SerializeField] GameObject rewardsPanel;
    [SerializeField] ItemSlot itemSlotPrefab;

    [SerializeField] TextMeshProUGUI enemyLevelAnnouncement;

    [SerializeField] Button areaOption1Button;
    [SerializeField] Button areaOption2Button;

    Area[] areaOptions;

    public override void Open()
    {
        base.Open();

        /*
        areaOptions = AreaSequencer.GetAreaSequencer().GetNextAreaOptions();

        enemyLevelAnnouncement.text = "Enemy level is now " + AreaSequencer.GetAreaSequencer().GetNextAreaLevel();

        areaOption1Button.GetComponentInChildren<TextMeshProUGUI>().text = areaOptions[0].GetAreaName();
        areaOption2Button.GetComponentInChildren<TextMeshProUGUI>().text = areaOptions[1].GetAreaName();
        */
    }

    public override void Close()
    {
        base.Close();

        ItemSlot[] itemSlots = rewardsPanel.GetComponentsInChildren<ItemSlot>();
        foreach(ItemSlot itemSlot in itemSlots)
        {
            Destroy(itemSlot.gameObject);
        }
    }

    public void Return()
    {
        Close();

        if (GetOwningUI().isServer)
            AreaSequencer.GetAreaSequencer().StopAreaSequence();
    }

    public void SetRewardItems(ItemList itemList)
    {
        foreach (Item item in itemList.GetItems())
        {
            ItemSlot slot = Instantiate(itemSlotPrefab, rewardsPanel.transform);
            slot.SetItem(item);
        }
    }

    /*
    public void SelectAreaOption1()
    {
        if (GetOwningUI().isServer)
        {
            AreaSequencer.GetAreaSequencer().LoadArea(AreaSequencer.GetAreaSequencer().GetNextAreaOptions()[0]);
        }

        Close();
    }

    public void SelectAreaOption2()
    {
        if (GetOwningUI().isServer)
        {
            AreaSequencer.GetAreaSequencer().LoadArea(AreaSequencer.GetAreaSequencer().GetNextAreaOptions()[1]);
        }
        
        Close();
    }
    */
}

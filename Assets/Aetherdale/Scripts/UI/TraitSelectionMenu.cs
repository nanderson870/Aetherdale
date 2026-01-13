using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TraitSelectionMenu : Menu, IOnLocalPlayerReadyTarget
{
    public TraitPanel traitPanelPrefab;

    public Transform traitPanelTransform;

    public Button rerollButton;
    public TextMeshProUGUI rerollButtonTMP;

    TraitInfo[] traits;



    public void OnLocalPlayerReady(Player player)
    {
        GetOwningPlayer().OnPendingTraitOptionsChanged += PendingTraitOptionsChanged;
    }

    void PendingTraitOptionsChanged(Player _)
    {
        SetTraits(GetOwningPlayer().pendingTraitOptions.ToArray());
    }

    public void SetTraits(TraitInfo[] traits)
    {
        foreach (Transform child in traitPanelTransform)
        {
            Destroy(child.gameObject);
        }

        this.traits = traits;
        for (int i = 0; i < traits.Count(); i++)
        {
            TraitPanel inst = Instantiate(traitPanelPrefab, traitPanelTransform);
            inst.SetTrait(traits[i]);

            inst.OnClick += () => { RequestTraitSelection(inst); };
        }


        // if (IsOpen())
        // {
        //     GetOwningUI().SetTraitSelectionReminderVisible(false);
        // }
    }


    public override void Update()
    {
        if (firstSelectedObject == null)
        {
            firstSelectedObject = traitPanelTransform.GetChild(0).gameObject;
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
        }
    }

    public override void Open()
    {

        if (Player.GetPlayers().Count == 1)
        {
            WorldManager.GetWorldManager().SetTimescale(0);
        }

        rerollButtonTMP.text = $"Reroll traits ({GetOwningPlayer().traitRerolls})";
        if (GetOwningPlayer().traitRerolls == 0)
        {
            rerollButton.enabled = false;
        }

        base.Open();
    }

    public override void Close()
    {
        base.Close();

        GetOwningUI().CmdEnableReminderIfNecessary();

        if (Player.GetPlayers().Count == 1)
        {
            WorldManager.GetWorldManager().SetTimescale(1);
        }

        foreach (Transform child in traitPanelTransform)
        {
            child.GetComponent<TraitPanel>().SetHovered(false);
        }

        
        GetOwningUI().SetTraitSelectionReminderVisible(GetOwningPlayer().traitsOwed > 0);
    }

    public void RequestTraitSelection(TraitPanel panel)
    {
        if (GetOwningUI().isClient)
        {
            GetOwningPlayer().CmdRequestTraitSelection(panel.traitInfo);
        }

        Close();
    }

    public void HoverPanel(TraitPanel hoveredPanel)
    {
        foreach (Transform child in traitPanelTransform)
        {
            TraitPanel panel = child.GetComponent<TraitPanel>();
            panel.SetHovered(panel == hoveredPanel);
        }
    }

    public void RerollButtonPressed()
    {
        if (GetOwningPlayer().traitRerolls <= 0)
        {
            return;
        }

        GetOwningPlayer().RerollPendingTraitOption();
    }

}

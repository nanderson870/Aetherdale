using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class FloatingInteractionPrompt : FloatingUIElement
{
    [SerializeField] GameObject talkObject;
    [SerializeField] GameObject interactObject;
    
    [SerializeField] TextMeshProUGUI interactionTextTMP;

    [SerializeField] GameObject talkKeybindIcon;
    [SerializeField] GameObject talkUnavailableIcon;
    
    [SerializeField] GameObject interactKeybindIcon;
    [SerializeField] GameObject interactUnavailableIcon;

    public override bool IsValid()
    {
        return true;
    }

    public void SetData(SelectedInteractionPromptData data)
    {
        interactObject.SetActive(false);
        talkObject.SetActive(false);

        // Tooltip.Hide();

        // Setup interaction info
        ControlledEntity localEntity = Player.GetLocalPlayer().GetControlledEntity();
        if (localEntity == null)
        {
            return;
        }

        if (data == null)
        {
            return;
        }

        if (data.position != Vector3.negativeInfinity)
        {
            SetWorldPosition(data.position);
        }
        
        if (data.selectable)
        {
            interactObject.SetActive(true);
            if (data.tooltipData != null)
            {
                string tooltipTitle = data.tooltipData.titleText;
                string tooltipText = data.tooltipData.descriptionText;

                
                // if (tooltipTitle != "" && tooltipText != "")
                // {
                //     Tooltip.SetDisplayMode(TooltipDisplayMode.Fixed);
                //     Tooltip.Show(data.associatedObject.gameObject, tooltipTitle, tooltipText);
                // }

            }

            interactionTextTMP.text = data.interactionPromptText;

            if (data.interactable)
            {
                interactKeybindIcon.SetActive(true);
                interactUnavailableIcon.SetActive(false);
            }
            else
            {
                interactKeybindIcon.SetActive(false);
                interactUnavailableIcon.SetActive(true);
            }

            //TODO tooltip?
        }


        if (data.canTalk)
        {
            // Setup dialogue info
            talkObject.SetActive(true);
            talkKeybindIcon.SetActive(true);
            talkUnavailableIcon.SetActive(false);
        }
    }

    public override void Hide()
    {
        base.Hide();

        Tooltip.Hide();
    }
}


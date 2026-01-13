using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SequenceStartMenu : Menu
{
    [SerializeField] Button selectNormalBtn;
    [SerializeField] Button selectGauntltBtn;

    [SerializeField] TextMeshProUGUI modeNameTMP;
    [SerializeField] TextMeshProUGUI descriptionTMP;

    [SerializeField] Color greyedOutBtnColor;

    [TextArea(10, 5)]
    [SerializeField] string normalModeDescription;

    [TextArea(10, 5)]
    [SerializeField] string gauntletModeDescription;


    Color defaultNormalBtnColor;
    Color defaultGauntletBtnColor;

    AreaSequencer.SequenceMode selection = AreaSequencer.SequenceMode.Normal;

    public void Start()
    {
        defaultNormalBtnColor = selectNormalBtn.image.color;
        defaultGauntletBtnColor = selectGauntltBtn.image.color;

        SelectNormal();
    }

    public override void ProcessInput()
    {
        if (InputSystem.actions.FindAction("Submit").WasPressedThisFrame())
        {
            StartSequence();
        }
    } 

    public void StartSequence()
    {

        Close();

        GetOwningUI().GetOwningPlayer().ClientStartSequence(selection);
    }

    public void SelectNormal()
    {
        selection = AreaSequencer.SequenceMode.Normal;

        selectNormalBtn.image.color = defaultNormalBtnColor;
        selectGauntltBtn.image.color = greyedOutBtnColor;

        modeNameTMP.text = "Normal Mode";
        descriptionTMP.text = normalModeDescription;
    }

    public void SelectGuantlet()
    {
        selection = AreaSequencer.SequenceMode.Gauntlet;

        selectNormalBtn.image.color = greyedOutBtnColor;
        selectGauntltBtn.image.color = defaultGauntletBtnColor;

        modeNameTMP.text = "Gauntlet Mode";
        descriptionTMP.text = gauntletModeDescription;
    }
}

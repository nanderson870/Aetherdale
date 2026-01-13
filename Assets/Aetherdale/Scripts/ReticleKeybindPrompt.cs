using Aetherdale;
using TMPro;
using UnityEngine;

public class ReticleKeybindPrompt : MonoBehaviour
{
    [SerializeField] KeybindImage keybindImage;
    [SerializeField] TextMeshProUGUI hintText;

    public void SetData(string action, string hint)
    {
        keybindImage.associatedInputActionName = action;
        hintText.text = hint;
    }
}
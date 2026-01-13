using Aetherdale;
using UnityEngine;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{
    [SerializeField] Image reticleImage;

    [SerializeField] Transform rightPromptGroup;
    [SerializeField] Transform leftPromptGroup;

    [SerializeField] ReticleKeybindPrompt keybindPromptPrefabRight;
    [SerializeField] ReticleKeybindPrompt keybindPromptPrefabLeft;
    
    public static void ClearKeybindPrompts()
    {
        Player.GetLocalPlayer().GetUI().GetReticle().ClearKeybindPromptsNonstatic();
    }

    public static void AddRightKeybindPrompt(string action, string hint)
    {
        Player.GetLocalPlayer().GetUI().GetReticle().CreateRightKeybindPrompt(action, hint);
    }

    public static void AddLeftKeybindPrompt(string action, string hint)
    {
        Player.GetLocalPlayer().GetUI().GetReticle().CreateLeftKeybindPrompt(action, hint);
    }


    void ClearKeybindPromptsNonstatic()
    {
        foreach (Transform child in rightPromptGroup) Destroy(child.gameObject);
        foreach (Transform child in leftPromptGroup) Destroy(child.gameObject);
    }

    void CreateRightKeybindPrompt(string action, string hint)
    {
        Instantiate(keybindPromptPrefabRight, rightPromptGroup).SetData(action, hint);
    }

    void CreateLeftKeybindPrompt(string action, string hint)
    {
        Instantiate(keybindPromptPrefabLeft, leftPromptGroup).SetData(action, hint);
    }
}

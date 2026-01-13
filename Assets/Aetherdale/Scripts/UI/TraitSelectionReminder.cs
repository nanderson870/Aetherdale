using Aetherdale;
using TMPro;
using UnityEngine;

public class TraitSelectionReminder : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmp;
    void Start()
    {
        OnInputSchemeChanged(InputManager.inputScheme);
        
        InputManager.OnInputSchemeChanged += OnInputSchemeChanged;       
    }

    void OnInputSchemeChanged(InputScheme inputScheme)
    {
        if (inputScheme == InputScheme.MouseAndKeyboard)
        {
            tmp.text = "Trait Available! \nPress C to choose";
        }
        else
        {
            if (InputManager.platform == Platform.Playstation)
            {
                tmp.text = "Trait Available! \nPress <sprite name=\"PlaystationDpadLeft\"> to choose";
            }
            else // Xbox/other
            {
                tmp.text = "Trait Available! \nPress <sprite name=\"XboxDpadLeft\"> to choose";
            }
        }
    }
}

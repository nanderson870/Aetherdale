using UnityEngine;
using UnityEngine.UI;

namespace Aetherdale
{
    public class KeybindImage : MonoBehaviour
    {
        public string associatedInputActionName;

        void Start()
        {
            RefreshImage(InputManager.inputScheme);

            InputManager.OnInputSchemeChanged += RefreshImage;
        }

        void OnDestroy()
        {
            InputManager.OnInputSchemeChanged -= RefreshImage;
        }

        void RefreshImage(InputScheme inputScheme)
        {
            Sprite keybindSprite = InputManager.inputActionIcons.GetInputActionSprite(associatedInputActionName);

            if (keybindSprite == null)
            {
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(true);
                GetComponent<Image>().sprite = keybindSprite;
            }
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace Aetherdale
{
        
    public enum InputScheme
    {
        MouseAndKeyboard = 0,
        Gamepad = 1
    }

    public enum Platform
    {
        PC = 0,
        Playstation = 1,
        Xbox = 2,
    }

    public class InputManager : MonoBehaviour
    {
        static InputManager Singleton;
        
        public static InputScheme inputScheme = InputScheme.MouseAndKeyboard;
        public static Platform platform = Platform.PC;

        public static InputActionIcons inputActionIcons;

        public static Action<InputScheme> OnInputSchemeChanged;
        
        InputActionMap inputActionMap;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (Singleton != null)
            {
                Destroy(gameObject);
                return;
            }

            Singleton = this;

            inputActionMap = InputSystem.actions.actionMaps[0];
            inputActionMap.actionTriggered += InputActionTriggered;

            inputActionIcons = GetComponent<InputActionSchemes>().mouseAndKeyboard;

            DontDestroyOnLoad(gameObject);
        }
        
        void InputActionTriggered(InputAction.CallbackContext context)
        {
            InputScheme previous = inputScheme;
            if (context.control.device is Gamepad gamepad)
            {
                inputScheme = InputScheme.Gamepad;
                if (gamepad is DualShockGamepad)
                {
                    inputActionIcons = GetComponent<InputActionSchemes>().playstation;
                    platform = Platform.Playstation;
                }
                else
                {
                    inputActionIcons = GetComponent<InputActionSchemes>().xbox;
                    platform = Platform.Xbox;
                }
            }
            else
            {
                inputScheme = InputScheme.MouseAndKeyboard;
                inputActionIcons = GetComponent<InputActionSchemes>().mouseAndKeyboard;
                platform = Platform.PC;
            }

            if (previous != inputScheme)
            {
                OnInputSchemeChanged?.Invoke(inputScheme);
            }
        }
    }
}

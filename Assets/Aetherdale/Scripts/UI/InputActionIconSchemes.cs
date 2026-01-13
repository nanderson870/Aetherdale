using System;
using UnityEngine;

public class InputActionSchemes : MonoBehaviour
{
    public InputActionIcons mouseAndKeyboard;
    public InputActionIcons playstation;
    public InputActionIcons xbox;
}

[Serializable]
public class InputActionIcons
{
    public Sprite move;
    public Sprite look;
    public Sprite attack;
    public Sprite secondaryAttack;
    public Sprite jump;
    public Sprite dodge;
    public Sprite sprint;
    public Sprite ability1;
    public Sprite ability2;
    public Sprite ultimateAbility;
    public Sprite transform;
    public Sprite trinket;
    public Sprite offensiveConsumable;
    public Sprite defensiveConsumable;
    public Sprite utilityConsumable;
    public Sprite talk;
    public Sprite interact;
    public Sprite uiNavigate;
    public Sprite uiSubmit;
    public Sprite uiCancel;
    public Sprite uiClick;
    public Sprite uiRightClick;
    public Sprite uiMiddleClick;
    public Sprite uiScroll;
    public Sprite uiPause;
    public Sprite uiChooseTrait;
    public Sprite uiTabRight;
    public Sprite uiTabLeft;
    public Sprite uiMenuAction1;
    public Sprite uiMenuAction2;


    public Sprite GetInputActionSprite(string name)
    {
        return name switch
        {
            "move" => move,
            "look" => look,
            "attack" => attack,
            "secondaryAttack" => secondaryAttack,
            "jump" => jump,
            "dodge" => dodge,
            "sprint" => sprint,
            "ability1" => ability1,
            "ability2" => ability2,
            "ultimateAbility" => ultimateAbility,
            "transform" => transform,
            "trinket" => trinket,
            "offensiveConsumable" => offensiveConsumable,
            "defensiveConsumable" => defensiveConsumable,
            "utilityConsumable" => utilityConsumable,
            "talk" => talk,
            "interact" => interact,
            "uiNavigate" => uiNavigate,
            "uiSubmit" => uiSubmit,
            "uiCancel" => uiCancel,
            "uiClick" => uiClick,
            "uiRightClick" => uiRightClick,
            "uiMiddleClick" => uiMiddleClick,
            "uiScroll" => uiScroll,
            "uiPause" => uiPause,
            "uiChooseTrait" => uiChooseTrait,
            "uiTabRight" => uiTabRight,
            "uiTabLeft" => uiTabLeft,
            "uiMenuAction1" => uiMenuAction1,
            "uiMenuAction2" => uiMenuAction2,
            _ => null,
        };
    }
}

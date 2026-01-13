using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    // ----- Input ----------
    InputAction lookInputAction;
    InputAction moveInputAction;
    InputAction jumpInputAction;
    InputAction attackInputAction;
    InputAction secondaryAttackInputAction;
    InputAction tertiaryAttackInputAction;
    InputAction dodgeInputAction;
    InputAction sprintInputAction;
    InputAction ability1InputAction;
    InputAction ability2InputAction;
    InputAction ultimateAbilityInputAction;
    InputAction transformInputAction;
    InputAction talkInputAction;
    InputAction interactInputAction;
    InputAction trinketInputAction;
    InputAction offensiveConsumableInputAction;
    InputAction defensiveConsumableInputAction;
    InputAction utilityConsumableInputAction;
    InputAction flipAimOffsetInputAction;

    
    public static PlayerInputData Input {get; internal set;} = new();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        lookInputAction = InputSystem.actions.FindAction("Look");
        moveInputAction = InputSystem.actions.FindAction("Move");
        jumpInputAction = InputSystem.actions.FindAction("Jump");
        attackInputAction = InputSystem.actions.FindAction("Attack");
        secondaryAttackInputAction = InputSystem.actions.FindAction("SecondaryAttack");
        tertiaryAttackInputAction = InputSystem.actions.FindAction("TertiaryAttack");
        dodgeInputAction = InputSystem.actions.FindAction("Dodge");
        sprintInputAction = InputSystem.actions.FindAction("Sprint");
        ability1InputAction = InputSystem.actions.FindAction("Ability1");
        ability2InputAction = InputSystem.actions.FindAction("Ability2");
        ultimateAbilityInputAction = InputSystem.actions.FindAction("UltimateAbility");
        transformInputAction = InputSystem.actions.FindAction("Transform");
        talkInputAction = InputSystem.actions.FindAction("Talk");
        interactInputAction = InputSystem.actions.FindAction("Interact");
        trinketInputAction = InputSystem.actions.FindAction("Trinket");
        offensiveConsumableInputAction = InputSystem.actions.FindAction("OffensiveConsumable");
        defensiveConsumableInputAction = InputSystem.actions.FindAction("DefensiveConsumable");
        utilityConsumableInputAction = InputSystem.actions.FindAction("UtilityConsumable");
        flipAimOffsetInputAction = InputSystem.actions.FindAction("FlipAimOffset");
    }

    // Update is called once per frame
    public void ReadInput(Player player)
    {
        // Looking angle preserved whether we are dead, go into GUI, whatever
        if (player.GetCamera() != null)
        {
            Input.lookingAngle = player.GetCamera().GetPreShakeEulers().y;
        }

        if (player == null || player.GetControlledEntity() == null || player.GetControlledEntity().IsDead())// || player.GetControlledEntity().InGUI())
        {
            return;
        }

        Input.movementInput = moveInputAction.ReadValue<Vector2>();

        Input.jump = jumpInputAction.WasPressedThisFrame();

        Input.lookInput = lookInputAction.ReadValue<Vector2>() * new Vector2(1, -1);

        Input.dodge = dodgeInputAction.WasPerformedThisFrame();

        Input.sprintDown = sprintInputAction.WasPressedThisFrame();
        Input.sprintReleased = sprintInputAction.WasReleasedThisFrame();

        Input.basicAttack1 = attackInputAction.IsPressed();
        Input.basicAttack2 = secondaryAttackInputAction.WasPressedThisFrame();
        Input.basicAttack3 = tertiaryAttackInputAction.WasPressedThisFrame();

        Input.releaseBasicAttack1 = attackInputAction.WasReleasedThisFrame();
        Input.releaseBasicAttack2 = secondaryAttackInputAction.WasReleasedThisFrame();
        Input.releaseBasicAttack3 = tertiaryAttackInputAction.WasReleasedThisFrame();

        Input.ability1 = ability1InputAction.WasPressedThisFrame();
        Input.releaseAbility1 = ability1InputAction.WasReleasedThisFrame();

        Input.ability2 = ability2InputAction.WasPressedThisFrame();
        Input.releaseAbility2 = ability2InputAction.WasReleasedThisFrame();

        Input.ultimateAbility = ultimateAbilityInputAction.WasPressedThisFrame();
        Input.releaseUltimateAbility= ultimateAbilityInputAction.WasReleasedThisFrame();

        Input.transform = transformInputAction.WasPressedThisFrame();

        Input.interact = interactInputAction.WasPressedThisFrame();
        Input.interactHeld = interactInputAction.IsPressed();

        Input.talk = talkInputAction.WasPressedThisFrame();

        Input.trinket = trinketInputAction.WasPressedThisFrame();

        Input.offensiveConsumable = offensiveConsumableInputAction.WasPressedThisFrame();
        Input.offensiveConsumableHeld = offensiveConsumableInputAction.IsPressed() && !offensiveConsumableInputAction.WasPressedThisFrame();
        Input.offensiveConsumableReleased = offensiveConsumableInputAction.WasReleasedThisFrame();

        Input.defensiveConsumable = defensiveConsumableInputAction.WasPressedThisFrame();
        Input.defensiveConsumableHeld = defensiveConsumableInputAction.IsPressed() && !defensiveConsumableInputAction.WasPressedThisFrame();
        Input.defensiveConsumableReleased = defensiveConsumableInputAction.WasReleasedThisFrame();

        Input.utilityConsumable = utilityConsumableInputAction.WasPressedThisFrame();
        Input.utilityConsumableHeld = utilityConsumableInputAction.IsPressed() && !utilityConsumableInputAction.WasPressedThisFrame();
        Input.utilityConsumableReleased = utilityConsumableInputAction.WasReleasedThisFrame();

        Input.flipAimOffset = flipAimOffsetInputAction.WasPressedThisFrame();
    }
}

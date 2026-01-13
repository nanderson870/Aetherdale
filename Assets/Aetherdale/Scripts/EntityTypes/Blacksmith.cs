
public class Blacksmith : NonPlayerCharacter, IInteractable
{
    public override void Interact(ControlledEntity interactingEntity)
    {
        // Player playerAddressed = interactingEntity.GetOwningPlayer();
        // playerAddressed.GetUI().TargetOpenBlacksmithMenu();
    }

    public override string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "Blacksmith";
    }

    public override bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }
}
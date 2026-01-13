

public class IdolUpgrader : NonPlayerCharacter
{
    public override string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return "";
    }

    public override bool IsInteractable(ControlledEntity interactingEntity)
    {
        return false; //true;
    }
}

public class BeatAreaObjective : Objective
{
    Area areaToBeat;

    public BeatAreaObjective(BeatAreaObjectiveData objectiveData) : base(objectiveData)
    {
        areaToBeat = objectiveData.AreaToBeat;
    }

    void ProgressObjective(string areaID)
    {
        if (areaID == areaToBeat.GetAreaID())
        {
            base.ProgressObjective();
        }
    }
    
    public override void RegisterCallbacks(Player owningPlayer)
    {
        owningPlayer.ClientOnAreaCompleted += ProgressObjective;
    }

    public override void UnregisterCallbacks(Player owningPlayer)
    {
    }

}

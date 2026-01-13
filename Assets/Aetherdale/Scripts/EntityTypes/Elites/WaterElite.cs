
/// <summary>
/// Elite with absorption shield effect
/// </summary>
public class WaterElite : ElementalElite 
{
    const float WATER_ELITE_ABSORB_CHANCE = 35.0F;
    public override string GetElitePrefix()
    {
        return "Aqueous";
    }

    public override Element GetElement()
    {
        return Element.Water;
    }

    public override void Start()
    {
        base.Start();

        Entity entity = GetComponent<Entity>();
        entity.SetStat(Stats.AbsorbChance, WATER_ELITE_ABSORB_CHANCE);
    }
}

using UnityEngine;

public class LightElite : ElementalElite
{
    const float PROJECTILE_CHARGER_CREATION_INTERVAL = 10;
    const float PROJECTILE_CHARGERS_PER_INTERVAL = 3;

    const float CHARGER_SPAWN_DISTANCE = 6.0F;


    public override string GetElitePrefix()
    {
        return "Radiant";
    }

    public override Element GetElement()
    {
        return Element.Light;
    }

    public override void Start()
    {
        base.Start();

        InvokeRepeating(nameof(CreateChargers), PROJECTILE_CHARGER_CREATION_INTERVAL, PROJECTILE_CHARGER_CREATION_INTERVAL);
    }

    void CreateChargers()
    {
        for (int i = 0; i < PROJECTILE_CHARGERS_PER_INTERVAL; i++)
        {
            Vector3 offset = Random.insideUnitSphere.normalized * CHARGER_SPAWN_DISTANCE;
            offset.y = Mathf.Abs(offset.y); // only at or above elite

            Vector3 pos = entity.GetWorldPosCenter() + offset;

            LightEliteProjectileCharger charger = Instantiate(AetherdaleData.GetAetherdaleData().lightEliteProjectileCharger, pos, Quaternion.identity);
            charger.owner = entity;
        }
    }
}
using Mirror;
using UnityEngine;

public class StormElite : ElementalElite
{
    const float LIGHTNING_BOLT_MIN_INTERVAL = 0.5F;
    const float LIGHTNING_BOLT_MAX_INTERVAL = 1.0F;

    const int MIN_BOLTS = 1;
    const int MAX_BOLTS = 2;

    const float LIGHTNING_RANGE = 30.0F;

    float currentIntervalRemaining = 0.0F;
    AreaOfEffect lightningStrike;

    public override string GetElitePrefix()
    {
        return "Electric";
    }

    public override Element GetElement()
    {
        return Element.Storm;
    }

    public override void Start()
    {
        base.Start();

        lightningStrike = AetherdaleData.GetAetherdaleData().lightningStrikeAOE;
    }

    void Update()
    {
        if (NetworkServer.active)
        {
            if (currentIntervalRemaining <= 0)
            {
                CallLightning();
                currentIntervalRemaining = Random.Range(LIGHTNING_BOLT_MIN_INTERVAL, LIGHTNING_BOLT_MAX_INTERVAL);
            }
            else
            {
                currentIntervalRemaining -= Time.deltaTime;
            }
        }
    }

    [Server]
    void CallLightning()
    {
        int boltsToCall = Random.Range(MIN_BOLTS, MAX_BOLTS + 1);
        for (int i = 0; i < boltsToCall; i++)
        {
            Vector3 position = new();

            Vector2 unitCirclePos = Random.insideUnitCircle * LIGHTNING_RANGE;

            Vector3 levelPoint = transform.position + new Vector3(unitCirclePos.x, 0, unitCirclePos.y);

            if (Physics.Raycast(levelPoint, Vector3.down, out RaycastHit hit, 80.0F, LayerMask.GetMask("Default", "Entities")))
            {
                position = hit.point;
            }
            else
            {
                position = levelPoint;
            }

            AreaOfEffect.Create(lightningStrike, position, GetComponent<Entity>(), hitType:HitType.Ability);
        }
    }
}
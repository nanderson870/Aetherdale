using Mirror;
using UnityEngine;

public class ShardStar : StatefulCombatEntity
{
    public const float BEAM_POINT_SPEED = 11.0F;
    [SerializeField] Laser mainBeamPrefab;

    [SerializeField] Transform mainBeamPoint;

    float beamLength = 25.0F;
    float beamDuration = 4F;
    float beamCooldown = 8F;
    int beamBaseDamagePerHit = 5;
    float beamHitInterval = 0.5F;


    Laser currentMainBeam;
    float beamCooldownRemaining = 0.0F;

    public override void Update()
    {
        base.Update();

        beamCooldownRemaining -= Time.deltaTime;
    }


    protected override State GetPreferredState()
    {
        State currentState = stateMachine.GetState();
        Entity nearestEnemy = GetPreferredEnemy();

        float distanceToEnemy = Mathf.Infinity;
        if (nearestEnemy != null)
        {
            distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);
        }

        if (currentState is BeamState)
        {
            if (currentState.ReadyForExit())
            {
                // Break out, this will soon be overridden
                return new DormantState(this);
            }
        }
        else
        {
            if (nearestEnemy != null)
            {
                if (beamCooldownRemaining <= 0 && distanceToEnemy <=  0.9F * beamLength)
                {
                    return new BeamState(this, nearestEnemy);
                }
                else
                {
                    return new KeepDistanceState(this, nearestEnemy, 0.75F * beamLength, 0.9F * beamLength);
                }
            }
        }

        return null;
    }


    [ServerCallback]
    void BeamCreate()
    {
        currentMainBeam = Instantiate(mainBeamPrefab);
        NetworkServer.Spawn(currentMainBeam.gameObject);

        currentMainBeam.SetHitData(this, beamBaseDamagePerHit, Element.Water, beamHitInterval, maxLength:beamLength);
    }


    void BeamTeardown()
    {
        if (currentMainBeam != null)
        {
            NetworkServer.UnSpawn(currentMainBeam.gameObject);
            Destroy(currentMainBeam);
        }
    }

    public class BeamState : State
    {
        ShardStar shardStar;
        Entity target;
        
        Vector3 beamTargetPosition = new();

        float startTime;

        public BeamState(ShardStar shardStar, Entity target)
        {
            this.shardStar = shardStar;

            this.target = target;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            shardStar.RpcSetAnimatorBool("firingBeam", true);

            startTime = Time.time;
        }

        public override void Update()
        {
            base.Update();

            if (target == null || !shardStar.SeesEntity(target))
            {
                return;
            }

            beamTargetPosition = Vector3.Lerp(beamTargetPosition, target.GetWorldPosCenter() + (target.GetVelocity() * Time.deltaTime * 0.1F), BEAM_POINT_SPEED * Time.deltaTime);

            if (shardStar.currentMainBeam != null) // entry animation may not be done yet
            {
                shardStar.currentMainBeam.SetPositions(shardStar.mainBeamPoint.position, beamTargetPosition);
            }

            shardStar.TurnTowards(target.gameObject, 240);
        }

        public override bool ReadyForExit()
        {
            return target == null || (Time.time - startTime) > shardStar.beamDuration;
        }

        public override void OnExit()
        {
            base.OnExit();

            shardStar.RpcSetAnimatorBool("firingBeam", false);

            shardStar.beamCooldownRemaining = shardStar.beamCooldown;

            shardStar.BeamTeardown();
        }
    }
}

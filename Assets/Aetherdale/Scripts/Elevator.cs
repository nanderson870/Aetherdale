using System;
using Mirror;
using UnityEngine;

public class Elevator : NetworkBehaviour, IVelocitySource
{
    [SerializeField] Vector3 travelOffset;

    [SerializeField] float lingerDuration = 3.0F;

    [SerializeField] float velocity = 4.0F;

    StateMachine stateMachine = new();


    Vector3 originalOffset;

    public Vector3 direction;

    bool extended = false;

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawCube(transform.position + travelOffset, GetComponent<Collider>().bounds.extents);
    }

    void Start()
    {
        originalOffset = transform.position;

        direction = travelOffset.normalized;
    }

    void Update()
    {
        if (!isServer)
        {
            return;
        }

        EvaluateState();

        stateMachine.Update();
    }

    void EvaluateState()
    {
        State currentState = stateMachine.GetState();
        if (currentState == null)
        {
            stateMachine.ChangeState(new LingerState(this));
            return;
        }

        
        if (currentState.ReadyForExit())
        {
            if (currentState is ExtendState || currentState is RetractState)
            {
                stateMachine.ChangeState(new LingerState(this));
            }
            else
            {
                if (extended)
                {
                    stateMachine.ChangeState(new RetractState(this));
                }
                else
                {
                    stateMachine.ChangeState(new ExtendState(this));
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("collide enter " + other.gameObject);
        if (other.gameObject.TryGetComponent(out ControlledEntity entity))
        {
            //entity.velocitySources.Add(this);
            //Debug.Log("Collision enter " + entity);
            entity.SetAnchoredObject(gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log("collide exit " + other.gameObject);
        if (other.gameObject.TryGetComponent(out ControlledEntity entity))
        {
            //entity.velocitySources.Remove(this);
            //Debug.Log("Collision exit " + entity);
            entity.SetAnchoredObject(null);
        }
    }

    public Vector3 GetVelocityApplied(Entity entity)
    {
        return GetComponent<Rigidbody>().linearVelocity;
    }

    #region STATES
    protected abstract class ElevatorState : State
    {
        public float startTime;
        public Elevator elevator;

        public ElevatorState(Elevator elevator)
        {
            this.elevator = elevator;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            startTime = Time.time;
        }
    }

    protected class ExtendState : ElevatorState
    {
        float stateDuration = 0;

        public ExtendState(Elevator elevator) : base(elevator)
        {
            stateDuration = elevator.travelOffset.magnitude / elevator.velocity;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            elevator.GetComponent<Rigidbody>().linearVelocity = elevator.direction * elevator.velocity;
        }

        public override void Update()
        {
            base.Update();

            float timeInState = Time.time - startTime;

            elevator.transform.position = elevator.originalOffset + ((timeInState / stateDuration) * elevator.travelOffset);
        }

        public override bool ReadyForExit()
        {
            return Time.time - startTime >= stateDuration;
        }

        public override void OnExit()
        {
            elevator.extended = true;
            elevator.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }

    protected class RetractState : ElevatorState
    {
        float stateDuration = 0;

        public RetractState(Elevator elevator) : base(elevator)
        {
            stateDuration = elevator.travelOffset.magnitude / elevator.velocity;
            
        }

        public override void OnEnter()
        {
            base.OnEnter();

            elevator.GetComponent<Rigidbody>().linearVelocity = -elevator.direction * elevator.velocity;
        }

        public override void Update()
        {
            base.Update();

            float timeInState = Time.time - startTime;

            elevator.transform.position = elevator.originalOffset + elevator.travelOffset -  ((timeInState / stateDuration) * elevator.travelOffset);
        }

        public override bool ReadyForExit()
        {
            return Time.time - startTime >= stateDuration;
        }

        public override void OnExit()
        {
            elevator.extended = false;
            elevator.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }

    protected class LingerState : ElevatorState
    {
        public LingerState(Elevator elevator) : base(elevator)
        {
        }

        public override bool ReadyForExit()
        {
            return Time.time - startTime >= elevator.lingerDuration;
        }
    }
    #endregion
}

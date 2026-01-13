using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AttackAnimBehaviour : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (NetworkServer.active)
        {
            animator.SetBool("attacking", true);

            if (animator.gameObject.TryGetComponent<Entity>(out var owningEntity))
            {
                owningEntity.attacking = true;
            }
            else
            {
                Debug.LogWarning("No entity found attached to this animation behavior's animator (" + animator.gameObject + ")");
            }
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}

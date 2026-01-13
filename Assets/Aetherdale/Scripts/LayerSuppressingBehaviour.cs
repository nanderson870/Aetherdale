using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerSuppressingBehaviour : StateMachineBehaviour
{
    [SerializeField] string layerToSuppress;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int suppressedLayerIndex = animator.GetLayerIndex(layerToSuppress);
        animator.SetLayerWeight(suppressedLayerIndex, 0);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int suppressedLayerIndex = animator.GetLayerIndex(layerToSuppress);
        animator.SetLayerWeight(suppressedLayerIndex, 0);
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int suppressedLayerIndex = animator.GetLayerIndex(layerToSuppress);
        animator.SetLayerWeight(suppressedLayerIndex, 1);
    }
}

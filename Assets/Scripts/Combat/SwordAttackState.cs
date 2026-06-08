using UnityEngine;

public class SwordAttackState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsAttacking", true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("IsAttacking", false);
        animator.ResetTrigger("LightAttack");
        animator.ResetTrigger("HeavyAttack");
    }
}

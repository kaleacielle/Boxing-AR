using UnityEngine;

public class CoachManager : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayIdle()
    {
        animator.ResetTrigger("PlayLeadJab");
        animator.ResetTrigger("PlayComboPunch");

        animator.SetTrigger("PlayIdle");
    }

    public void PlayLeadJab()
    {
        animator.ResetTrigger("PlayIdle");
        animator.ResetTrigger("PlayComboPunch");

        animator.SetTrigger("PlayLeadJab");
    }

    public void PlayComboPunch()
    {
        animator.ResetTrigger("PlayIdle");
        animator.ResetTrigger("PlayLeadJab");

        animator.SetTrigger("PlayComboPunch");
    }
}
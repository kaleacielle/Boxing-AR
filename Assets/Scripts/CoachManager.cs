using UnityEngine;
using System;

public class CoachManager : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError(" CoachManager: Animator not found!");
        }
    }

    public void PlayIdle()
    {
        Debug.Log(
            " CoachManager.PlayIdle() CALLED\n" +
            Environment.StackTrace
        );

        animator.ResetTrigger("PlayLeadJab");
        animator.ResetTrigger("PlayComboPunch");

        animator.SetTrigger("PlayIdle");
    }

    public void PlayLeadJab()
    {
        Debug.LogWarning(
            " CoachManager.PlayLeadJab() CALLED\n" +
            Environment.StackTrace
        );

        animator.ResetTrigger("PlayIdle");
        animator.ResetTrigger("PlayComboPunch");

        animator.SetTrigger("PlayLeadJab");
    }

    public void PlayComboPunch()
    {
        Debug.LogWarning(
            " CoachManager.PlayComboPunch() CALLED\n" +
            Environment.StackTrace
        );

        animator.ResetTrigger("PlayIdle");
        animator.ResetTrigger("PlayLeadJab");

        animator.SetTrigger("PlayComboPunch");
    }
}
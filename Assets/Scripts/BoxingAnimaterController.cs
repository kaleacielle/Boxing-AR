using UnityEngine;

public class BoxerAnimatorController : MonoBehaviour
{
    private Animator animator;

    private string currentPose = "";

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        string pose = UDPReceiver.latestPose;

        if (pose == currentPose)
            return;

        currentPose = pose;

        switch (pose)
        {
            case "Guard":
                animator.Play("Idle");
                break;

            case "LeadJab":
                animator.Play("Lead Jab");
                break;

            case "BodyJab":
                animator.Play("Body Jab");
                break;

            case "Combo":
                animator.Play("Combo Punch");
                break;

            default:
                animator.Play("Idle");
                break;
        }
    }
}
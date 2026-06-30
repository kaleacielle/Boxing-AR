using UnityEngine;

public enum LessonState
{
    WaitingForPlayer,
    Guard,
    LeadJab,
    ComboPunch,
    Finished
}

public class GameManager : MonoBehaviour
{
    public CoachManager coach;
    public PoseDetector poseDetector;

    public LessonState currentLesson =
        LessonState.WaitingForPlayer;

    void Start()
    {
        currentLesson = LessonState.WaitingForPlayer;
    }

    void Update()
    {
        switch(currentLesson)
        {
            case LessonState.WaitingForPlayer:
                WaitingForPlayer();
                break;

            case LessonState.Guard:
                GuardLesson();
                break;

            case LessonState.LeadJab:
                LeadJabLesson();
                break;

            case LessonState.ComboPunch:
                ComboPunchLesson();
                break;
        }
    }

    void WaitingForPlayer()
    {
        if (UDPReceiver.bodyDetected)
        {
            Debug.Log("Player Detected");

            coach.PlayIdle();

            currentLesson = LessonState.Guard;
        }
    }

    void GuardLesson()
    {
        if (poseDetector.CurrentPose == BoxingPose.Guard)
        {
            Debug.Log("Great Guard!");

            coach.PlayLeadJab();

            currentLesson = LessonState.LeadJab;
        }
    }

    void LeadJabLesson()
    {

    }

    void ComboPunchLesson()
    {

    }
}
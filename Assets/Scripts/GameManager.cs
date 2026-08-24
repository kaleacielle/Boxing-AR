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
    [Header("References")]
    public CoachManager coach;
    public PoseDetector poseDetector;
    public UIManager uiManager;

    private PoseComparisonManager poseComparisonManager;

    [Header("Lesson")]
    public LessonState currentLesson = LessonState.WaitingForPlayer;

    private const int totalLessons = 3;

    void Start()
    {
        poseComparisonManager = FindFirstObjectByType<PoseComparisonManager>();
        currentLesson = LessonState.WaitingForPlayer;

        uiManager.SetLesson("WAITING...");
        uiManager.SetFeedback("Stand in front of the camera");
        uiManager.SetProgress(0, totalLessons);
    }

    void Update()
    {
        if (poseComparisonManager != null && !poseComparisonManager.IsExperienceActive)
        {
            currentLesson = LessonState.WaitingForPlayer;
            return;
        }

        switch (currentLesson)
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
            coach.PlayIdle();

            uiManager.SetLesson("GUARD");
            uiManager.SetFeedback("Raise both hands to your face.");
            uiManager.SetProgress(1, totalLessons);

            currentLesson = LessonState.Guard;
        }
    }

    void GuardLesson()
    {
        if (poseDetector.CurrentPose == BoxingPose.Guard &&
            HasPassedPoseScoreThreshold())
        {
            coach.PlayLeadJab();

            uiManager.SetLesson("LEAD JAB");
            uiManager.SetFeedback("Great Guard! Extend your left arm.");
            uiManager.SetProgress(2, totalLessons);

            currentLesson = LessonState.LeadJab;
        }
    }

    void LeadJabLesson()
    {
        if (poseDetector.CurrentPose == BoxingPose.LeadJab &&
            HasPassedPoseScoreThreshold())
        {
            coach.PlayComboPunch();

            uiManager.SetLesson("COMBINATION");
            uiManager.SetFeedback("Excellent! Watch the coach.");
            uiManager.SetProgress(3, totalLessons);

            currentLesson = LessonState.ComboPunch;
        }
    }

    void ComboPunchLesson()
    {
        uiManager.SetFeedback("Lesson Complete! Great Work!");

        currentLesson = LessonState.Finished;
    }

    bool HasPassedPoseScoreThreshold()
    {
        return poseComparisonManager != null &&
            poseComparisonManager.currentScore >=
            poseComparisonManager.completionScore;
    }
}
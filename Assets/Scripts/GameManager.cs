using System.Collections;
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

    [Header("Countdown Between Poses")]
    [Range(0.1f, 3f)]
    public float messageDuration = 1f;

    [Range(0.1f, 3f)]
    public float countdownDuration = 1f;

    [Range(0.1f, 3f)]
    public float matchPoseDuration = 1f;

    private const int totalLessons = 3;

    private bool isTransitioning = false;

    void Start()
    {
        poseComparisonManager =
            FindFirstObjectByType<PoseComparisonManager>();

        currentLesson = LessonState.WaitingForPlayer;

        uiManager.SetLesson("WAITING...");
        uiManager.SetFeedback("Stand in front of the camera");
        uiManager.SetProgress(0, totalLessons);
    }

    void Update()
    {
        // Prevent pose detection while countdown is playing
        if (isTransitioning)
            return;

        if (
            poseComparisonManager != null &&
            !poseComparisonManager.IsExperienceActive
        )
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

    // --------------------------------------------------
    // WAITING
    // --------------------------------------------------

    void WaitingForPlayer()
    {
        if (UDPReceiver.bodyDetected)
        {
            coach.PlayIdle();

            uiManager.SetLesson("GUARD");
            uiManager.SetFeedback(
                "Raise both hands to your face."
            );

            uiManager.SetProgress(
                1,
                totalLessons
            );

            currentLesson = LessonState.Guard;
        }
    }

    // --------------------------------------------------
    // GUARD
    // --------------------------------------------------

    void GuardLesson()
    {
        if (
            poseDetector.CurrentPose ==
            BoxingPose.Guard
        )
        {
            isTransitioning = true;

            StartCoroutine(
                CountdownToLeadJab()
            );
        }
    }

    IEnumerator CountdownToLeadJab()
    {
        // Hide normal coaching UI
        uiManager.SetCoachingUIVisible(false);
        uiManager.SetPoseScoreVisible(false);

        // COMPLETED
        uiManager.ShowReadyMessage(
            "COMPLETED!"
        );

        yield return new WaitForSeconds(
            messageDuration
        );

        // NEXT POSE
        uiManager.ShowReadyMessage(
            "NEXT POSE"
        );

        yield return new WaitForSeconds(
            messageDuration
        );

        // 3
        uiManager.ShowReadyMessage("3");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // 2
        uiManager.ShowReadyMessage("2");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // 1
        uiManager.ShowReadyMessage("1");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // Change coach to Lead Jab
        coach.PlayLeadJab();

        // Update lesson before player starts
        uiManager.SetLesson(
            "LEAD JAB"
        );

        uiManager.SetProgress(
            2,
            totalLessons
        );

        // MATCH THE POSE
        uiManager.ShowReadyMessage(
            "MATCH THE POSE!"
        );

        yield return new WaitForSeconds(
            matchPoseDuration
        );

        // Bring normal UI back
        uiManager.HideReadyMessage();

        uiManager.SetCoachingUIVisible(true);
        uiManager.SetPoseScoreVisible(true);

        uiManager.SetFeedback(
            "Extend your left arm."
        );

        currentLesson =
            LessonState.LeadJab;

        isTransitioning = false;
    }

    // --------------------------------------------------
    // LEAD JAB
    // --------------------------------------------------

    void LeadJabLesson()
    {
        if (
            poseDetector.CurrentPose ==
            BoxingPose.LeadJab
        )
        {
            isTransitioning = true;

            StartCoroutine(
                CountdownToComboPunch()
            );
        }
    }

    IEnumerator CountdownToComboPunch()
    {
        // Hide normal coaching UI
        uiManager.SetCoachingUIVisible(false);
        uiManager.SetPoseScoreVisible(false);

        // COMPLETED
        uiManager.ShowReadyMessage(
            "COMPLETED!"
        );

        yield return new WaitForSeconds(
            messageDuration
        );

        // NEXT POSE
        uiManager.ShowReadyMessage(
            "NEXT POSE"
        );

        yield return new WaitForSeconds(
            messageDuration
        );

        // 3
        uiManager.ShowReadyMessage("3");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // 2
        uiManager.ShowReadyMessage("2");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // 1
        uiManager.ShowReadyMessage("1");

        yield return new WaitForSeconds(
            countdownDuration
        );

        // Change coach to Combo
        coach.PlayComboPunch();

        // Update lesson
        uiManager.SetLesson(
            "COMBINATION"
        );

        uiManager.SetProgress(
            3,
            totalLessons
        );

        // MATCH THE POSE
        uiManager.ShowReadyMessage(
            "MATCH THE POSE!"
        );

        yield return new WaitForSeconds(
            matchPoseDuration
        );

        // Bring normal UI back
        uiManager.HideReadyMessage();

        uiManager.SetCoachingUIVisible(true);
        uiManager.SetPoseScoreVisible(true);

        uiManager.SetFeedback(
            "Copy the coach's combination."
        );

        currentLesson =
            LessonState.ComboPunch;

        isTransitioning = false;
    }

    // --------------------------------------------------
    // COMBINATION
    // --------------------------------------------------

    void ComboPunchLesson()
    {
        uiManager.SetFeedback(
            "Lesson Complete! Great Work!"
        );

        currentLesson =
            LessonState.Finished;
    }
}
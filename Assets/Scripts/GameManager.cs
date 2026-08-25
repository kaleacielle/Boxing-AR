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
    public UIManager uiManager;
    public PoseComparisonManager poseComparisonManager;

    [Header("Lesson")]
    public LessonState currentLesson = LessonState.WaitingForPlayer;

    private void Start()
    {
        if (poseComparisonManager == null)
        {
            poseComparisonManager =
                FindFirstObjectByType<PoseComparisonManager>();
        }

        currentLesson =
            LessonState.WaitingForPlayer;

        // Do NOT start coach animations here.
        // PoseComparisonManager handles the whole lesson flow.
    }

    private void Update()
    {
        // GameManager intentionally does not control
        // coach animations or pose progression anymore.

        // PoseComparisonManager is now the single authority for:
        //
        // Wave
        // ↓
        // Countdown
        // ↓
        // Guard
        // ↓
        // 100%
        // ↓
        // Countdown
        // ↓
        // Lead Jab
        // ↓
        // 100%
        // ↓
        // Countdown
        // ↓
        // Combination
    }

    public void SetLessonState(
        LessonState newState
    )
    {
        currentLesson = newState;
    }

    public void ResetLessonState()
    {
        currentLesson =
            LessonState.WaitingForPlayer;
    }
}
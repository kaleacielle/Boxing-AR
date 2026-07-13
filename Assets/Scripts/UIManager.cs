using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Lesson UI")]
    public TMP_Text lessonText;
    public TMP_Text feedbackText;
    public TMP_Text progressText;

    [Header("Pose Matching UI")]
    public TMP_Text poseScoreText;
    public TMP_Text hintText;

    public void SetLesson(string lesson)
    {
        if (lessonText != null)
            lessonText.text = lesson;
    }

    public void SetFeedback(string feedback)
    {
        if (feedbackText != null)
            feedbackText.text = feedback;
    }

    public void SetProgress(int current, int total)
    {
        if (progressText != null)
            progressText.text = $"Lesson {current} / {total}";
    }

    public void SetPoseScore(float score)
    {
        if (poseScoreText != null)
            poseScoreText.text = $"Pose Match\n{Mathf.RoundToInt(score)}%";
    }

    public void SetHint(string hint)
    {
        if (hintText != null)
            hintText.text = hint;
    }
}
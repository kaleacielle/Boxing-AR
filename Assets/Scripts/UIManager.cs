using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text lessonText;
    public TMP_Text feedbackText;
    public TMP_Text progressText;

    public void SetLesson(string lesson)
    {
        lessonText.text = lesson;
    }

    public void SetFeedback(string feedback)
    {
        feedbackText.text = feedback;
    }

    public void SetProgress(int current, int total)
    {
        progressText.text = $"Lesson {current} / {total}";
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Lesson UI")]
    public TMP_Text lessonText;
    public TMP_Text feedbackText;
    public TMP_Text progressText;

    [Header("Pose Matching UI")]
    public TMP_Text poseScoreText;
    public Slider poseScoreBar;
    public TMP_Text hintText;

    [Header("Start and Countdown UI")]
    [Tooltip("Large text in the centre of the screen.")]
    public TMP_Text readyText;

    [Header("Floating Animation")]
    [Range(0.5f, 2f)]
    public float floatingSpeed = 1f;

    [Range(0f, 40f)]
    public float floatingDistance = 12f;

    [Range(0.8f, 1.5f)]
    public float pulseScale = 1.08f;

    private Vector2 readyTextStartPosition;
    private Vector3 readyTextStartScale;
    private Coroutine readyAnimationCoroutine;

    private void Awake()
    {
        if (readyText != null)
        {
            readyTextStartPosition =
                readyText.rectTransform.anchoredPosition;

            readyTextStartScale =
                readyText.rectTransform.localScale;
        }
    }

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
            progressText.text = $"{current} / {total}";
    }

    public void SetPoseScore(float score)
    {
        if (poseScoreBar != null)
            poseScoreBar.value = Mathf.Clamp(score, 0f, 100f);

        if (poseScoreText != null)
        {
            poseScoreText.text =
                $"Pose Match\n{Mathf.RoundToInt(score)}%";
        }
    }

    public void SetHint(string hint)
    {
        if (hintText != null)
            hintText.text = hint;
    }

    public void ShowReadyMessage(string message)
    {
        if (readyText == null)
            return;

        readyText.text = message;
        readyText.gameObject.SetActive(true);

        if (readyAnimationCoroutine != null)
            StopCoroutine(readyAnimationCoroutine);

        readyAnimationCoroutine =
            StartCoroutine(AnimateReadyText());
    }

    public void HideReadyMessage()
    {
        if (readyAnimationCoroutine != null)
        {
            StopCoroutine(readyAnimationCoroutine);
            readyAnimationCoroutine = null;
        }

        if (readyText != null)
        {
            readyText.rectTransform.anchoredPosition =
                readyTextStartPosition;

            readyText.rectTransform.localScale =
                readyTextStartScale;

            readyText.gameObject.SetActive(false);
        }
    }

    public void SetPoseScoreVisible(bool visible)
    {
        if (poseScoreText != null)
            poseScoreText.gameObject.SetActive(visible);

        if (poseScoreBar != null)
            poseScoreBar.gameObject.SetActive(visible);
    }

    public void SetCoachingUIVisible(bool visible)
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(visible);

        if (hintText != null)
            hintText.gameObject.SetActive(visible);
    }

    private IEnumerator AnimateReadyText()
    {
        float timer = 0f;

        while (readyText != null && readyText.gameObject.activeSelf)
        {
            timer += Time.deltaTime * floatingSpeed;

            float verticalMovement =
                Mathf.Sin(timer * Mathf.PI * 2f) *
                floatingDistance;

            float scaleAmount =
                Mathf.Lerp(
                    1f,
                    pulseScale,
                    (Mathf.Sin(timer * Mathf.PI * 2f) + 1f) * 0.5f
                );

            readyText.rectTransform.anchoredPosition =
                readyTextStartPosition +
                Vector2.up * verticalMovement;

            readyText.rectTransform.localScale =
                readyTextStartScale * scaleAmount;

            yield return null;
        }
    }
}
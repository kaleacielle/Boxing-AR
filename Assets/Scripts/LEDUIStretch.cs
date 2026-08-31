using UnityEngine;

public class LEDUIStretch : MonoBehaviour
{
    [Header("Original design resolution")]
    public float originalWidth = 1920f;

    [Header("Visible LED section")]
    public float visibleWidth = 640f;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // 1920 / 640 = 3
        float horizontalStretch = originalWidth / visibleWidth;

        // Stretch UI horizontally the same way as LED_Output
        rectTransform.localScale = new Vector3(
            horizontalStretch,
            1f,
            1f
        );
    }
}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class LEDOutputStretch : MonoBehaviour
{
    private RectTransform rt;
    private RawImage rawImage;

    private void Awake()
    {
        ApplyStretch();
    }

    private void Start()
    {
        Screen.SetResolution(
            1920,
            1080,
            FullScreenMode.FullScreenWindow
        );

        ApplyStretch();
    }

    private void OnEnable()
    {
        ApplyStretch();
    }

    private void ApplyStretch()
    {
        rt = GetComponent<RectTransform>();
        rawImage = GetComponent<RawImage>();

        // Stretch RawImage over the whole Canvas
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;

        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;

        // Use the entire LED_Output texture
        rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
    }
}
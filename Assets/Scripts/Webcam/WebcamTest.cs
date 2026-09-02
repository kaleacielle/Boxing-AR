using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class WebcamTest : MonoBehaviour
{
    private const string PreferredCameraKey = "BoxingAR.PreferredCamera";

    public static event System.Action<int, string> SelectedCameraChanged;

    public RawImage webcamImage;

    [Header("Camera capture")]
    [Min(1)] public int requestedWidth = 1280;
    [Min(1)] public int requestedHeight = 720;
    [Range(1, 120)] public int requestedFPS = 30;
    public bool mirrorHorizontally = true;

    [Header("Pose tracking frame relay")]
    [Min(1)] public int trackingFramePort = 5053;
    [Range(160, 640)] public int trackingFrameWidth = 480;
    [Range(90, 480)] public int trackingFrameHeight = 270;
    [Range(1, 30)] public int trackingFramesPerSecond = 12;
    [Range(20, 90)] public int trackingJpegQuality = 55;

    private readonly List<WebCamDevice> availableDevices = new List<WebCamDevice>();
    private WebCamTexture webcamTexture;
    private Text cameraNameLabel;
    private int selectedDeviceIndex = -1;
    private UdpClient trackingFrameSender;
    private Texture2D trackingFrameTexture;
    private Coroutine trackingFrameCoroutine;

    private IEnumerator Start()
    {
        CreateCameraSelectorUI();

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }

        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            SetStatus("Camera permission denied");
            yield break;
        }

        RefreshDevicesAndSelectSavedCamera();
        trackingFrameSender = new UdpClient();
        trackingFrameCoroutine = StartCoroutine(SendTrackingFrames());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SelectNextCamera();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RefreshDevicesAndSelectSavedCamera();
        }
    }

    public void SelectPreviousCamera()
    {
        CycleCamera(-1);
    }

    public void SelectNextCamera()
    {
        CycleCamera(1);
    }

    public void RefreshDevicesAndSelectSavedCamera()
    {
        string preferredDevice = PlayerPrefs.GetString(PreferredCameraKey, string.Empty);
        RefreshDeviceList();

        if (availableDevices.Count == 0)
        {
            selectedDeviceIndex = -1;
            StopCamera();
            SetStatus("No camera found - press R to retry");
            return;
        }

        selectedDeviceIndex = 0;
        if (!string.IsNullOrEmpty(preferredDevice))
        {
            int savedIndex = availableDevices.FindIndex(device => device.name == preferredDevice);
            if (savedIndex >= 0)
            {
                selectedDeviceIndex = savedIndex;
            }
        }

        StartSelectedCamera();
    }

    private void CycleCamera(int direction)
    {
        string currentDevice = GetCurrentDeviceName();
        RefreshDeviceList();

        if (availableDevices.Count == 0)
        {
            selectedDeviceIndex = -1;
            StopCamera();
            SetStatus("No camera found - press R to retry");
            return;
        }

        int currentIndex = availableDevices.FindIndex(device => device.name == currentDevice);
        if (currentIndex < 0)
        {
            currentIndex = Mathf.Clamp(selectedDeviceIndex, 0, availableDevices.Count - 1);
        }

        selectedDeviceIndex = (currentIndex + direction) % availableDevices.Count;
        if (selectedDeviceIndex < 0)
        {
            selectedDeviceIndex += availableDevices.Count;
        }

        StartSelectedCamera();
    }

    private void RefreshDeviceList()
    {
        availableDevices.Clear();
        availableDevices.AddRange(WebCamTexture.devices);
    }

    private void StartSelectedCamera()
    {
        if (selectedDeviceIndex < 0 || selectedDeviceIndex >= availableDevices.Count)
        {
            return;
        }

        StopCamera();

        WebCamDevice selectedDevice = availableDevices[selectedDeviceIndex];
        webcamTexture = new WebCamTexture(
            selectedDevice.name,
            requestedWidth,
            requestedHeight,
            requestedFPS);

        if (webcamImage != null)
        {
            webcamImage.texture = webcamTexture;
            webcamImage.uvRect = mirrorHorizontally
                ? new Rect(1f, 0f, -1f, 1f)
                : new Rect(0f, 0f, 1f, 1f);
        }

        webcamTexture.Play();
        PlayerPrefs.SetString(PreferredCameraKey, selectedDevice.name);
        PlayerPrefs.Save();
        SelectedCameraChanged?.Invoke(selectedDeviceIndex, selectedDevice.name);
        SetStatus($"{selectedDevice.name}  ({selectedDeviceIndex + 1}/{availableDevices.Count})");
        Debug.Log($"Webcam selected: {selectedDevice.name}");
    }

    private string GetCurrentDeviceName()
    {
        return webcamTexture != null ? webcamTexture.deviceName : string.Empty;
    }

    private IEnumerator SendTrackingFrames()
    {
        while (true)
        {
            float interval = 1f / Mathf.Max(1, trackingFramesPerSecond);

            if (webcamTexture != null &&
                webcamTexture.isPlaying &&
                webcamTexture.didUpdateThisFrame &&
                webcamTexture.width > 16)
            {
                yield return new WaitForEndOfFrame();
                SendTrackingFrame();
            }

            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private void SendTrackingFrame()
    {
        int width = Mathf.Max(160, trackingFrameWidth);
        int height = Mathf.Max(90, trackingFrameHeight);
        RenderTexture temporary = RenderTexture.GetTemporary(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(webcamTexture, temporary);
            RenderTexture.active = temporary;

            if (trackingFrameTexture == null ||
                trackingFrameTexture.width != width ||
                trackingFrameTexture.height != height)
            {
                if (trackingFrameTexture != null)
                    Destroy(trackingFrameTexture);

                trackingFrameTexture = new Texture2D(
                    width,
                    height,
                    TextureFormat.RGB24,
                    false);
            }

            trackingFrameTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            trackingFrameTexture.Apply(false, false);
            byte[] jpeg = trackingFrameTexture.EncodeToJPG(trackingJpegQuality);

            // A UDP datagram cannot exceed 65,507 bytes.
            if (jpeg.Length <= 60000)
            {
                trackingFrameSender.Send(jpeg, jpeg.Length, "127.0.0.1", trackingFramePort);
            }
            else
            {
                Debug.LogWarning("Tracking frame was too large and was skipped.", this);
            }
        }
        catch (SocketException exception)
        {
            Debug.LogWarning($"Could not send a tracking frame: {exception.Message}", this);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private void StopCamera()
    {
        if (webcamTexture == null)
        {
            return;
        }

        if (webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        Destroy(webcamTexture);
        webcamTexture = null;
    }

    private void OnDestroy()
    {
        if (trackingFrameCoroutine != null)
            StopCoroutine(trackingFrameCoroutine);

        trackingFrameSender?.Close();
        trackingFrameSender = null;

        if (trackingFrameTexture != null)
            Destroy(trackingFrameTexture);

        StopCamera();
    }

    private void CreateCameraSelectorUI()
    {
        if (webcamImage == null)
        {
            Debug.LogError("WebcamTest requires a RawImage reference.", this);
            return;
        }

        Transform existing = webcamImage.transform.Find("CameraSelector");
        if (existing != null)
        {
            cameraNameLabel = existing.GetComponentInChildren<Text>(true);
            return;
        }

        GameObject panelObject = new GameObject(
            "CameraSelector",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(webcamImage.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 42f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);
        panelImage.raycastTarget = true;

        CreateSelectorButton(panelRect, "PreviousCamera", "<", true, SelectPreviousCamera);
        CreateSelectorButton(panelRect, "NextCamera", ">", false, SelectNextCamera);

        GameObject labelObject = new GameObject(
            "CameraName",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        labelObject.transform.SetParent(panelRect, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(46f, 2f);
        labelRect.offsetMax = new Vector2(-46f, -2f);

        cameraNameLabel = labelObject.GetComponent<Text>();
        cameraNameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cameraNameLabel.fontSize = 16;
        cameraNameLabel.alignment = TextAnchor.MiddleCenter;
        cameraNameLabel.color = Color.white;
        cameraNameLabel.raycastTarget = false;
        cameraNameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        cameraNameLabel.verticalOverflow = VerticalWrapMode.Truncate;
        SetStatus("Finding cameras...");
    }

    private static void CreateSelectorButton(
        RectTransform parent,
        string objectName,
        string label,
        bool alignLeft,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        float edge = alignLeft ? 0f : 1f;
        buttonRect.anchorMin = new Vector2(edge, 0f);
        buttonRect.anchorMax = new Vector2(edge, 1f);
        buttonRect.pivot = new Vector2(edge, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(42f, 0f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        GameObject textObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        textObject.transform.SetParent(buttonRect, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text buttonText = textObject.GetComponent<Text>();
        buttonText.text = label;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 22;
        buttonText.fontStyle = FontStyle.Bold;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        buttonText.raycastTarget = false;
    }

    private void SetStatus(string message)
    {
        if (cameraNameLabel != null)
        {
            cameraNameLabel.text = message;
        }
    }
}

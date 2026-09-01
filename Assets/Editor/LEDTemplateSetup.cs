using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LEDTemplateSetup
{
    public const int DesignWidth = 608;
    public const int DesignHeight = 1080;
    public const int TallDesignWidth = 640;
    public const int TallDesignHeight = 1920;
    public const int OutputWidth = 1920;
    public const int OutputHeight = 1080;

    public const string ScenePath = "Assets/Scenes/LEDTemplateTest.unity";
    public const string RenderTexturePath = "Assets/RenderTextures/LED_Output.renderTexture";
    public const string TallScenePath = "Assets/Scenes/LEDTemplateTest_640x1920.unity";
    public const string TallRenderTexturePath = "Assets/RenderTextures/LED_Output_640x1920.renderTexture";
    public const string TestBackgroundPath = "Assets/UI/TestBackground.png";

    [MenuItem("Tools/Toshiba LED/Create Templates/Create 608x1080 Template")]
    public static void CreateTestTemplate()
    {
        CreateTemplate(DesignWidth, DesignHeight, ScenePath, RenderTexturePath, "LED_Output");
    }

    [MenuItem("Tools/Toshiba LED/Create Test Template")]
    public static void CreateOriginalTestTemplateMenu()
    {
        CreateTestTemplate();
    }

    [MenuItem("Tools/Toshiba LED/Create Templates/Create 640x1920 Template")]
    public static void CreateTallTestTemplate()
    {
        CreateTemplate(
            TallDesignWidth,
            TallDesignHeight,
            TallScenePath,
            TallRenderTexturePath,
            "LED_Output_640x1920");
    }

    [MenuItem("Tools/Toshiba LED/Create Templates/Create Both Templates")]
    public static void CreateBothTemplates()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        CreateTemplate(DesignWidth, DesignHeight, ScenePath, RenderTexturePath, "LED_Output", false);
        CreateTemplate(
            TallDesignWidth,
            TallDesignHeight,
            TallScenePath,
            TallRenderTexturePath,
            "LED_Output_640x1920",
            false);

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Toshiba LED Templates",
                "Both 608x1080 and 640x1920 templates are ready.",
                "OK");
        }
    }

    private static void CreateTemplate(
        int designWidth,
        int designHeight,
        string scenePath,
        string renderTexturePath,
        string renderTextureName,
        bool askToSave = true)
    {
        if (askToSave && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureAssetFolders();
        Texture2D testBackground = ConfigureTestBackgroundImport();
        RenderTexture ledOutput = CreateOrUpdateRenderTexture(
            designWidth,
            designHeight,
            renderTexturePath,
            renderTextureName);
        Scene scene = OpenOrCreateTemplateScene(scenePath);

        Camera captureCamera = ConfigureCaptureCamera(scene, ledOutput, designWidth, designHeight);
        ConfigureCaptureCanvas(scene, captureCamera, designWidth, designHeight, testBackground);
        ConfigureOutputCanvas(scene, ledOutput);
        ConfigureWindowsOutput();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AddSceneToBuildSettings(scenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

        Debug.Log(
            $"Toshiba LED test template created. The complete {designWidth}x{designHeight} capture is stretched to the " +
            "1920x1080 output without preserving aspect ratio.");

        if (askToSave && !Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Toshiba LED Template",
                $"{Path.GetFileNameWithoutExtension(scenePath)} is ready.\n\n" +
                "Use Tools > Toshiba LED > Open LED Preview to compare the portrait design and stretched HDMI output.",
                "OK");
        }
    }

    [MenuItem("Tools/Toshiba LED/Open Test Scene")]
    public static void OpenTestScene()
    {
        if (!File.Exists(ScenePath))
        {
            CreateTestTemplate();
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("Tools/Toshiba LED/Open 640x1920 Test Scene")]
    public static void OpenTallTestScene()
    {
        OpenOrCreateSceneFromMenu(TallScenePath, CreateTallTestTemplate);
    }

    private static void OpenOrCreateSceneFromMenu(string scenePath, System.Action createAction)
    {
        if (!File.Exists(scenePath))
        {
            createAction();
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private static void EnsureAssetFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        if (!AssetDatabase.IsValidFolder("Assets/RenderTextures"))
        {
            AssetDatabase.CreateFolder("Assets", "RenderTextures");
        }

        if (!AssetDatabase.IsValidFolder("Assets/UI"))
        {
            AssetDatabase.CreateFolder("Assets", "UI");
        }
    }

    private static Texture2D ConfigureTestBackgroundImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TestBackgroundPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        bool requiresImport =
            importer.textureType != TextureImporterType.Default ||
            importer.mipmapEnabled ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.maxTextureSize < 2048;

        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;

        if (requiresImport)
        {
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(TestBackgroundPath);
    }

    private static RenderTexture CreateOrUpdateRenderTexture(
        int designWidth,
        int designHeight,
        string renderTexturePath,
        string renderTextureName)
    {
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderTexturePath);
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(designWidth, designHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = renderTextureName
            };
            AssetDatabase.CreateAsset(renderTexture, renderTexturePath);
        }
        else
        {
            renderTexture.Release();
            renderTexture.width = designWidth;
            renderTexture.height = designHeight;
            renderTexture.depth = 24;
            renderTexture.format = RenderTextureFormat.ARGB32;
        }

        renderTexture.antiAliasing = 1;
        renderTexture.useMipMap = false;
        renderTexture.autoGenerateMips = false;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.anisoLevel = 0;
        EditorUtility.SetDirty(renderTexture);
        return renderTexture;
    }

    private static Scene OpenOrCreateTemplateScene(string scenePath)
    {
        if (File.Exists(scenePath))
        {
            return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    private static Camera ConfigureCaptureCamera(
        Scene scene,
        RenderTexture ledOutput,
        int designWidth,
        int designHeight)
    {
        GameObject cameraObject = GetOrCreateRoot(scene, "CaptureCamera");
        Camera camera = GetOrAddComponent<Camera>(cameraObject);
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
        cameraObject.transform.localScale = Vector3.one;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.018f, 0.022f, 0.03f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.aspect = (float)designWidth / designHeight;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        camera.depth = 0f;
        camera.targetTexture = ledOutput;
        camera.targetDisplay = 0;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.useOcclusionCulling = false;
        camera.enabled = true;

        AudioListener listener = cameraObject.GetComponent<AudioListener>();
        if (listener != null)
        {
            Object.DestroyImmediate(listener);
        }

        return camera;
    }

    private static void ConfigureCaptureCanvas(
        Scene scene,
        Camera captureCamera,
        int designWidth,
        int designHeight,
        Texture2D testBackground)
    {
        GameObject canvasObject = GetOrCreateRoot(scene, "CaptureCanvas");
        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = captureCamera;
        canvas.planeDistance = 1f;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 0;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(designWidth, designHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;

        GameObject backgroundObject = GetOrCreateUIChild(canvasObject.transform, "Background");
        RectTransform backgroundRect = GetOrAddComponent<RectTransform>(backgroundObject);
        StretchToParent(backgroundRect);
        Image oldBackgroundImage = backgroundObject.GetComponent<Image>();
        if (oldBackgroundImage != null)
        {
            Object.DestroyImmediate(oldBackgroundImage);
        }

        RawImage background = GetOrAddComponent<RawImage>(backgroundObject);
        background.texture = testBackground;
        background.color = testBackground != null
            ? Color.white
            : new Color(0.035f, 0.043f, 0.06f, 1f);
        background.raycastTarget = false;
        if (testBackground != null)
        {
            float cropWidth = Mathf.Min(designWidth, testBackground.width);
            float normalizedWidth = cropWidth / testBackground.width;
            background.uvRect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
        }
        else
        {
            background.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
        backgroundRect.SetAsFirstSibling();

        GameObject gameUI = GetOrCreateUIChild(canvasObject.transform, "GameUI");
        StretchToParent(GetOrAddComponent<RectTransform>(gameUI));

        Transform oldTestUI = canvasObject.transform.Find("TestUI");
        if (oldTestUI != null && gameUI.transform.Find("TestUI") == null)
        {
            oldTestUI.SetParent(gameUI.transform, false);
        }

        GameObject testUI = GetOrCreateUIChild(gameUI.transform, "TestUI");
        StretchToParent(GetOrAddComponent<RectTransform>(testUI));
        testUI.SetActive(testBackground == null);

        CreateTitle(testUI.transform);
        CreateMarker(testUI.transform, "TopMarker", "TOP", new Vector2(0.5f, 1f), new Vector2(0f, -170f));
        CreateMarker(testUI.transform, "CenterMarker", "CENTER", new Vector2(0.5f, 0.5f), Vector2.zero);
        CreateMarker(testUI.transform, "BottomMarker", "BOTTOM", new Vector2(0.5f, 0f), new Vector2(0f, 85f));
        CreateBoundary(testUI.transform, "LeftBoundary", true);
        CreateBoundary(testUI.transform, "RightBoundary", false);
    }

    private static void CreateTitle(Transform parent)
    {
        GameObject panelObject = GetOrCreateUIChild(parent, "TitlePanel");
        RectTransform panelRect = GetOrAddComponent<RectTransform>(panelObject);
        panelRect.anchorMin = new Vector2(0.08f, 1f);
        panelRect.anchorMax = new Vector2(0.92f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -40f);
        panelRect.sizeDelta = new Vector2(0f, 105f);
        panelRect.localScale = Vector3.one;

        Image panel = GetOrAddComponent<Image>(panelObject);
        panel.color = new Color(0.035f, 0.55f, 0.72f, 1f);
        panel.raycastTarget = false;

        GameObject textObject = GetOrCreateUIChild(panelObject.transform, "Label");
        RectTransform textRect = GetOrAddComponent<RectTransform>(textObject);
        StretchToParent(textRect, new Vector2(18f, 10f));
        ConfigureText(GetOrAddComponent<Text>(textObject), "TOSHIBA TEST", 46, FontStyle.Bold);
    }

    private static void CreateMarker(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchor,
        Vector2 anchoredPosition)
    {
        GameObject markerObject = GetOrCreateUIChild(parent, objectName);
        RectTransform markerRect = GetOrAddComponent<RectTransform>(markerObject);
        markerRect.anchorMin = anchor;
        markerRect.anchorMax = anchor;
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.anchoredPosition = anchoredPosition;
        markerRect.sizeDelta = new Vector2(300f, 70f);
        markerRect.localScale = Vector3.one;

        Text text = GetOrAddComponent<Text>(markerObject);
        ConfigureText(text, label, 38, FontStyle.Bold);
        text.color = new Color(0.96f, 0.88f, 0.25f, 1f);
    }

    private static void CreateBoundary(Transform parent, string objectName, bool left)
    {
        GameObject boundaryObject = GetOrCreateUIChild(parent, objectName);
        RectTransform boundaryRect = GetOrAddComponent<RectTransform>(boundaryObject);
        float edge = left ? 0f : 1f;
        boundaryRect.anchorMin = new Vector2(edge, 0f);
        boundaryRect.anchorMax = new Vector2(edge, 1f);
        boundaryRect.pivot = new Vector2(edge, 0.5f);
        boundaryRect.anchoredPosition = Vector2.zero;
        boundaryRect.sizeDelta = new Vector2(14f, 0f);
        boundaryRect.localScale = Vector3.one;

        Image image = GetOrAddComponent<Image>(boundaryObject);
        image.color = left
            ? new Color(0.95f, 0.18f, 0.16f, 1f)
            : new Color(0.18f, 0.95f, 0.38f, 1f);
        image.raycastTarget = false;
    }

    private static void ConfigureText(Text text, string value, int fontSize, FontStyle style)
    {
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static void ConfigureOutputCanvas(Scene scene, RenderTexture ledOutput)
    {
        GameObject canvasObject = GetOrCreateRoot(scene, "OutputCanvas");
        Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.targetDisplay = 0;
        canvas.pixelPerfect = false;
        canvas.sortingOrder = 1000;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(OutputWidth, OutputHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject outputObject = GetOrCreateUIChild(canvasObject.transform, "OutputRawImage");
        RectTransform outputRect = GetOrAddComponent<RectTransform>(outputObject);
        StretchToParent(outputRect);
        RawImage outputImage = GetOrAddComponent<RawImage>(outputObject);
        outputImage.texture = ledOutput;
        outputImage.color = Color.white;
        outputImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        outputImage.raycastTarget = false;

        AspectRatioFitter aspectRatioFitter = outputObject.GetComponent<AspectRatioFitter>();
        if (aspectRatioFitter != null)
        {
            Object.DestroyImmediate(aspectRatioFitter);
        }
    }

    private static void ConfigureWindowsOutput()
    {
        PlayerSettings.defaultScreenWidth = OutputWidth;
        PlayerSettings.defaultScreenHeight = OutputHeight;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = false;
        PlayerSettings.runInBackground = true;

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
        }
    }

    [MenuItem("Tools/Toshiba LED/Validate Templates/Validate 608x1080 Template")]
    public static void ValidateTestTemplate()
    {
        ValidateTemplate(DesignWidth, DesignHeight, ScenePath, RenderTexturePath);
    }

    [MenuItem("Tools/Toshiba LED/Validate Templates/Validate 640x1920 Template")]
    public static void ValidateTallTestTemplate()
    {
        ValidateTemplate(TallDesignWidth, TallDesignHeight, TallScenePath, TallRenderTexturePath);
    }

    private static void ValidateTemplate(
        int designWidth,
        int designHeight,
        string scenePath,
        string renderTexturePath)
    {
        int failures = 0;
        string resolution = $"{designWidth}x{designHeight}";
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderTexturePath);
        Check(renderTexture != null, $"{resolution} Render Texture exists.", ref failures);
        if (renderTexture != null)
        {
            Check(
                renderTexture.width == designWidth && renderTexture.height == designHeight,
                $"Render Texture is exactly {resolution}.",
                ref failures);
        }

        Check(File.Exists(scenePath), $"{resolution} test scene exists.", ref failures);
        if (File.Exists(scenePath))
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject captureCameraObject = FindRoot(scene, "CaptureCamera");
            Camera captureCamera = captureCameraObject != null ? captureCameraObject.GetComponent<Camera>() : null;
            Check(
                captureCamera != null && captureCamera.targetTexture == renderTexture,
                "CaptureCamera renders into LED_Output.",
                ref failures);

            GameObject captureCanvasObject = FindRoot(scene, "CaptureCanvas");
            Canvas captureCanvas = captureCanvasObject != null ? captureCanvasObject.GetComponent<Canvas>() : null;
            CanvasScaler captureScaler = captureCanvasObject != null ? captureCanvasObject.GetComponent<CanvasScaler>() : null;
            Check(
                captureCanvas != null &&
                captureCanvas.renderMode == RenderMode.ScreenSpaceCamera &&
                captureCanvas.worldCamera == captureCamera,
                "CaptureCanvas is camera-space UI captured by CaptureCamera.",
                ref failures);
            Check(
                captureScaler != null && captureScaler.referenceResolution == new Vector2(designWidth, designHeight),
                $"CaptureCanvas design reference is {resolution}.",
                ref failures);

            GameObject outputCanvasObject = FindRoot(scene, "OutputCanvas");
            Canvas outputCanvas = outputCanvasObject != null ? outputCanvasObject.GetComponent<Canvas>() : null;
            Transform outputTransform = outputCanvasObject != null
                ? outputCanvasObject.transform.Find("OutputRawImage")
                : null;
            RawImage outputImage = outputTransform != null ? outputTransform.GetComponent<RawImage>() : null;
            RectTransform outputRect = outputTransform as RectTransform;
            Check(
                outputCanvas != null && outputCanvas.renderMode == RenderMode.ScreenSpaceOverlay,
                "OutputCanvas targets the normal display as overlay-only output.",
                ref failures);
            Check(
                outputImage != null && outputImage.texture == renderTexture && outputImage.uvRect == new Rect(0f, 0f, 1f, 1f),
                "OutputRawImage displays the complete LED_Output texture.",
                ref failures);
            Check(
                outputRect != null &&
                outputRect.anchorMin == Vector2.zero &&
                outputRect.anchorMax == Vector2.one &&
                outputRect.offsetMin == Vector2.zero &&
                outputRect.offsetMax == Vector2.zero,
                "OutputRawImage stretches to all four display edges.",
                ref failures);
            Check(
                outputTransform != null && outputTransform.GetComponent<AspectRatioFitter>() == null,
                "OutputRawImage has no aspect-ratio preservation component.",
                ref failures);
        }

        Check(
            PlayerSettings.defaultScreenWidth == OutputWidth &&
            PlayerSettings.defaultScreenHeight == OutputHeight &&
            PlayerSettings.fullScreenMode == FullScreenMode.FullScreenWindow,
            "Windows player defaults to a 1920x1080 fullscreen window.",
            ref failures);
        Check(
            EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64,
            "Active build target is Windows x86-64.",
            ref failures);

        if (failures == 0)
        {
            float horizontalScale = (float)OutputWidth / designWidth;
            Debug.Log(
                $"Toshiba LED {resolution} template validation passed. The frame is intentionally stretched " +
                $"to 1920x1080 ({horizontalScale:0.####}x horizontal scale). Physical LED verification is still required on-site.");
        }
        else
        {
            Debug.LogError(
                $"Toshiba LED {resolution} template validation found {failures} problem(s). " +
                "Run the matching Create Template command and validate again.");
        }
    }

    private static void Check(bool condition, string description, ref int failures)
    {
        if (condition)
        {
            Debug.Log("[Toshiba LED] PASS: " + description);
        }
        else
        {
            failures++;
            Debug.LogError("[Toshiba LED] FAIL: " + description);
        }
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] oldScenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[oldScenes.Length + 1];
        int index = 1;
        newScenes[0] = new EditorBuildSettingsScene(scenePath, true);

        foreach (EditorBuildSettingsScene oldScene in oldScenes)
        {
            if (oldScene.path == scenePath)
            {
                continue;
            }

            newScenes[index++] = oldScene;
        }

        if (index != newScenes.Length)
        {
            System.Array.Resize(ref newScenes, index);
        }

        EditorBuildSettings.scenes = newScenes;
    }

    private static GameObject GetOrCreateRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
            {
                return root;
            }
        }

        GameObject created = new GameObject(objectName);
        SceneManager.MoveGameObjectToScene(created, scene);
        return created;
    }

    private static GameObject GetOrCreateUIChild(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject created = new GameObject(objectName, typeof(RectTransform));
        created.transform.SetParent(parent, false);
        return created;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        StretchToParent(rectTransform, Vector2.zero);
    }

    private static void StretchToParent(RectTransform rectTransform, Vector2 inset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = inset;
        rectTransform.offsetMax = -inset;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }
}

public sealed class LEDOutputPreviewWindow : EditorWindow
{
    private int sourceMode;
    private int previewMode;
    private bool autoRefresh = true;

    [MenuItem("Tools/Toshiba LED/Open LED Preview")]
    public static void Open()
    {
        LEDOutputPreviewWindow window = GetWindow<LEDOutputPreviewWindow>("Toshiba LED Preview");
        window.minSize = new Vector2(430f, 500f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += RepaintIfNeeded;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintIfNeeded;
    }

    private void RepaintIfNeeded()
    {
        if (autoRefresh)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6f);
        GUILayout.Label("Template source", EditorStyles.boldLabel);
        sourceMode = GUILayout.Toolbar(sourceMode, new[] { "608x1080", "640x1920" });
        GUILayout.Label("Preview mode", EditorStyles.boldLabel);
        previewMode = GUILayout.Toolbar(previewMode, new[] { "Design proportions", "HDMI 1920x1080" });

        int designWidth = sourceMode == 0 ? LEDTemplateSetup.DesignWidth : LEDTemplateSetup.TallDesignWidth;
        int designHeight = sourceMode == 0 ? LEDTemplateSetup.DesignHeight : LEDTemplateSetup.TallDesignHeight;
        string renderTexturePath = sourceMode == 0
            ? LEDTemplateSetup.RenderTexturePath
            : LEDTemplateSetup.TallRenderTexturePath;
        string scenePath = sourceMode == 0 ? LEDTemplateSetup.ScenePath : LEDTemplateSetup.TallScenePath;

        using (new EditorGUILayout.HorizontalScope())
        {
            autoRefresh = EditorGUILayout.ToggleLeft("Auto refresh", autoRefresh, GUILayout.Width(110f));
            if (GUILayout.Button("Refresh capture", GUILayout.Width(120f)))
            {
                RenderCaptureCamera();
                Repaint();
            }

            if (GUILayout.Button("Open scene", GUILayout.Width(90f)))
            {
                OpenScene(scenePath);
            }
        }

        EditorGUILayout.HelpBox(
            previewMode == 0
                ? $"Portrait composition with the original {designWidth}x{designHeight} proportions."
                : "Final HDMI simulation. Horizontal distortion is intentional and aspect ratio is not preserved.",
            MessageType.Info);

        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderTexturePath);
        if (texture == null)
        {
            EditorGUILayout.HelpBox(
                $"The {designWidth}x{designHeight} output does not exist yet. Run its Create Template command.",
                MessageType.Warning);
            return;
        }

        if (Event.current.type == EventType.Repaint && autoRefresh && !EditorApplication.isPlaying)
        {
            RenderCaptureCamera();
        }

        GUILayout.Label(
            previewMode == 0
                ? $"DESIGN PREVIEW  {designWidth} x {designHeight}"
                : "HDMI OUTPUT PREVIEW  1920 x 1080",
            EditorStyles.boldLabel);
        Rect available = GUILayoutUtility.GetRect(1f, 10000f, 1f, 10000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(available, new Color(0.075f, 0.075f, 0.075f, 1f));

        float aspect = previewMode == 0
            ? (float)designWidth / designHeight
            : (float)LEDTemplateSetup.OutputWidth / LEDTemplateSetup.OutputHeight;
        Rect previewRect = FitAspect(available, aspect, 12f);
        GUI.DrawTexture(previewRect, texture, ScaleMode.StretchToFill, false);
    }

    private static void OpenScene(string scenePath)
    {
        if (!File.Exists(scenePath))
        {
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private static void RenderCaptureCamera()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        GameObject cameraObject = GameObject.Find("CaptureCamera");
        Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        if (camera != null && camera.targetTexture != null)
        {
            camera.Render();
        }
    }

    private static Rect FitAspect(Rect available, float aspect, float padding)
    {
        Rect inner = new Rect(
            available.x + padding,
            available.y + padding,
            Mathf.Max(1f, available.width - padding * 2f),
            Mathf.Max(1f, available.height - padding * 2f));

        float width = inner.width;
        float height = width / aspect;
        if (height > inner.height)
        {
            height = inner.height;
            width = height * aspect;
        }

        return new Rect(
            inner.x + (inner.width - width) * 0.5f,
            inner.y + (inner.height - height) * 0.5f,
            width,
            height);
    }
}

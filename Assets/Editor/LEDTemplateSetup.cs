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
    public const int NativeDesignWidth = 1920;
    public const int NativeDesignHeight = 1080;
    public const int EventDesignWidth = 344;
    public const int EventDesignHeight = 1032;

    public const string ScenePath = "Assets/Scenes/LEDTemplateTest_608x1080.unity";
    public const string LegacyScenePath = "Assets/Scenes/LEDTemplateTest.unity";
    public const string RenderTexturePath = "Assets/RenderTextures/LED_Output.renderTexture";
    public const string TallScenePath = "Assets/Scenes/LEDTemplateTest_640x1920.unity";
    public const string TallRenderTexturePath = "Assets/RenderTextures/LED_Output_640x1920.renderTexture";
    public const string NativeScenePath = "Assets/Scenes/LEDTemplateTest_1920x1080.unity";
    public const string NativeRenderTexturePath = "Assets/RenderTextures/LED_Output_1920x1080.renderTexture";
    public const string TestBackgroundPath = "Assets/UI/TestBackground.png";
    public const string BoxingSourceScenePath = "Assets/Scenes/Main.unity";
    public const string BoxingIntegratedScenePath = "Assets/Scenes/Main_LED_640x1920.unity";
    public const string Boxing608ScenePath = "Assets/Scenes/Main_LED_608x1080.unity";
    public const string BoxingNativeScenePath = "Assets/Scenes/Main_LED_1920x1080.unity";
    public const string BoxingEventScenePath = "Assets/Scenes/Main_LED_344x1032.unity";
    public const string EventRenderTexturePath = "Assets/RenderTextures/LED_Output_344x1032.renderTexture";
    public const string SharingPackagePath = "Exports/Toshiba_LED_Template_v7.unitypackage";
    private const string BoxingLayoutRequestPath = "Temp/ToshibaApplyUILayout.request";
    private const string EventVariantRequestPath = "Temp/ToshibaCreate344x1032.request";

    [InitializeOnLoadMethod]
    private static void ProcessPendingEventVariantRequest()
    {
        if (!Application.isBatchMode && File.Exists(GetEventVariantRequestAbsolutePath()))
            EditorApplication.delayCall += TryCreatePendingEventVariant;
    }

    private static void TryCreatePendingEventVariant()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryCreatePendingEventVariant;
            return;
        }

        try
        {
            CreateBoxingEventVariant(false);
            File.Delete(GetEventVariantRequestAbsolutePath());
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static string GetEventVariantRequestAbsolutePath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
        return Path.Combine(projectRoot, EventVariantRequestPath);
    }

    [MenuItem("Tools/Toshiba LED/Export Sharing Package (v7)")]
    public static void ExportSharingPackage()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string absolutePackagePath = Path.Combine(projectRoot, SharingPackagePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePackagePath));

        string[] packageAssets =
        {
            "Assets/Editor/LEDTemplateSetup.cs",
            "Assets/UI/TestBackground.png",
            "Assets/UI/README_Toshiba_LED.txt"
        };

        foreach (string assetPath in packageAssets)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                throw new FileNotFoundException(
                    "Cannot export the Toshiba LED sharing package because an asset is missing.",
                    assetPath);
            }
        }

        AssetDatabase.ExportPackage(
            packageAssets,
            absolutePackagePath,
            ExportPackageOptions.Default);

        Debug.Log("Exported Toshiba LED sharing package to: " + absolutePackagePath);
    }

    [InitializeOnLoadMethod]
    private static void ProcessPendingBoxingLayoutRequest()
    {
        if (!Application.isBatchMode && File.Exists(BoxingLayoutRequestPath))
        {
            EditorApplication.delayCall += TryApplyPendingBoxingLayout;
        }
    }

    private static void TryApplyPendingBoxingLayout()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryApplyPendingBoxingLayout;
            return;
        }

        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            if (SceneManager.GetSceneAt(index).isDirty)
            {
                Debug.LogWarning(
                    "Toshiba LED UI layout is waiting because an open scene has unsaved changes. " +
                    "Save the scene, then run Tools > Toshiba LED > Apply Boxing Portrait UI Layout.");
                return;
            }
        }

        ApplyBoxingPortraitUILayoutInternal();
        File.Delete(BoxingLayoutRequestPath);
    }

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

    [MenuItem("Tools/Toshiba LED/Create Templates/Create 1920x1080 Normal Template")]
    public static void CreateNativeTestTemplate()
    {
        CreateTemplate(
            NativeDesignWidth,
            NativeDesignHeight,
            NativeScenePath,
            NativeRenderTexturePath,
            "LED_Output_1920x1080");
    }

    [MenuItem("Tools/Toshiba LED/Create Templates/Create All Templates")]
    public static void CreateAllTemplates()
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
        CreateTemplate(
            NativeDesignWidth,
            NativeDesignHeight,
            NativeScenePath,
            NativeRenderTexturePath,
            "LED_Output_1920x1080",
            false);

        if (File.Exists(BoxingIntegratedScenePath))
        {
            AddSceneToBuildSettings(BoxingIntegratedScenePath);
            Debug.Log("Kept the integrated Boxing LED scene first in Build Profiles.");
        }

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Toshiba LED Templates",
                "The 608x1080, 640x1920, and normal 1920x1080 templates are ready.",
                "OK");
        }
    }

    [MenuItem("Tools/Toshiba LED/Create Templates/Create Both Templates")]
    public static void CreateBothTemplates()
    {
        CreateAllTemplates();
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

        if (scenePath == ScenePath)
        {
            MigrateLegacy608SceneIfNeeded();
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

        bool nativeOutput = designWidth == OutputWidth && designHeight == OutputHeight;
        Debug.Log(
            nativeOutput
                ? "Toshiba LED normal template created. The complete 1920x1080 capture maps 1:1 to the 1920x1080 output."
                : $"Toshiba LED test template created. The complete {designWidth}x{designHeight} capture is stretched to the " +
                  "1920x1080 output without preserving aspect ratio.");

        if (askToSave && !Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Toshiba LED Template",
                $"{Path.GetFileNameWithoutExtension(scenePath)} is ready.\n\n" +
                "Use Tools > Toshiba LED > Open LED Preview to compare the internal design and HDMI output.",
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

    [MenuItem("Tools/Toshiba LED/Open 1920x1080 Normal Test Scene")]
    public static void OpenNativeTestScene()
    {
        OpenOrCreateSceneFromMenu(NativeScenePath, CreateNativeTestTemplate);
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

    [MenuItem("Tools/Toshiba LED/Integrate Boxing Main Scene (640x1920)")]
    public static void IntegrateBoxingMainScene()
    {
        if (!File.Exists(BoxingSourceScenePath))
        {
            Debug.LogError($"Boxing source scene was not found at {BoxingSourceScenePath}.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureAssetFolders();
        Texture2D testBackground = ConfigureTestBackgroundImport();
        RenderTexture ledOutput = CreateOrUpdateRenderTexture(
            TallDesignWidth,
            TallDesignHeight,
            TallRenderTexturePath,
            "LED_Output_640x1920");

        Scene boxingScene = EditorSceneManager.OpenScene(BoxingSourceScenePath, OpenSceneMode.Single);
        Camera boxingCamera = FindCameraInScene(boxingScene, "Main Camera");
        if (boxingCamera == null)
        {
            Debug.LogError("The Boxing Main scene does not contain a camera named Main Camera.");
            return;
        }

        boxingCamera.targetTexture = ledOutput;
        boxingCamera.aspect = (float)TallDesignWidth / TallDesignHeight;
        EditorUtility.SetDirty(boxingCamera);

        ConfigureCaptureCanvas(
            boxingScene,
            boxingCamera,
            TallDesignWidth,
            TallDesignHeight,
            testBackground);
        ConfigureOutputCanvas(boxingScene, ledOutput);
        ConfigureWindowsOutput();

        GameObject captureCanvasObject = FindRoot(boxingScene, "CaptureCanvas");
        Transform background = captureCanvasObject.transform.Find("Background");
        if (background != null)
        {
            background.gameObject.SetActive(false);
        }

        Transform gameUI = captureCanvasObject.transform.Find("GameUI");
        Transform generatedTestUI = gameUI != null ? gameUI.Find("TestUI") : null;
        if (generatedTestUI != null)
        {
            generatedTestUI.gameObject.SetActive(false);
        }

        GameObject originalCanvasObject = FindRoot(boxingScene, "Canvas");
        if (originalCanvasObject == null || gameUI == null)
        {
            Debug.LogError("Could not find the original Boxing Canvas or the new CaptureCanvas/GameUI container.");
            return;
        }

        Transform originalRawImage = originalCanvasObject.transform.Find("RawImage");
        if (originalRawImage != null)
        {
            originalRawImage.gameObject.SetActive(false);
        }

        Transform[] originalChildren = new Transform[originalCanvasObject.transform.childCount];
        for (int index = 0; index < originalChildren.Length; index++)
        {
            originalChildren[index] = originalCanvasObject.transform.GetChild(index);
        }

        foreach (Transform child in originalChildren)
        {
            if (child == originalRawImage)
            {
                continue;
            }

            child.SetParent(gameUI, false);
        }

        Canvas originalCanvas = originalCanvasObject.GetComponent<Canvas>();
        CanvasScaler originalScaler = originalCanvasObject.GetComponent<CanvasScaler>();
        GraphicRaycaster originalRaycaster = originalCanvasObject.GetComponent<GraphicRaycaster>();
        if (originalCanvas != null)
        {
            originalCanvas.enabled = false;
        }

        if (originalScaler != null)
        {
            originalScaler.enabled = false;
        }

        if (originalRaycaster != null)
        {
            originalRaycaster.enabled = false;
        }

        SetBehaviourEnabledByTypeName(originalCanvasObject, "LEDUIStretch", false);
        originalCanvasObject.name = "UIManager_LegacyHolder";
        originalCanvasObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(boxingScene);
        EditorSceneManager.SaveScene(boxingScene, BoxingIntegratedScenePath);
        AddSceneToBuildSettings(BoxingIntegratedScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(BoxingIntegratedScenePath);

        ValidateBoxingIntegration();

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Boxing Toshiba LED Integration",
                "Main_LED_640x1920 is ready. The original Main scene was not modified.",
                "OK");
        }
    }

    [MenuItem("Tools/Toshiba LED/Validate Boxing Integration")]
    public static void ValidateBoxingIntegration()
    {
        int failures = 0;
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(TallRenderTexturePath);
        Check(
            renderTexture != null &&
            renderTexture.width == TallDesignWidth &&
            renderTexture.height == TallDesignHeight,
            "Boxing Render Texture is exactly 640x1920.",
            ref failures);

        Check(File.Exists(BoxingIntegratedScenePath), "Integrated Boxing scene exists.", ref failures);
        if (File.Exists(BoxingIntegratedScenePath))
        {
            Scene scene = EditorSceneManager.OpenScene(BoxingIntegratedScenePath, OpenSceneMode.Single);
            Camera camera = FindCameraInScene(scene, "Main Camera");
            GameObject captureCanvasObject = FindRoot(scene, "CaptureCanvas");
            Canvas captureCanvas = captureCanvasObject != null ? captureCanvasObject.GetComponent<Canvas>() : null;
            Transform gameUI = captureCanvasObject != null ? captureCanvasObject.transform.Find("GameUI") : null;
            GameObject outputCanvasObject = FindRoot(scene, "OutputCanvas");
            Transform outputImageTransform = outputCanvasObject != null
                ? outputCanvasObject.transform.Find("OutputRawImage")
                : null;
            RawImage outputImage = outputImageTransform != null
                ? outputImageTransform.GetComponent<RawImage>()
                : null;
            GameObject logicHolder = FindRoot(scene, "UIManager_LegacyHolder");
            Canvas oldCanvas = logicHolder != null ? logicHolder.GetComponent<Canvas>() : null;

            Check(camera != null && camera.targetTexture == renderTexture, "Main Camera captures into the 640x1920 texture.", ref failures);
            Check(
                captureCanvas != null &&
                captureCanvas.renderMode == RenderMode.ScreenSpaceCamera &&
                captureCanvas.worldCamera == camera,
                "Boxing UI is in the camera-space capture stage.",
                ref failures);
            Check(gameUI != null && gameUI.childCount > 1, "Existing Boxing UI objects were moved under CaptureCanvas/GameUI.", ref failures);
            Check(oldCanvas != null && !oldCanvas.enabled, "Old Screen Space Overlay Canvas is disabled.", ref failures);
            Check(
                logicHolder != null && !GetBehaviourEnabledByTypeName(logicHolder, "LEDUIStretch"),
                "Old LEDUIStretch component is disabled.",
                ref failures);
            Check(
                outputImage != null &&
                outputImage.texture == renderTexture &&
                outputImageTransform.GetComponent<AspectRatioFitter>() == null,
                "OutputRawImage stretches the complete captured frame without aspect preservation.",
                ref failures);
        }

        if (failures == 0)
        {
            Debug.Log("Boxing Toshiba LED integration validation passed. Physical LED verification is still required on-site.");
        }
        else
        {
            Debug.LogError($"Boxing Toshiba LED integration validation found {failures} problem(s).");
        }
    }

    [MenuItem("Tools/Toshiba LED/Recreate Boxing Game In All LED Sizes")]
    public static void RecreateBoxingGameInAllLEDSizes()
    {
        if (!File.Exists(BoxingIntegratedScenePath))
        {
            Debug.LogError(
                $"The integrated Boxing master scene was not found at {BoxingIntegratedScenePath}. " +
                "Run Integrate Boxing Main Scene (640x1920) first.");
            return;
        }

        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureAssetFolders();
        Texture2D testBackground = ConfigureTestBackgroundImport();
        RenderTexture output608 = CreateOrUpdateRenderTexture(
            DesignWidth,
            DesignHeight,
            RenderTexturePath,
            "LED_Output");
        RenderTexture outputNative = CreateOrUpdateRenderTexture(
            NativeDesignWidth,
            NativeDesignHeight,
            NativeRenderTexturePath,
            "LED_Output_1920x1080");

        CreateBoxingSizeVariant(
            Boxing608ScenePath,
            DesignWidth,
            DesignHeight,
            output608,
            testBackground);
        CreateBoxingSizeVariant(
            BoxingNativeScenePath,
            NativeDesignWidth,
            NativeDesignHeight,
            outputNative,
            testBackground);

        AddSceneToBuildSettings(BoxingIntegratedScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(BoxingIntegratedScenePath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(BoxingIntegratedScenePath);

        Debug.Log(
            "Recreated the Boxing game and UI at 608x1080, 640x1920, and normal 1920x1080. " +
            "The existing 640x1920 integrated scene remains the master and first build scene.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Boxing LED Size Variants",
                "Created Main_LED_608x1080 and Main_LED_1920x1080 from the working " +
                "Main_LED_640x1920 master scene.",
                "OK");
        }
    }

    [MenuItem("Tools/Toshiba LED/Create Boxing Main 344x1032 Event Version")]
    public static void CreateBoxing344x1032EventVersion()
    {
        CreateBoxingEventVariant(true);
    }

    public static void CreateBoxing344x1032EventVersionAutomated()
    {
        CreateBoxingEventVariant(false);
    }

    private static void CreateBoxingEventVariant(bool showDialog)
    {
        if (!File.Exists(BoxingIntegratedScenePath))
        {
            throw new FileNotFoundException(
                "The 640x1920 Boxing master scene is required.",
                BoxingIntegratedScenePath);
        }

        if (showDialog &&
            !Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureAssetFolders();
        Texture2D testBackground = ConfigureTestBackgroundImport();
        RenderTexture eventOutput = CreateOrUpdateRenderTexture(
            EventDesignWidth,
            EventDesignHeight,
            EventRenderTexturePath,
            "LED_Output_344x1032");

        CreateBoxingSizeVariant(
            BoxingEventScenePath,
            EventDesignWidth,
            EventDesignHeight,
            eventOutput,
            testBackground);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int failures = 0;
        ValidateBoxingSizeVariant(
            BoxingEventScenePath,
            EventDesignWidth,
            EventDesignHeight,
            EventRenderTexturePath,
            ref failures);

        EditorSceneManager.OpenScene(BoxingEventScenePath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(BoxingEventScenePath);

        if (failures > 0)
            throw new System.InvalidOperationException("The 344x1032 Boxing scene failed validation.");

        Debug.Log(
            "Created and validated Main_LED_344x1032. Its complete 344x1032 frame " +
            "stretches to the 1920x1080 Windows/HDMI output.");

        if (showDialog && !Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "344x1032 Event Version Ready",
                "Main_LED_344x1032 is ready and is first in Build Profiles.",
                "OK");
        }
    }

    [MenuItem("Tools/Toshiba LED/Validate Boxing Size Variants")]
    public static void ValidateBoxingSizeVariants()
    {
        int failures = 0;
        ValidateBoxingSizeVariant(
            BoxingIntegratedScenePath,
            TallDesignWidth,
            TallDesignHeight,
            TallRenderTexturePath,
            ref failures);
        ValidateBoxingSizeVariant(
            Boxing608ScenePath,
            DesignWidth,
            DesignHeight,
            RenderTexturePath,
            ref failures);
        ValidateBoxingSizeVariant(
            BoxingNativeScenePath,
            NativeDesignWidth,
            NativeDesignHeight,
            NativeRenderTexturePath,
            ref failures);

        if (failures == 0)
        {
            Debug.Log(
                "Boxing LED size variant validation passed for 608x1080, 640x1920, and 1920x1080. " +
                "Physical LED verification is still required on-site.");
        }
        else
        {
            Debug.LogError($"Boxing LED size variant validation found {failures} problem(s).");
        }
    }

    private static void CreateBoxingSizeVariant(
        string targetScenePath,
        int designWidth,
        int designHeight,
        RenderTexture renderTexture,
        Texture2D testBackground)
    {
        Scene masterScene = EditorSceneManager.OpenScene(BoxingIntegratedScenePath, OpenSceneMode.Single);
        if (!EditorSceneManager.SaveScene(masterScene, targetScenePath, true))
        {
            throw new IOException($"Could not create the Boxing LED scene variant at {targetScenePath}.");
        }

        Scene variantScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
        GameObject cameraObject = FindRoot(variantScene, "Main Camera") ?? FindRoot(variantScene, "CaptureCamera");
        Camera captureCamera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        if (captureCamera == null)
        {
            throw new MissingComponentException($"No capture camera was found in {targetScenePath}.");
        }

        captureCamera.targetTexture = renderTexture;
        captureCamera.aspect = (float)designWidth / designHeight;

        GameObject captureCanvasObject = FindRoot(variantScene, "CaptureCanvas");
        CanvasScaler captureScaler = captureCanvasObject != null
            ? captureCanvasObject.GetComponent<CanvasScaler>()
            : null;
        if (captureScaler == null)
        {
            throw new MissingComponentException($"CaptureCanvas was not found in {targetScenePath}.");
        }

        captureScaler.referenceResolution = new Vector2(designWidth, designHeight);
        Transform gameUI = captureCanvasObject.transform.Find("GameUI");
        if (gameUI != null)
        {
            float xRatio = (float)designWidth / TallDesignWidth;
            float yRatio = (float)designHeight / TallDesignHeight;
            float uniformScale = Mathf.Min(xRatio, yRatio);
            foreach (RectTransform child in gameUI.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.parent != gameUI || child.name == "TestUI")
                {
                    continue;
                }

                Vector2 position = child.anchoredPosition;
                child.anchoredPosition = new Vector2(position.x * xRatio, position.y * yRatio);
                child.localScale = new Vector3(
                    child.localScale.x * uniformScale,
                    child.localScale.y * uniformScale,
                    child.localScale.z);
            }
        }

        Transform backgroundTransform = captureCanvasObject.transform.Find("Background");
        RawImage background = backgroundTransform != null ? backgroundTransform.GetComponent<RawImage>() : null;
        if (background != null && testBackground != null)
        {
            background.texture = testBackground;
            float cropWidth = Mathf.Min(designWidth, testBackground.width);
            float normalizedWidth = cropWidth / testBackground.width;
            background.uvRect = new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
        }

        GameObject outputCanvasObject = FindRoot(variantScene, "OutputCanvas");
        Transform outputTransform = outputCanvasObject != null
            ? outputCanvasObject.transform.Find("OutputRawImage")
            : null;
        RawImage outputImage = outputTransform != null ? outputTransform.GetComponent<RawImage>() : null;
        if (outputImage == null)
        {
            throw new MissingComponentException($"OutputRawImage was not found in {targetScenePath}.");
        }

        outputImage.texture = renderTexture;
        outputImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        StretchToParent(outputTransform as RectTransform);
        EditorSceneManager.MarkSceneDirty(variantScene);
        EditorSceneManager.SaveScene(variantScene, targetScenePath);
        AddSceneToBuildSettings(targetScenePath);
    }

    private static void ValidateBoxingSizeVariant(
        string scenePath,
        int designWidth,
        int designHeight,
        string renderTexturePath,
        ref int failures)
    {
        string resolution = $"{designWidth}x{designHeight}";
        RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderTexturePath);
        Check(
            renderTexture != null && renderTexture.width == designWidth && renderTexture.height == designHeight,
            $"Boxing {resolution} Render Texture is exact.",
            ref failures);
        Check(File.Exists(scenePath), $"Boxing {resolution} scene exists at {scenePath}.", ref failures);
        if (!File.Exists(scenePath))
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject cameraObject = FindRoot(scene, "Main Camera") ?? FindRoot(scene, "CaptureCamera");
        Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        Check(
            camera != null && camera.targetTexture == renderTexture,
            $"Boxing {resolution} camera captures into the matching texture.",
            ref failures);

        GameObject captureCanvasObject = FindRoot(scene, "CaptureCanvas");
        CanvasScaler scaler = captureCanvasObject != null ? captureCanvasObject.GetComponent<CanvasScaler>() : null;
        Check(
            scaler != null && scaler.referenceResolution == new Vector2(designWidth, designHeight),
            $"Boxing {resolution} CaptureCanvas uses the matching reference resolution.",
            ref failures);

        Transform gameUI = captureCanvasObject != null ? captureCanvasObject.transform.Find("GameUI") : null;
        Check(gameUI != null && gameUI.childCount > 0, $"Boxing {resolution} UI was recreated.", ref failures);

        GameObject outputCanvasObject = FindRoot(scene, "OutputCanvas");
        Transform outputTransform = outputCanvasObject != null
            ? outputCanvasObject.transform.Find("OutputRawImage")
            : null;
        RawImage outputImage = outputTransform != null ? outputTransform.GetComponent<RawImage>() : null;
        Check(
            outputImage != null && outputImage.texture == renderTexture &&
            outputTransform.GetComponent<AspectRatioFitter>() == null,
            $"Boxing {resolution} output displays the complete frame without aspect preservation.",
            ref failures);
    }

    private static Camera FindCameraInScene(Scene scene, string cameraName)
    {
        GameObject cameraObject = FindRoot(scene, cameraName);
        return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
    }

    private static void SetBehaviourEnabledByTypeName(GameObject gameObject, string typeName, bool enabled)
    {
        foreach (MonoBehaviour behaviour in gameObject.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == typeName)
            {
                behaviour.enabled = enabled;
                EditorUtility.SetDirty(behaviour);
            }
        }
    }

    private static bool GetBehaviourEnabledByTypeName(GameObject gameObject, string typeName)
    {
        foreach (MonoBehaviour behaviour in gameObject.GetComponents<MonoBehaviour>())
        {
            if (behaviour != null && behaviour.GetType().Name == typeName)
            {
                return behaviour.enabled;
            }
        }

        return false;
    }

    [MenuItem("Tools/Toshiba LED/Apply Boxing Portrait UI Layout")]
    public static void ApplyBoxingPortraitUILayout()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        ApplyBoxingPortraitUILayoutInternal();
    }

    private static void ApplyBoxingPortraitUILayoutInternal()
    {
        if (!File.Exists(BoxingIntegratedScenePath))
        {
            Debug.LogError("Create the integrated Boxing scene before applying its portrait UI layout.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(BoxingIntegratedScenePath, OpenSceneMode.Single);
        GameObject captureCanvasObject = FindRoot(scene, "CaptureCanvas");
        Transform gameUI = captureCanvasObject != null ? captureCanvasObject.transform.Find("GameUI") : null;
        if (gameUI == null)
        {
            Debug.LogError("CaptureCanvas/GameUI was not found in the integrated Boxing scene.");
            return;
        }

        ConfigurePortraitRect(gameUI, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(560f, 100f));
        SetTMPFontSize(gameUI.Find("Title"), 58f);

        Transform lessonPanel = ConfigurePortraitRect(gameUI, "UI  (1)", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(360f, 82f));
        if (lessonPanel != null)
        {
            lessonPanel.SetAsFirstSibling();
        }

        Transform lesson = ConfigurePortraitRect(gameUI, "Lesson", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(360f, 82f));
        ConfigureChildToFill(lesson, "LessonMessage", 34f);

        ConfigurePortraitRect(gameUI, "Slider", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(360f, 34f));
        Transform score = ConfigurePortraitRect(gameUI, "Score", Vector2.one, Vector2.one, new Vector2(-30f, -245f), new Vector2(170f, 72f));
        SetTMPFontSize(score, 44f);

        ConfigurePortraitRect(gameUI, "WebcamTest", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -340f), new Vector2(270f, 210f));

        Transform progress = ConfigurePortraitRect(gameUI, "Progress ", Vector2.one, Vector2.one, new Vector2(-28f, -350f), new Vector2(250f, 150f));
        SetTMPFontSize(progress, 30f);
        ConfigureChildRect(
            progress,
            "ProgressMessage",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 5f),
            new Vector2(250f, 90f),
            64f);

        Transform hint = ConfigurePortraitRect(gameUI, "Hint", Vector2.one, Vector2.one, new Vector2(-28f, -525f), new Vector2(250f, 70f));
        SetTMPFontSize(hint, 30f);

        Transform ready = ConfigurePortraitRect(gameUI, "Ready", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 130f));
        SetTMPFontSize(ready, 72f);

        Transform feedback = ConfigurePortraitRect(gameUI, "Feedback", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 265f), new Vector2(560f, 130f));
        SetTMPFontSize(feedback, 48f);
        ConfigureChildToFill(feedback, "FeedbackMessage", 32f);

        Transform timer = ConfigurePortraitRect(gameUI, "Timer", Vector2.one, Vector2.one, new Vector2(-25f, -35f), new Vector2(220f, 65f));
        SetTMPFontSize(timer, 30f);

        SetDirectChildActive(gameUI, "UI ", false);
        SetDirectChildActive(gameUI, "UI  (3)", false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BoxingIntegratedScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Applied the Boxing 640x1920 portrait UI layout. Existing UIManager references were preserved.");
    }

    private static Transform ConfigurePortraitRect(
        Transform parent,
        string childName,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        RectTransform rect = child as RectTransform;
        if (rect == null)
        {
            return child;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return child;
    }

    private static void ConfigureChildToFill(Transform parent, string childName, float fontSize)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        RectTransform rect = child as RectTransform;
        if (rect != null)
        {
            StretchToParent(rect, new Vector2(8f, 4f));
        }

        SetTMPFontSize(child, fontSize);
    }

    private static void ConfigureChildRect(
        Transform parent,
        string childName,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        ConfigurePortraitRect(parent, childName, anchor, pivot, position, size);
        SetTMPFontSize(parent != null ? parent.Find(childName) : null, fontSize);
    }

    private static void SetTMPFontSize(Transform transform, float fontSize)
    {
        if (transform == null)
        {
            return;
        }

        foreach (MonoBehaviour behaviour in transform.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour.GetType().Name != "TextMeshProUGUI")
            {
                continue;
            }

            SerializedObject serializedText = new SerializedObject(behaviour);
            SerializedProperty fontSizeProperty = serializedText.FindProperty("m_fontSize");
            if (fontSizeProperty != null)
            {
                fontSizeProperty.floatValue = fontSize;
                serializedText.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void SetDirectChildActive(Transform parent, string childName, bool active)
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        if (child != null)
        {
            child.gameObject.SetActive(active);
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

    [MenuItem("Tools/Toshiba LED/Validate Templates/Validate All Templates")]
    public static void ValidateAllTemplates()
    {
        ValidateTemplate(DesignWidth, DesignHeight, ScenePath, RenderTexturePath);
        ValidateTemplate(TallDesignWidth, TallDesignHeight, TallScenePath, TallRenderTexturePath);
        ValidateTemplate(NativeDesignWidth, NativeDesignHeight, NativeScenePath, NativeRenderTexturePath);
    }

    [MenuItem("Tools/Toshiba LED/Validate Templates/Validate 640x1920 Template")]
    public static void ValidateTallTestTemplate()
    {
        ValidateTemplate(TallDesignWidth, TallDesignHeight, TallScenePath, TallRenderTexturePath);
    }

    [MenuItem("Tools/Toshiba LED/Validate Templates/Validate 1920x1080 Normal Template")]
    public static void ValidateNativeTestTemplate()
    {
        ValidateTemplate(NativeDesignWidth, NativeDesignHeight, NativeScenePath, NativeRenderTexturePath);
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
            bool nativeOutput = designWidth == OutputWidth && designHeight == OutputHeight;
            if (nativeOutput)
            {
                Debug.Log(
                    "Toshiba LED 1920x1080 normal template validation passed. The internal frame maps 1:1 to the " +
                    "1920x1080 Windows output. Physical LED verification is still required on-site.");
            }
            else
            {
                float horizontalScale = (float)OutputWidth / designWidth;
                Debug.Log(
                    $"Toshiba LED {resolution} template validation passed. The frame is intentionally stretched " +
                    $"to 1920x1080 ({horizontalScale:0.####}x horizontal scale). Physical LED verification is still required on-site.");
            }
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
            if (oldScene.path == scenePath || oldScene.path == LegacyScenePath)
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

    private static void MigrateLegacy608SceneIfNeeded()
    {
        if (File.Exists(ScenePath) || !File.Exists(LegacyScenePath))
        {
            return;
        }

        string error = AssetDatabase.MoveAsset(LegacyScenePath, ScenePath);
        if (!string.IsNullOrEmpty(error))
        {
            throw new IOException(
                $"Could not rename the legacy 608x1080 scene from {LegacyScenePath} to {ScenePath}: {error}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Renamed the legacy 608x1080 scene to {ScenePath}.");
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
        sourceMode = GUILayout.Toolbar(sourceMode, new[] { "608x1080", "640x1920", "1920x1080" });
        GUILayout.Label("Preview mode", EditorStyles.boldLabel);
        previewMode = GUILayout.Toolbar(previewMode, new[] { "Design proportions", "HDMI 1920x1080" });

        int designWidth;
        int designHeight;
        string renderTexturePath;
        string scenePath;
        if (sourceMode == 0)
        {
            designWidth = LEDTemplateSetup.DesignWidth;
            designHeight = LEDTemplateSetup.DesignHeight;
            renderTexturePath = LEDTemplateSetup.RenderTexturePath;
            scenePath = LEDTemplateSetup.ScenePath;
        }
        else if (sourceMode == 1)
        {
            designWidth = LEDTemplateSetup.TallDesignWidth;
            designHeight = LEDTemplateSetup.TallDesignHeight;
            renderTexturePath = LEDTemplateSetup.TallRenderTexturePath;
            scenePath = LEDTemplateSetup.TallScenePath;
        }
        else
        {
            designWidth = LEDTemplateSetup.NativeDesignWidth;
            designHeight = LEDTemplateSetup.NativeDesignHeight;
            renderTexturePath = LEDTemplateSetup.NativeRenderTexturePath;
            scenePath = LEDTemplateSetup.NativeScenePath;
        }

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

        bool nativeOutput = designWidth == LEDTemplateSetup.OutputWidth &&
                            designHeight == LEDTemplateSetup.OutputHeight;
        string previewHelp = previewMode == 0
            ? $"Internal composition with the original {designWidth}x{designHeight} proportions."
            : nativeOutput
                ? "Final HDMI simulation. The normal 1920x1080 template maps to the output without distortion."
                : "Final HDMI simulation. Distortion is intentional and aspect ratio is not preserved.";
        EditorGUILayout.HelpBox(previewHelp, MessageType.Info);

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

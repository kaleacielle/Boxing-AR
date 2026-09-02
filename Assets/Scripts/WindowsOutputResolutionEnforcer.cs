using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forces the standalone Toshiba/HDMI player to use the required Windows signal.
/// Editor Game view resolution is intentionally left under the developer's control.
/// </summary>
public sealed class WindowsOutputResolutionEnforcer : MonoBehaviour
{
    private const int StandardOutputWidth = 1920;
    private const int StandardOutputHeight = 1080;
    private const int EventOutputWidth = 344;
    private const int EventOutputHeight = 1032;
    private const float RecheckInterval = 1f;

    private float nextRecheckTime;
    private bool userSelectedWindowed;
    private int outputWidth = StandardOutputWidth;
    private int outputHeight = StandardOutputHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        GameObject enforcer = new GameObject("Windows Output Resolution Enforcer");
        DontDestroyOnLoad(enforcer);
        enforcer.AddComponent<WindowsOutputResolutionEnforcer>();
#endif
    }

    private IEnumerator Start()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResolveOutputResolution(SceneManager.GetActiveScene());
        ApplyResolution();

        // Unity can restore a previously saved window size during startup, so
        // enforce the output again after the first rendered frame.
        yield return null;
        if (!userSelectedWindowed)
            ApplyResolution();

        yield return new WaitForSecondsRealtime(0.5f);
        if (!userSelectedWindowed)
            ApplyResolution();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveOutputResolution(scene);
        if (!userSelectedWindowed)
            ApplyResolution();
    }

    private void ResolveOutputResolution(Scene scene)
    {
        bool useEventResolution = scene.name == "Main_LED_344x1032";
        outputWidth = useEventResolution ? EventOutputWidth : StandardOutputWidth;
        outputHeight = useEventResolution ? EventOutputHeight : StandardOutputHeight;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            return;
        }

        bool altPressed =
            Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

        if (altPressed && Input.GetKeyDown(KeyCode.Return))
        {
            ToggleFullscreen();
            return;
        }

        if (userSelectedWindowed)
            return;

        if (Time.unscaledTime < nextRecheckTime)
            return;

        nextRecheckTime = Time.unscaledTime + RecheckInterval;

        if (Screen.width != outputWidth ||
            Screen.height != outputHeight ||
            Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
        {
            ApplyResolution();
        }
    }

    private void ToggleFullscreen()
    {
        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
        {
            userSelectedWindowed = true;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
        else
        {
            userSelectedWindowed = false;
            ApplyResolution();
        }
    }

    private void ApplyResolution()
    {
        Screen.SetResolution(
            outputWidth,
            outputHeight,
            FullScreenMode.FullScreenWindow);
    }
}

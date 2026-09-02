using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PythonPoseLauncher : MonoBehaviour
{
    private const string PreferredCameraKey = "BoxingAR.PreferredCamera";

    [Header("Python tracking")]
    public string scriptName = "pose_test.py";
    public int udpPort = 5052;
    public int trackingFramePort = 5053;

    private static PythonPoseLauncher instance;
    private Process pythonProcess;
    private string projectRoot;
    private int selectedCameraIndex;
    private string selectedCameraName = "Camera 1";
    private bool stoppingPoseTracking;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ResolvePreferredCamera();
        WebcamTest.SelectedCameraChanged += HandleSelectedCameraChanged;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        projectRoot = FindProjectRoot();
        StartPoseTracking();
    }

    private void StartPoseTracking()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
            return;

        string executablePath;
        string arguments;
        string workingDirectory;

        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            executablePath = GetPackagedWindowsTrackerPath();
            if (!File.Exists(executablePath))
            {
                UnityEngine.Debug.LogError(
                    $"Packaged pose tracker was not found: {executablePath}. " +
                    "Rebuild the game so the PoseTracker folder is included.");
                return;

            }

            arguments =
                "--udp-port " + udpPort +
                " --frame-port " + trackingFramePort;
            workingDirectory = Path.GetDirectoryName(executablePath);
        }
        else
        {
            if (string.IsNullOrEmpty(projectRoot))
            {
                UnityEngine.Debug.LogError("Could not find the project folder containing pose_test.py.");
                return;
            }

            string scriptPath = Path.Combine(projectRoot, scriptName);
            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"Python pose script was not found: {scriptPath}");
                return;
            }

            executablePath = Application.platform == RuntimePlatform.WindowsEditor
                ? GetWindowsPythonPath()
                : GetMacPythonPath();

            if (string.IsNullOrEmpty(executablePath))
                return;

            arguments = Quote(scriptPath) +
                " --udp-port " + udpPort +
                " --frame-port " + trackingFramePort;
            workingDirectory = projectRoot;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = true
            };

            pythonProcess = Process.Start(startInfo);
            pythonProcess.EnableRaisingEvents = true;
            pythonProcess.ErrorDataReceived += HandlePythonError;
            pythonProcess.Exited += HandlePythonExit;
            pythonProcess.BeginErrorReadLine();
            UnityEngine.Debug.Log(
                $"Started MediaPipe tracking from Unity camera frames " +
                $"on UDP ports {trackingFramePort} -> {udpPort}.");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"Could not start Python pose tracking: {exception.Message}");
        }
    }

    private static string GetPackagedWindowsTrackerPath()
    {
        string playerDirectory = Directory.GetParent(Application.dataPath)?.FullName;
        return Path.Combine(playerDirectory ?? string.Empty, "PoseTracker", "PoseTracker.exe");
    }

    private void HandlePythonError(object sender, DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
            UnityEngine.Debug.LogError($"Python tracking: {eventArgs.Data}");
    }

    private void HandlePythonExit(object sender, EventArgs eventArgs)
    {
        if (!stoppingPoseTracking && pythonProcess != null && pythonProcess.ExitCode != 0)
            UnityEngine.Debug.LogError($"Python tracking stopped with exit code {pythonProcess.ExitCode}.");
    }

    private void ResolvePreferredCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        string preferredName = PlayerPrefs.GetString(PreferredCameraKey, string.Empty);

        selectedCameraIndex = 0;
        if (!string.IsNullOrEmpty(preferredName))
        {
            for (int index = 0; index < devices.Length; index++)
            {
                if (devices[index].name == preferredName)
                {
                    selectedCameraIndex = index;
                    break;
                }
            }
        }

        if (devices.Length > 0)
            selectedCameraName = devices[Mathf.Clamp(selectedCameraIndex, 0, devices.Length - 1)].name;
    }

    private void HandleSelectedCameraChanged(int cameraIndex, string cameraName)
    {
        selectedCameraIndex = Mathf.Max(0, cameraIndex);
        selectedCameraName = string.IsNullOrEmpty(cameraName)
            ? $"Camera {selectedCameraIndex + 1}"
            : cameraName;

        UnityEngine.Debug.Log($"Pose tracking is receiving frames from {selectedCameraName}.");
    }

    private string GetWindowsPythonPath()
    {
        string venvPath = Path.Combine(projectRoot, "venv_windows", "Scripts", "python.exe");
        if (File.Exists(venvPath) && CanRunProcess(venvPath, "-m pip --version"))
            return venvPath;

        string pythonLauncher = FindWindowsPythonLauncher();
        string pythonExecutable = FindWindowsPythonExecutable();
        if (string.IsNullOrEmpty(pythonLauncher) && string.IsNullOrEmpty(pythonExecutable))
        {
            UnityEngine.Debug.LogError("A supported Python version (3.9 to 3.12) was not found.");
            return null;
        }

        string venvArguments = "-m venv --clear " + Quote(Path.Combine(projectRoot, "venv_windows"));
        if (!string.IsNullOrEmpty(pythonLauncher))
            venvArguments = "-3.11 " + venvArguments;

        if (!RunProcess(
                string.IsNullOrEmpty(pythonLauncher) ? pythonExecutable : pythonLauncher,
                venvArguments))
            return null;

        string createdPythonPath = Path.Combine(projectRoot, "venv_windows", "Scripts", "python.exe");
        string requirementsPath = Path.Combine(projectRoot, "requirements_windows.txt");
        if (!File.Exists(createdPythonPath) || !File.Exists(requirementsPath))
        {
            UnityEngine.Debug.LogError("Windows Python environment setup files are missing.");
            return null;
        }

        if (!RunProcess(
            createdPythonPath,
            "-m pip install --trusted-host pypi.org " +
            "--trusted-host files.pythonhosted.org -r " + Quote(requirementsPath)))
            return null;

        return createdPythonPath;
    }

    private string GetMacPythonPath()
    {
        string venvPath = Path.Combine(projectRoot, "venv", "bin", "python3.11");
        if (File.Exists(venvPath) && CanRunProcess(venvPath, "-m pip --version"))
            return venvPath;

        string systemPython = FindMacPython311();
        if (string.IsNullOrEmpty(systemPython))
        {
            UnityEngine.Debug.LogError("Python 3.11 was not found on macOS.");
            return null;
        }

        string venvDirectory = Path.Combine(projectRoot, "venv");
        if (!RunProcess(systemPython, "-m venv --clear " + Quote(venvDirectory)))
            return null;

        string requirementsPath = Path.Combine(projectRoot, "requirements.txt");
        if (!File.Exists(venvPath) || !File.Exists(requirementsPath))
        {
            UnityEngine.Debug.LogError("macOS Python environment setup files are missing.");
            return null;
        }

        if (!RunProcess(
            venvPath,
            "-m pip install --trusted-host pypi.org " +
            "--trusted-host files.pythonhosted.org -r " + Quote(requirementsPath)))
            return null;

        return venvPath;
    }

    private string FindMacPython311()
    {
        string[] candidates =
        {
            "python3.11",
            "/opt/homebrew/bin/python3.11",
            "/usr/local/bin/python3.11"
        };

        foreach (string candidate in candidates)
        {
            if (CanRunSupportedPython(candidate, "--version"))
                return candidate;
        }

        return null;
    }

    private string FindWindowsPythonLauncher()
    {
        if (CanRunSupportedPython("py.exe", "-3.11 --version"))
            return "py.exe";

        return null;
    }

    private string FindWindowsPythonExecutable()
    {
        string[] candidates =
        {
            "python.exe",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Python", "Python311", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Python", "Python311", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Python311", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Python312", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Python310", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Python39", "python.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Python311", "python.exe")
        };

        foreach (string candidate in candidates)
        {
            if (CanRunSupportedPython(candidate, "--version"))
                return candidate;
        }

        return null;
    }

    private bool CanRunSupportedPython(string fileName, string arguments)
    {
        try
        {
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                string versionOutput = output + error;
                return process.ExitCode == 0 &&
                    (versionOutput.Contains("Python 3.9") ||
                     versionOutput.Contains("Python 3.10") ||
                     versionOutput.Contains("Python 3.11") ||
                     versionOutput.Contains("Python 3.12"));
            }
        }
        catch
        {
            return false;
        }
    }

    private bool CanRunProcess(string fileName, string arguments)
    {
        try
        {
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    private bool RunProcess(string fileName, string arguments)
    {
        try
        {
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    UnityEngine.Debug.LogError($"Python setup command failed with exit code {process.ExitCode}.");
                return process.ExitCode == 0;
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"Python setup command failed: {exception.Message}");
            return false;
        }
    }
    private string FindProjectRoot()
    {
        string[] candidates =
        {
            Directory.GetParent(Application.dataPath)?.FullName,
            Directory.GetParent(Application.dataPath)?.Parent?.FullName,
            Directory.GetCurrentDirectory()
        };

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && File.Exists(Path.Combine(candidate, scriptName)))
                return candidate;
        }

        return null;
    }

    private string Quote(string path)
    {
        return "\"" + path.Replace("\"", "\\\"") + "\"";
    }

    private void OnApplicationQuit()
    {
        StopPoseTracking();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            WebcamTest.SelectedCameraChanged -= HandleSelectedCameraChanged;
            StopPoseTracking();
        }
    }

    private void StopPoseTracking()
    {
        if (pythonProcess == null)
            return;

        try
        {
            stoppingPoseTracking = true;
            if (!pythonProcess.HasExited)
                pythonProcess.Kill();
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            pythonProcess.Dispose();
            pythonProcess = null;
            stoppingPoseTracking = false;
        }
    }
}

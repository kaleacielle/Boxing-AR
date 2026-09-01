using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class PythonPoseLauncher : MonoBehaviour
{
    [Header("Python tracking")]
    public string scriptName = "pose_test.py";
    public int udpPort = 5052;
    public bool autoStartOnPlay = true;

    private static PythonPoseLauncher instance;
    private Process pythonProcess;
    private string projectRoot;
    private string resolvedScriptPath;
    private string resolvedPythonPath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PythonPoseLauncher>() != null)
            return;

        GameObject launcher = new GameObject("PythonPoseLauncher");
        launcher.AddComponent<PythonPoseLauncher>();
        DontDestroyOnLoad(launcher);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return null;
        StartPoseTracking();
    }

    public void StartPoseTracking()
    {
        if (!autoStartOnPlay && !Application.isPlaying)
            return;

        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            UnityEngine.Debug.Log($"[TRACKER] Python process already running: {pythonProcess.ProcessName} (PID {pythonProcess.Id})");
            return;
        }

        CleanupStaleTrackerProcesses();

        if (!Application.isEditor)
        {
            string bundledTracker = ResolveBundledTrackerPath();
            if (!string.IsNullOrEmpty(bundledTracker))
            {
                LaunchBundledTracker(bundledTracker);
                return;
            }

            UnityEngine.Debug.LogError("[TRACKER] Standalone build did not find a bundled tracker in the app output. The build is missing Tracking/PoseTracker.");
            return;
        }

        projectRoot = FindProjectRoot();
        if (string.IsNullOrEmpty(projectRoot))
        {
            UnityEngine.Debug.LogError("[TRACKER] Could not locate project root containing pose_test.py.");
            return;
        }

        resolvedScriptPath = Path.Combine(projectRoot, scriptName);
        if (!File.Exists(resolvedScriptPath))
        {
            UnityEngine.Debug.LogError($"[TRACKER] Python script not found: {resolvedScriptPath}");
            return;
        }

        resolvedPythonPath = ResolvePythonExecutable();
        if (string.IsNullOrEmpty(resolvedPythonPath))
        {
            UnityEngine.Debug.LogError("[TRACKER] Failed to resolve a valid Python executable.");
            return;
        }

        UnityEngine.Debug.Log($"[TRACKER] Running in Editor mode");
        UnityEngine.Debug.Log($"[TRACKER] Platform: {Application.platform}");
        UnityEngine.Debug.Log($"[TRACKER] Python executable: {resolvedPythonPath}");
        UnityEngine.Debug.Log($"[TRACKER] Script path: {resolvedScriptPath}");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = resolvedPythonPath,
                Arguments = Quote(resolvedScriptPath),
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            pythonProcess = Process.Start(startInfo);
            if (pythonProcess == null)
            {
                UnityEngine.Debug.LogError("[TRACKER] Process.Start returned null.");
                return;
            }

            pythonProcess.EnableRaisingEvents = true;
            pythonProcess.OutputDataReceived += HandlePythonOutput;
            pythonProcess.ErrorDataReceived += HandlePythonError;
            pythonProcess.Exited += HandlePythonExit;
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log($"[TRACKER] Tracker started. UDP port: {udpPort}. PID: {pythonProcess.Id}");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"[TRACKER] Could not start Python pose tracking: {exception.Message}");
        }
    }

    private void HandlePythonOutput(object sender, DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
            UnityEngine.Debug.Log($"[TRACKER] stdout: {eventArgs.Data}");
    }

    private void HandlePythonError(object sender, DataReceivedEventArgs eventArgs)
    {
        if (!string.IsNullOrEmpty(eventArgs.Data))
            UnityEngine.Debug.LogError($"[TRACKER] stderr: {eventArgs.Data}");
    }

    private void HandlePythonExit(object sender, EventArgs eventArgs)
    {
        if (pythonProcess == null)
            return;

        int exitCode = pythonProcess.ExitCode;
        UnityEngine.Debug.LogWarning($"[TRACKER] Python tracking process exited with code {exitCode}.");
    }

    private void CleanupStaleTrackerProcesses()
    {
        try
        {
            string command = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer
                ? "powershell -NoProfile -Command \"Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'python' -or $_.Name -match 'py' } | ForEach-Object { $p = $_; $cmd = (Get-CimInstance Win32_Process -Filter \"ProcessId = $($p.ProcessId)\").CommandLine; if ($cmd -match 'pose_test.py') { Stop-Process -Id $p.ProcessId -Force } }\""
                : "bash -lc 'pgrep -af \"pose_test.py\" | awk \"{print $1}\" | xargs -r kill -9'";

            using (Process cleanup = Process.Start(new ProcessStartInfo
            {
                FileName = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ? "cmd.exe" : "/bin/bash",
                Arguments = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer ? "/C " + command : "-lc \"" + command + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                if (cleanup == null)
                    return;

                string stdout = cleanup.StandardOutput.ReadToEnd();
                string stderr = cleanup.StandardError.ReadToEnd();
                cleanup.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                    UnityEngine.Debug.Log($"[TRACKER] Stale tracker cleanup: {stdout.Trim()}");
                if (!string.IsNullOrEmpty(stderr))
                    UnityEngine.Debug.LogWarning($"[TRACKER] Stale tracker cleanup warning: {stderr.Trim()}");
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[TRACKER] Could not clean stale tracker processes: {exception.Message}");
        }
    }

    private string ResolvePythonExecutable()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            return ResolveWindowsPythonExecutable();

        return ResolveMacPythonExecutable();
    }

    private string ResolveMacPythonExecutable()
    {
        string venvPython = Path.Combine(projectRoot, "venv", "bin", "python");
        if (File.Exists(venvPython) && CanRunPython(venvPython, "--version"))
            return venvPython;

        string venvPython311 = Path.Combine(projectRoot, "venv", "bin", "python3.11");
        if (File.Exists(venvPython311) && CanRunPython(venvPython311, "--version"))
            return venvPython311;

        string systemPython = FindMacPython311();
        if (string.IsNullOrEmpty(systemPython))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Python 3.11 was not found on macOS. Install it or create the project venv.");
            return null;
        }

        string venvDirectory = Path.Combine(projectRoot, "venv");
        if (!RunProcess(systemPython, "-m venv " + Quote(venvDirectory)))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Failed to create the macOS project venv.");
            return null;
        }

        string createdPython = Path.Combine(projectRoot, "venv", "bin", "python");
        if (!File.Exists(createdPython))
        {
            createdPython = Path.Combine(projectRoot, "venv", "bin", "python3.11");
        }

        if (!File.Exists(createdPython))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] macOS venv was created but python executable is missing.");
            return null;
        }

        string requirementsPath = Path.Combine(projectRoot, "requirements.txt");
        if (File.Exists(requirementsPath) && !RunProcess(createdPython, "-m pip install -r " + Quote(requirementsPath)))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Failed to install macOS Python requirements.");
            return null;
        }

        return createdPython;
    }

    private string ResolveWindowsPythonExecutable()
    {
        string venvPython = Path.Combine(projectRoot, "venv_windows", "Scripts", "python.exe");
        if (File.Exists(venvPython) && CanRunPython(venvPython, "--version"))
            return venvPython;

        string pyLauncher = FindWindowsPythonLauncher();
        string pythonExecutable = FindWindowsPythonExecutable();

        if (string.IsNullOrEmpty(pyLauncher) && string.IsNullOrEmpty(pythonExecutable))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] No valid Windows Python 3.11 executable was found. Install Python 3.11 and try again.");
            return null;
        }

        string pythonForVenv = !string.IsNullOrEmpty(pyLauncher) ? pyLauncher : pythonExecutable;
        string venvCreateCommand = "-m venv " + Quote(Path.Combine(projectRoot, "venv_windows"));
        if (!string.IsNullOrEmpty(pyLauncher))
            venvCreateCommand = "-3.11 " + venvCreateCommand;

        if (!RunProcess(pythonForVenv, venvCreateCommand))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Failed to create the Windows project venv.");
            return null;
        }

        string createdPython = Path.Combine(projectRoot, "venv_windows", "Scripts", "python.exe");
        if (!File.Exists(createdPython))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Windows venv was created but python.exe is missing.");
            return null;
        }

        string requirementsPath = Path.Combine(projectRoot, "requirements_windows.txt");
        if (File.Exists(requirementsPath) && !RunProcess(createdPython, "-m pip install -r " + Quote(requirementsPath)))
        {
            UnityEngine.Debug.LogError("[PythonPoseLauncher] Failed to install Windows Python requirements.");
            return null;
        }

        return createdPython;
    }

    private string FindMacPython311()
    {
        string[] candidates =
        {
            "python3.11",
            "/opt/homebrew/bin/python3.11",
            "/usr/local/bin/python3.11",
            "/usr/bin/python3"
        };

        foreach (string candidate in candidates)
        {
            if (CanRunPython(candidate, "--version"))
                return candidate;
        }

        return null;
    }

    private string FindWindowsPythonLauncher()
    {
        if (CanRunPython("py.exe", "-3.11 --version"))
            return "py.exe";

        return null;
    }

    private string FindWindowsPythonExecutable()
    {
        string[] candidates =
        {
            "python.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Python", "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python311", "python.exe")
        };

        foreach (string candidate in candidates)
        {
            if (CanRunPython(candidate, "--version"))
                return candidate;
        }

        return null;
    }

    private bool CanRunPython(string fileName, string arguments)
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
                return process.ExitCode == 0 && (output + error).Contains("Python");
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
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                    UnityEngine.Debug.Log($"[PythonPoseLauncher] setup stdout: {stdout.Trim()}");
                if (!string.IsNullOrEmpty(stderr))
                    UnityEngine.Debug.LogError($"[PythonPoseLauncher] setup stderr: {stderr.Trim()}");

                if (process.ExitCode != 0)
                    UnityEngine.Debug.LogError($"[PythonPoseLauncher] Command failed: {fileName} {arguments} (exit {process.ExitCode})");

                return process.ExitCode == 0;
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"[PythonPoseLauncher] Command failed: {fileName} {arguments}. Error: {exception.Message}");
            return false;
        }
    }

    private string ResolveBundledTrackerPath()
    {
        string[] candidateRoots = GetCandidateBuildRoots();
        foreach (string root in candidateRoots)
        {
            if (string.IsNullOrEmpty(root))
                continue;

            string[] candidatePaths = new[]
            {
                Path.Combine(root, "Tracking", "PoseTracker.exe"),
                Path.Combine(root, "Tracking", "PoseTracker"),
                Path.Combine(root, "Tracking", "PoseTracker.app", "Contents", "MacOS", "PoseTracker"),
                Path.Combine(root, "Tracking", "PoseTracker.app"),
                Path.Combine(root, "PoseTracker.exe"),
                Path.Combine(root, "PoseTracker")
            };

            foreach (string candidate in candidatePaths)
            {
                if (File.Exists(candidate))
                {
                    UnityEngine.Debug.Log($"[TRACKER] Bundled tracker found: {candidate}");
                    return candidate;
                }
            }
        }

        return null;
    }

    private void LaunchBundledTracker(string trackerPath)
    {
        UnityEngine.Debug.Log($"[TRACKER] Running in standalone build mode");
        UnityEngine.Debug.Log($"[TRACKER] Platform: {Application.platform}");
        UnityEngine.Debug.Log($"[TRACKER] Tracker path: {trackerPath}");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = trackerPath,
                WorkingDirectory = Path.GetDirectoryName(trackerPath),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            pythonProcess = Process.Start(startInfo);
            if (pythonProcess == null)
            {
                UnityEngine.Debug.LogError("[TRACKER] Bundled tracker start returned null.");
                return;
            }

            UnityEngine.Debug.Log($"[TRACKER] Tracker started. PID: {pythonProcess.Id}");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"[TRACKER] Failed to launch bundled tracker: {exception.Message}");
        }
    }

    private string[] GetCandidateBuildRoots()
    {
        var roots = new System.Collections.Generic.List<string>();

        string dataPath = Application.dataPath;
        if (!string.IsNullOrEmpty(dataPath))
            roots.Add(dataPath);

        string appDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appDirectory))
            roots.Add(appDirectory);

        string dataParent = Path.GetDirectoryName(dataPath);
        if (!string.IsNullOrEmpty(dataParent))
            roots.Add(dataParent);

        string appParent = Path.GetDirectoryName(appDirectory);
        if (!string.IsNullOrEmpty(appParent))
            roots.Add(appParent);

        string currentDirectory = Directory.GetCurrentDirectory();
        if (!string.IsNullOrEmpty(currentDirectory))
            roots.Add(currentDirectory);

        for (int i = 0; i < roots.Count; i++)
        {
            string currentRoot = roots[i];
            for (int depth = 0; depth < 8; depth++)
            {
                if (Directory.Exists(Path.Combine(currentRoot, "Tracking")))
                {
                    roots.Add(currentRoot);
                    break;
                }

                string parent = Directory.GetParent(currentRoot)?.FullName;
                if (string.IsNullOrEmpty(parent) || parent == currentRoot)
                    break;

                currentRoot = parent;
            }
        }

        return roots.ToArray();
    }

    private string FindProjectRoot()
    {
        string[] candidateRoots = new[]
        {
            Application.dataPath,
            Path.GetDirectoryName(Application.dataPath),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory,
            Path.GetDirectoryName(AppContext.BaseDirectory)
        };

        foreach (string root in candidateRoots)
        {
            if (string.IsNullOrEmpty(root))
                continue;

            try
            {
                string candidate = root;
                for (int i = 0; i < 8; i++)
                {
                    if (File.Exists(Path.Combine(candidate, scriptName)))
                        return candidate;
                    if (Directory.Exists(Path.Combine(candidate, "Assets")) && File.Exists(Path.Combine(candidate, "pose_test.py")))
                        return candidate;

                    string parent = Directory.GetParent(candidate)?.FullName;
                    if (string.IsNullOrEmpty(parent) || parent == candidate)
                        break;
                    candidate = parent;
                }
            }
            catch
            {
                // Ignore invalid directories while walking upward.
            }
        }

        if (Directory.Exists("Assets") && File.Exists(Path.Combine(".", scriptName)))
            return Directory.GetCurrentDirectory();

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
            StopPoseTracking();
    }

    private void StopPoseTracking()
    {
        if (pythonProcess == null)
            return;

        try
        {
            if (!pythonProcess.HasExited)
                pythonProcess.Kill();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[PythonPoseLauncher] Could not kill Python process cleanly: {exception.Message}");
        }
        finally
        {
            pythonProcess.Dispose();
            pythonProcess = null;
        }
    }
}

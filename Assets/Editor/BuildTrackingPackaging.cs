using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildTrackingPackaging : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPostprocessBuild(BuildReport report)
    {
        BuildTarget target = report.summary.platform;
        string outputPath = report.summary.outputPath;

        if (target == BuildTarget.StandaloneWindows ||
            target == BuildTarget.StandaloneWindows64 ||
            target == BuildTarget.StandaloneOSX)
        {
            PackageStandaloneTracking(target, outputPath);
        }
    }

    [MenuItem("Boxing AR/Package Standalone Tracking")]
    public static void PackageStandaloneTrackingMenu()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outputPath = GetDefaultBuildOutputPath(target);
        PackageStandaloneTracking(target, outputPath);
    }

    public static void PackageStandaloneTracking(BuildTarget target, string outputPath)
    {
        string projectRoot = GetProjectRoot();
        string trackerScript = Path.Combine(projectRoot, "pose_test.py");

        if (!File.Exists(trackerScript))
        {
            UnityEngine.Debug.LogError("[TRACKER-BUILD] pose_test.py was not found. Packaging aborted.");
            return;
        }

        string pythonPath = ResolvePythonForPackaging();
        if (string.IsNullOrEmpty(pythonPath))
        {
            UnityEngine.Debug.LogError("[TRACKER-BUILD] No usable Python executable was found for packaging. Install Python 3.11 and ensure PyInstaller is available.");
            return;
        }

        string buildRoot = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(buildRoot))
            buildRoot = projectRoot;

        string stagingDir = Path.Combine(projectRoot, "Temp", "PyInstaller");
        string distDir = Path.Combine(projectRoot, "Temp", "PyInstallerDist");
        Directory.CreateDirectory(stagingDir);
        Directory.CreateDirectory(distDir);

        string packageName = "PoseTracker";
        string pyinstallerArgs = BuildPyInstallerArguments(target, trackerScript, stagingDir, distDir, packageName);

        UnityEngine.Debug.Log($"[TRACKER-BUILD] Packaging tracker with Python: {pythonPath}");
        UnityEngine.Debug.Log($"[TRACKER-BUILD] Build output path: {outputPath}");
        UnityEngine.Debug.Log($"[TRACKER-BUILD] PyInstaller command: {pythonPath} {pyinstallerArgs}");

        if (!RunProcess(pythonPath, pyinstallerArgs, projectRoot))
        {
            UnityEngine.Debug.LogError("[TRACKER-BUILD] PyInstaller packaging failed.");
            return;
        }

        string finalTrackerPath = FindBuiltTracker(distDir, target);
        if (string.IsNullOrEmpty(finalTrackerPath))
        {
            UnityEngine.Debug.LogError("[TRACKER-BUILD] The packaged tracker executable could not be found after PyInstaller completed.");
            return;
        }

        string trackingDir = Path.Combine(buildRoot, "Tracking");
        Directory.CreateDirectory(trackingDir);

        if (Directory.Exists(finalTrackerPath))
        {
            CopyDirectory(finalTrackerPath, Path.Combine(trackingDir, Path.GetFileName(finalTrackerPath)));
        }
        else if (File.Exists(finalTrackerPath))
        {
            string targetTrackerPath = Path.Combine(trackingDir, Path.GetFileName(finalTrackerPath));
            File.Copy(finalTrackerPath, targetTrackerPath, true);
        }

        UnityEngine.Debug.Log($"[TRACKER-BUILD] Tracker packaged into: {trackingDir}");
        UnityEngine.Debug.Log($"[TRACKER-BUILD] Tracker file: {Path.Combine(trackingDir, Path.GetFileName(finalTrackerPath))}");
    }

    private static string BuildPyInstallerArguments(BuildTarget target, string trackerScript, string stagingDir, string distDir, string packageName)
    {
        string hiddenImports = "--hidden-import=mediapipe --hidden-import=cv2 --collect-all mediapipe --collect-all cv2";
        return $"-m PyInstaller --noconfirm --onefile --name {packageName} --distpath \"{distDir}\" --workpath \"{stagingDir}\" --specpath \"{stagingDir}\" {hiddenImports} \"{trackerScript}\"";
    }

    private static string ResolvePythonForPackaging()
    {
        string projectRoot = GetProjectRoot();
        string[] candidates = new[]
        {
            Path.Combine(projectRoot, "venv", "bin", "python"),
            Path.Combine(projectRoot, "venv", "bin", "python3.11"),
            Path.Combine(projectRoot, "venv_windows", "Scripts", "python.exe"),
            "python3.11",
            "python3",
            "python",
            "py.exe",
            "/opt/homebrew/bin/python3.11",
            "/usr/local/bin/python3.11",
            "/usr/bin/python3",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python311", "python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python311", "python.exe")
        };

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate))
                continue;

            if (candidate == "py.exe")
            {
                if (CanRunPython(candidate, "-3.11 --version"))
                    return candidate;
                continue;
            }

            if (File.Exists(candidate) && CanRunPython(candidate, "--version"))
                return candidate;

            if (IsCommandAvailable(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsCommandAvailable(string command)
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                process.Start();
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }
        catch
        {
            return false;
        }
    }

    private static bool CanRunPython(string fileName, string arguments)
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
                if (process == null)
                    return false;

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

    private static bool RunProcess(string fileName, string arguments, string workingDirectory)
    {
        try
        {
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                if (process == null)
                    return false;

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(stdout))
                    UnityEngine.Debug.Log($"[TRACKER-BUILD] {stdout.Trim()}");
                if (!string.IsNullOrEmpty(stderr))
                    UnityEngine.Debug.LogError($"[TRACKER-BUILD] {stderr.Trim()}");

                return process.ExitCode == 0;
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogError($"[TRACKER-BUILD] Command failed: {fileName} {arguments}. Error: {exception.Message}");
            return false;
        }
    }

    private static string FindBuiltTracker(string distDir, BuildTarget target)
    {
        List<string> candidates = new List<string>
        {
            Path.Combine(distDir, "PoseTracker.exe"),
            Path.Combine(distDir, "PoseTracker"),
            Path.Combine(distDir, "PoseTracker", "PoseTracker.exe"),
            Path.Combine(distDir, "PoseTracker", "PoseTracker"),
            Path.Combine(distDir, "PoseTracker.app", "Contents", "MacOS", "PoseTracker"),
            Path.Combine(distDir, "PoseTracker.app")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
            if (Directory.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string GetDefaultBuildOutputPath(BuildTarget target)
    {
        string projectRoot = GetProjectRoot();
        string buildFolder = Path.Combine(projectRoot, "Builds");
        Directory.CreateDirectory(buildFolder);

        if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            return Path.Combine(buildFolder, "Boxing-AR.exe");

        if (target == BuildTarget.StandaloneOSX)
            return Path.Combine(buildFolder, "Boxing-AR.app");

        return Path.Combine(buildFolder, "Boxing-AR");
    }

    private static string GetProjectRoot()
    {
        string dataPath = Application.dataPath;
        if (!string.IsNullOrEmpty(dataPath))
        {
            string parent = Directory.GetParent(dataPath)?.FullName;
            if (!string.IsNullOrEmpty(parent))
                return parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        if (!Directory.Exists(sourceDir))
            return;

        Directory.CreateDirectory(destinationDir);

        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destinationFile = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, destinationFile, true);
        }

        foreach (string directory in Directory.GetDirectories(sourceDir))
        {
            string destinationSubDir = Path.Combine(destinationDir, Path.GetFileName(directory));
            CopyDirectory(directory, destinationSubDir);
        }
    }
}

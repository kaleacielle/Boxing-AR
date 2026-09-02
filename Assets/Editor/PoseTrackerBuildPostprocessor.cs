using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class PoseTrackerBuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64)
            return;

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        string source = Path.Combine(projectRoot ?? string.Empty, "PoseTrackerWindows", "PoseTracker");
        if (!Directory.Exists(source))
        {
            throw new BuildFailedException(
                $"The packaged pose tracker is missing: {source}. " +
                "Generate PoseTrackerWindows before building.");
        }

        string buildDirectory = Path.GetFullPath(
            Path.GetDirectoryName(report.summary.outputPath) ?? string.Empty);
        string destination = Path.GetFullPath(Path.Combine(buildDirectory, "PoseTracker"));
        string allowedPrefix = buildDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!destination.StartsWith(allowedPrefix, StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("PoseTracker destination escaped the Windows build folder.");

        if (Directory.Exists(destination))
            Directory.Delete(destination, true);

        CopyDirectory(source, destination);
        Debug.Log($"Included standalone MediaPipe tracker: {destination}");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string file in Directory.GetFiles(source))
        {
            string target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, true);
        }

        foreach (string directory in Directory.GetDirectories(source))
        {
            string target = Path.Combine(destination, Path.GetFileName(directory));
            CopyDirectory(directory, target);
        }
    }
}

#if UNITY_EDITOR_WIN
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
internal sealed class AndroidGradleHomeIsolation : IPreprocessBuildWithReport
{
    static AndroidGradleHomeIsolation()
    {
        ConfigureGradleHome();
    }

    public int callbackOrder => -10000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android) return;
        ConfigureGradleHome();
        ClearLegacyNinjaDependencies();
    }

    private static void ConfigureGradleHome()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string previousHome = Path.Combine(projectRoot, "Library", "GradleUserHome");
        string configuredHome = Environment.GetEnvironmentVariable("GRADLE_USER_HOME");
        // Preserve an explicitly configured home, but migrate this project's
        // previous cache: its long path exceeds Windows' native build limit.
        if (!string.IsNullOrWhiteSpace(configuredHome)
            && !string.Equals(Path.GetFullPath(configuredHome), previousHome,
                StringComparison.OrdinalIgnoreCase))
            return;

        // Gradle daemons remain isolated from other projects while prefab
        // headers stay below the 260-character path limit used by Ninja.
        string gradleHome = Path.Combine(projectRoot, ".g");

        try
        {
            Directory.CreateDirectory(gradleHome);
            Environment.SetEnvironmentVariable("GRADLE_USER_HOME", gradleHome);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not isolate the Android Gradle user home: " + exception.Message);
        }
    }

    private static void ClearLegacyNinjaDependencies()
    {
        // This generated cache can retain absolute header paths even after
        // Gradle recreates build.ninja with the shorter home directory.
        string nativeBuildRoot = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), ".utmp");
        if (!Directory.Exists(nativeBuildRoot)) return;

        foreach (string dependencyFile in Directory.EnumerateFiles(nativeBuildRoot, ".ninja_deps", SearchOption.AllDirectories))
        {
            try
            {
                string dependencies = Encoding.UTF8.GetString(File.ReadAllBytes(dependencyFile));
                if (dependencies.IndexOf("/Library/GradleUserHome/", StringComparison.OrdinalIgnoreCase) < 0) continue;
                File.Delete(dependencyFile);
                Debug.Log("Cleared stale Android Ninja dependencies: " + dependencyFile);
            }
            catch (IOException exception)
            {
                Debug.LogWarning("Could not clear stale Android Ninja dependencies: " + exception.Message);
            }
        }
    }
}
#endif

#if UNITY_EDITOR_WIN
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class AndroidGradleHomeIsolation
{
    static AndroidGradleHomeIsolation()
    {
        // Gradle daemons are registered under GRADLE_USER_HOME. Keep this
        // project's Android builds separate from other Unity projects.
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GRADLE_USER_HOME")))
            return;

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string gradleHome = Path.Combine(projectRoot, "Library", "GradleUserHome");

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
}
#endif

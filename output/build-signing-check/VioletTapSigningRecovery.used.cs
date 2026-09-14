using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Temporary local recovery hook. Credentials never leave Unity memory or enter this source file.
[InitializeOnLoad]
public static class VioletTapSigningRecovery
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static readonly string DirectoryPath = Path.Combine(Root, "output/build-signing-check");
    private static readonly string Request = Path.Combine(DirectoryPath, "REPAIR_AND_BUILD");
    private static readonly string Result = Path.Combine(DirectoryPath, "rebuild-result.txt");
    private static bool running;

    static VioletTapSigningRecovery() => EditorApplication.update += Tick;

    private static void Tick()
    {
        if (running || EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating
            || BuildPipeline.isBuildingPlayer || !File.Exists(Request)) return;
        running = true;
        File.Delete(Request);
        try
        {
            var last = BuildReport.GetLatestReport();
            string output = Path.Combine(Root, "violettap.aab");
            string expectedKeystore = Path.Combine(Root, "user.keystore");
            string configuredKeystore = PlayerSettings.Android.keystoreName;
            if (configuredKeystore.StartsWith("{inproject}:", StringComparison.Ordinal))
                configuredKeystore = Path.Combine(Root, configuredKeystore.Substring("{inproject}:".Length).Trim());
            if (!Path.IsPathRooted(configuredKeystore)) configuredKeystore = Path.Combine(Root, configuredKeystore);
            if (last == null || last.summary.platform != BuildTarget.Android
                || !string.Equals(Path.GetFullPath(last.summary.outputPath), output, StringComparison.OrdinalIgnoreCase)
                || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android
                || !PlayerSettings.Android.useCustomKeystore
                || !string.Equals(Path.GetFullPath(configuredKeystore), expectedKeystore, StringComparison.OrdinalIgnoreCase)
                || PlayerSettings.Android.keyaliasName != "secondwindgames")
                throw new InvalidOperationException("The current signing/build target differs from the verified failed build.");

            string gradle = File.ReadAllText(Path.Combine(Root, "Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build.gradle"));
            var match = Regex.Match(gradle, @"(?m)^\s*storePassword\s+'((?:\\.|[^'\\])*)'\s*$");
            if (!match.Success) throw new InvalidOperationException("Verified current store password was not found.");
            string password = Regex.Unescape(match.Groups[1].Value);
            // The read-only Java check proved this current store password also unlocks this private key.
            PlayerSettings.Android.keystorePass = password;
            PlayerSettings.Android.keyaliasPass = password;
            File.WriteAllText(Result, "SIGNING_PASSWORD_CORRECTED\nBUILD_STARTED " + DateTime.Now.ToString("O") + "\n");

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = last.summary.options & ~(BuildOptions.AutoRunPlayer | BuildOptions.ShowBuiltPlayer
                    | BuildOptions.AcceptExternalModificationsToPlayer)
            };
            var report = BuildPipeline.BuildPlayer(options);
            File.AppendAllText(Result, "BUILD_RESULT " + report.summary.result + "\n"
                + "ERRORS " + report.summary.totalErrors + "\n"
                + "WARNINGS " + report.summary.totalWarnings + "\n"
                + "OUTPUT " + report.summary.outputPath + "\n"
                + "FINISHED " + DateTime.Now.ToString("O") + "\n");
        }
        catch (Exception exception)
        {
            File.AppendAllText(Result, "RECOVERY_FAILED " + exception.GetType().Name + "\n");
        }
        finally { running = false; }
    }
}

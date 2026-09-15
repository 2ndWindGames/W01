#if UNITY_EDITOR && UNITY_ANDROID
using System;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VioletTap.Editor
{
    internal sealed class AndroidEdgeToEdgeBuild : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            if (!PlayerSettings.Android.renderOutsideSafeArea)
                throw new BuildFailedException("Violet Tap: enable Player > Android > Resolution and Presentation > Render Outside Safe Area. The game already applies safe-area insets to its camera/UI.");
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            try
            {
                AndroidEdgeToEdgeProject.Apply(path);
                Debug.Log("Violet Tap: Android 15+ launch theme uses cutout mode ALWAYS; Unity's version-specific splash theme is preserved. Third-party SDK API warnings require separate verification.");
            }
            catch (Exception exception)
            {
                throw new BuildFailedException("Violet Tap Android edge-to-edge configuration failed: " + exception.Message);
            }
        }
    }
}
#endif

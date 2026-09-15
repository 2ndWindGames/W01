using System;
using System.IO;
using System.Text;
using System.Xml;

namespace VioletTap.Editor
{
    // Kept independent of Unity APIs so the generated Android resources can be
    // regression-tested without opening or modifying the user's current scene.
    public static class AndroidEdgeToEdgeProject
    {
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
        private const string ActivityName = "com.unity3d.player.UnityPlayerGameActivity";
        private const string ThemeName = "VioletTapGameActivityTheme";
        private const string ResourceFile = "violettap_edge_to_edge.xml";

        public static void Apply(string unityLibraryPath)
        {
            string manifestPath = Path.Combine(unityLibraryPath, "src/main/AndroidManifest.xml");
            var manifest = new XmlDocument { XmlResolver = null };
            manifest.Load(manifestPath);
            var namespaces = new XmlNamespaceManager(manifest.NameTable);
            namespaces.AddNamespace("android", AndroidNamespace);

            var activity = manifest.SelectSingleNode(
                "/manifest/application/activity[@android:name='" + ActivityName + "']", namespaces) as XmlElement;
            if (activity == null)
                throw new InvalidOperationException("Violet Tap requires UnityPlayerGameActivity. Check Android Application Entry Point.");

            string previousTheme = activity.GetAttribute("theme", AndroidNamespace);
            if (previousTheme != "@style/BaseUnityGameActivityTheme" && previousTheme != "@style/" + ThemeName)
                throw new InvalidOperationException("Unexpected Android game theme: " + previousTheme
                    + ". Review its parent before applying the edge-to-edge theme.");

            var safeAreaMetadata = manifest.SelectSingleNode(
                "/manifest/application/meta-data[@android:name='unity.render-outside-safearea']", namespaces) as XmlElement;
            if (safeAreaMetadata == null || !string.Equals(
                    safeAreaMetadata.GetAttribute("value", AndroidNamespace), "true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Enable Android Render Outside Safe Area. ResponsiveGameViewport handles the interactive safe area.");

            // The parent resolves to Unity's version-specific theme (including
            // Android 12's splash resources). Older Android versions stay unchanged.
            string basicTheme = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                + "<resources>\n"
                + "  <style name=\"" + ThemeName + "\" parent=\"@style/BaseUnityGameActivityTheme\" />\n"
                + "</resources>\n";
            string android15Theme = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                + "<resources>\n"
                + "  <style name=\"" + ThemeName + "\" parent=\"@style/BaseUnityGameActivityTheme\">\n"
                + "    <item name=\"android:windowLayoutInDisplayCutoutMode\">always</item>\n"
                + "  </style>\n"
                + "</resources>\n";

            // Only project-owned generated files are written. Never rewrite Unity
            // classes, Google SDK AARs, or third-party activities to hide a warning.
            WriteIfChanged(Path.Combine(unityLibraryPath, "src/main/res/values", ResourceFile), basicTheme);
            WriteIfChanged(Path.Combine(unityLibraryPath, "src/main/res/values-v35", ResourceFile), android15Theme);
            if (previousTheme != "@style/" + ThemeName)
            {
                activity.SetAttribute("theme", AndroidNamespace, "@style/" + ThemeName);
                manifest.Save(manifestPath);
            }
        }

        private static void WriteIfChanged(string path, string contents)
        {
            if (File.Exists(path) && File.ReadAllText(path) == contents) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }
    }
}

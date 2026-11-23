#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Memeopoly.Editor
{
    /// <summary>
    /// Compiles the real Unity player for GitHub Pages.
    /// Call with -executeMethod Memeopoly.Editor.MemeopolyWebGLBuild.Build.
    /// </summary>
    public static class MemeopolyWebGLBuild
    {
        [MenuItem("Memeopoly/Build WebGL Preview")]
        public static void Build()
        {
            // The first import generates the scene if it doesn't exist yet.
            MemeopolyProjectSetup.OpenBoard();
            const string scene = "Assets/Scenes/Memeopoly.unity";
            if (!File.Exists(scene))
                throw new FileNotFoundException("Memeopoly Unity scene did not generate.", scene);

            // Build destination deliberately lives OUTSIDE the Unity project.
            // GitHub Actions uploads this exact directory to Pages.
            string outputPath = Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", "..", "build", "WebGL", "Memeopoly"));
            Directory.CreateDirectory(outputPath);

            PlayerSettings.companyName = "Wilbur Wonka Studio";
            PlayerSettings.productName = "Memeopoly - Unity Test";
            PlayerSettings.defaultWebScreenWidth = 430;
            PlayerSettings.defaultWebScreenHeight = 900;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Memeopoly WebGL compilation FAILED: " + report.summary.result +
                                    "; errors: " + report.summary.totalErrors);

            string entry = Path.Combine(outputPath, "index.html");
            if (!File.Exists(entry))
                throw new FileNotFoundException("Unity produced no launch page.", entry);
            Debug.Log("MEMEOPOLY UNITY WEBGL BUILD SUCCEEDED: " + entry);
        }
    }
}
#endif

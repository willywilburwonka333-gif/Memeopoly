#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Memeopoly.Editor
{
    /// <summary>
    /// Unity Build Automation does not use our custom manual build method.
    /// This hook sets responsive WebGL settings for *every* Unity Cloud build.
    /// The template itself lives in Assets/WebGLTemplates/MemeopolyMobile.
    /// </summary>
    public sealed class MemeopolyMobileWebGLPrebuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -1000; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform != BuildTarget.WebGL) return;
            Apply();
        }

        public static void Apply()
        {
            PlayerSettings.WebGL.template = "PROJECT:MemeopolyMobile";
            PlayerSettings.defaultWebScreenWidth = 430;
            PlayerSettings.defaultWebScreenHeight = 900;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            Debug.Log("Memeopoly: full-device responsive WebGL template enabled.");
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Memeopoly.Editor
{
    // Makes a working first scene automatically. No handwritten Unity YAML,
    // missing prefab GUIDs or drag-and-drop setup is required.
    [InitializeOnLoad]
    public static class MemeopolyProjectSetup
    {
        private const string SceneFile = "Assets/Scenes/Memeopoly.unity";

        static MemeopolyProjectSetup()
        {
            EditorApplication.delayCall += EnsureScene;
        }

        [MenuItem("Memeopoly/Open Playable Board")]
        public static void OpenBoard()
        {
            EnsureScene();
            if (File.Exists(SceneFile))
                EditorSceneManager.OpenScene(SceneFile, OpenSceneMode.Single);
        }

        private static void EnsureScene()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!File.Exists(SceneFile))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("Memeopoly");
                go.AddComponent<MemeopolyGame>();
                EditorSceneManager.SaveScene(scene, SceneFile);
                AssetDatabase.Refresh();
                Debug.Log("Memeopoly: created fully playable Unity scene at " + SceneFile);
            }

            var buildScenes = EditorBuildSettings.scenes;
            bool registered = false;
            foreach (var entry in buildScenes) if (entry.path == SceneFile) registered = true;
            if (!registered)
                EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(SceneFile, true) };

            if (EditorSceneManager.GetActiveScene().path == "" && File.Exists(SceneFile))
                EditorSceneManager.OpenScene(SceneFile, OpenSceneMode.Single);
        }
    }
}
#endif
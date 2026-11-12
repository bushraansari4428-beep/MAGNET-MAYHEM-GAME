#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MagnetMayhem.EditorTools
{
    [InitializeOnLoad]
    public static class MagnetMayhemEditorBuilder
    {
        static MagnetMayhemEditorBuilder()
        {
            EnsureSceneInBuildSettings();
        }

        public static void EnsureSceneInBuildSettings()
        {
            string scenePath = "Assets/Scenes/MainScene.unity";
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            bool found = false;
            if (currentScenes != null)
            {
                foreach (var s in currentScenes)
                {
                    if (s != null && s.path == scenePath)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
                Debug.Log("✅ Registered Assets/Scenes/MainScene.unity into EditorBuildSettings.");
            }
        }

        public static void BuildAndroid()
        {
            Debug.Log("🚀 Starting Magnet Mayhem Android Build Pipeline...");
            EnsureSceneInBuildSettings();

            PlayerSettings.companyName = "NeonArcadeStudios";
            PlayerSettings.productName = "Magnet Mayhem";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.NeonArcadeStudios.MagnetMayhem");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            string outDir = "build/Android";
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new string[] { "Assets/Scenes/MainScene.unity" },
                locationPathName = "build/Android/MagnetMayhem.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"🎉 APK Build Succeeded! Total size: {summary.totalSize} bytes at {summary.outputPath}");
            }
            else
            {
                Debug.LogError($"💥 APK Build Failed with result {summary.result}, errors: {summary.totalErrors}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Tools/Magnet Mayhem/Setup Complete Game Scene")]
        public static void SetupCompleteScene()
        {
            PlayerSettings.companyName = "NeonArcadeStudios";
            PlayerSettings.productName = "MAGNET MAYHEM";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            EnsureSceneInBuildSettings();

            MagnetMayhemGame existing = Object.FindObjectOfType<MagnetMayhemGame>();
            if (existing == null)
            {
                GameObject engineObj = new GameObject("MagnetMayhem_MasterEngine");
                engineObj.AddComponent<MagnetMayhemGame>();
                Undo.RegisterCreatedObjectUndo(engineObj, "Create Magnet Mayhem Engine");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✅ MAGNET MAYHEM Scene & Portrait Settings Configured! Press PLAY to start.");
        }

        [MenuItem("Tools/Magnet Mayhem/Reset Saved Progress")]
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("🔄 MAGNET MAYHEM PlayerPrefs Reset Complete!");
        }
    }
}
#endif

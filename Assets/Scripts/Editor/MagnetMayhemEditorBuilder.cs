#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MagnetMayhem.EditorTools
{
    public static class MagnetMayhemEditorBuilder
    {
        [MenuItem("Tools/Magnet Mayhem/Setup Complete Game Scene")]
        public static void SetupCompleteScene()
        {
            // Configure Portrait 9:16 Player Settings for Android / iOS
            PlayerSettings.companyName = "NeonArcadeStudios";
            PlayerSettings.productName = "MAGNET MAYHEM";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Ensure Master Engine exists in current Scene
            MagnetMayhemGame existing = Object.FindObjectOfType<MagnetMayhemGame>();
            if (existing == null)
            {
                GameObject engineObj = new GameObject("MagnetMayhem_MasterEngine");
                engineObj.AddComponent<MagnetMayhemGame>();
                Undo.RegisterCreatedObjectUndo(engineObj, "Create Magnet Mayhem Engine");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✅ MAGNET MAYHEM Scene & Portrait 1080x1920 Settings Configured! Press PLAY to start.");
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

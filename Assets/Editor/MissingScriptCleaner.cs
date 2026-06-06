using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class MissingScriptCleaner
{
    [MenuItem("Tools/Remove Missing Scripts In All Scenes")]
    static void RemoveMissingScriptsInAllScenes()
    {
        string[] scenePaths = System.IO.Directory.GetFiles(
            Application.dataPath, "*.unity", System.IO.SearchOption.AllDirectories);

        int totalRemoved = 0;

        foreach (string scenePath in scenePaths)
        {
            string assetPath = "Assets" + scenePath.Replace(Application.dataPath, "").Replace("\\", "/");
            Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);

            int removed = 0;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene != scene) continue;
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                if (count > 0)
                {
                    removed += count;
                    EditorUtility.SetDirty(go);
                }
            }

            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Removed {removed} missing script(s) from scene: {scene.name}");
            }

            totalRemoved += removed;
        }

        Debug.Log($"Scenes done. Total removed: {totalRemoved}");
    }

    [MenuItem("Tools/Remove Missing Scripts In All Prefabs")]
    static void RemoveMissingScriptsInAllPrefabs()
    {
        string[] prefabPaths = System.IO.Directory.GetFiles(
            Application.dataPath, "*.prefab", System.IO.SearchOption.AllDirectories);

        int totalRemoved = 0;

        foreach (string prefabPath in prefabPaths)
        {
            string assetPath = "Assets" + prefabPath.Replace(Application.dataPath, "").Replace("\\", "/");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;

            int removed = RemoveFromHierarchy(prefab);
            if (removed > 0)
            {
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log($"Removed {removed} missing script(s) from prefab: {assetPath}");
                totalRemoved += removed;
            }
        }

        Debug.Log($"Prefabs done. Total removed: {totalRemoved}");
    }

    static int RemoveFromHierarchy(GameObject root)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        foreach (Transform child in root.transform)
            count += RemoveFromHierarchy(child.gameObject);
        return count;
    }
}

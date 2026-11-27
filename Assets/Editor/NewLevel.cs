using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Tools > Garden Defense > New Level From Open Level (Tommy, 2026-11-27): makes the next level (one past the
// highest LevelN scene) from the level that's open. the scene is copied with everything in it (camera, managers,
// route objects, lights, music, weather) and so is its LevelConfig (waves, sun, health, weather), given the new
// level number and the old level's biome (left on Auto it would go by the new number). the copy's map is
// cleared: every tilemap beside the Ground emptied (the cliffs go with the raised ground) and, on a day level,
// the Ground filled with the clover brush. the new scene goes into the build settings and opens with the Route
// Builder beside it: paint the map, then build the route
public static class NewLevel
{
    private const string ScenesFolder = "Assets/Scenes/";
    private const string ConfigsFolder = "Assets/ScriptableObjects/LevelData/";
    private const string CloverBrushPath = "Assets/Tilesets/officialTilesets/dayFiles/Brushes/CloverBrush.asset";

    [MenuItem("Tools/Garden Defense/New Level From Open Level")]
    public static void Make()
    {
        Scene source = SceneManager.GetActiveScene();
        ProceduralLevel level = Object.FindAnyObjectByType<ProceduralLevel>();
        if (string.IsNullOrEmpty(source.path) || level == null || level.config == null)
        {
            EditorUtility.DisplayDialog("New Level", "Open a level first (a scene whose SpawnPoint has a ProceduralLevel with a LevelConfig).", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int n = NextNumber();
        string scenePath = ScenesFolder + "Level" + n + ".unity";
        string configPath = ConfigsFolder + "Level" + n + ".asset";
        if (File.Exists(scenePath) || File.Exists(configPath))
        {
            EditorUtility.DisplayDialog("New Level", $"{scenePath} or {configPath} is already there.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("New Level",
                $"Make Level {n} from {source.name}?\n\nIt copies the scene and its waves ({level.config.name} to Level{n}), " +
                $"clears the map, and adds Level {n} to the build settings.", $"Make Level {n}", "Cancel"))
            return;

        // the waves and settings, with the new number and the old biome
        LevelConfig oldConfig = level.config;
        if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(oldConfig), configPath))
        {
            EditorUtility.DisplayDialog("New Level", "Couldn't copy " + oldConfig.name + ".", "OK");
            return;
        }
        LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);
        config.levelNumber = n;
        config.biome = oldConfig.CurrentBiome;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        // the scene
        if (!EditorSceneManager.SaveScene(source, scenePath, true))
        {
            EditorUtility.DisplayDialog("New Level", "Couldn't save the copy to " + scenePath + ".", "OK");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        ProceduralLevel copy = Object.FindAnyObjectByType<ProceduralLevel>();
        if (copy != null)
        {
            copy.config = config;
            copy.testLevel = false;
            EditorUtility.SetDirty(copy);
            PrefabUtility.RecordPrefabInstancePropertyModifications(copy);
        }
        ClearMap(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        List<EditorBuildSettingsScene> build = EditorBuildSettings.scenes.ToList();
        if (build.All(s => s.path != scenePath))
        {
            build.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = build.ToArray();
        }

        RouteBuilder.Open();
        EditorUtility.DisplayDialog("New Level",
            $"Level {n} is open with an empty map.\n\n" +
            "1. Paint it with the Tile Palette (the brushes are on the day palette's top row).\n" +
            "2. In the Route Builder, pick the exit and press Build route.\n" +
            $"3. Set its waves in Level{n}.asset and give it a button in the level selector.", "OK");
    }

    private static int NextNumber()
    {
        int max = 0;
        foreach (string file in Directory.GetFiles(ScenesFolder, "Level*.unity"))
        {
            Match m = Regex.Match(Path.GetFileNameWithoutExtension(file), @"^Level(\d+)$");
            if (m.Success) max = Mathf.Max(max, int.Parse(m.Groups[1].Value));
        }
        return max + 1;
    }

    // every tilemap beside the Ground emptied; a Ground of day clover filled again with the clover brush
    private static void ClearMap(Scene scene)
    {
        Tilemap ground = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
            .FirstOrDefault(m => m.name == "Ground" && m.gameObject.scene == scene);
        if (ground == null || ground.transform.parent == null) return;
        foreach (Tilemap map in ground.transform.parent.GetComponentsInChildren<Tilemap>(true))
            if (map != ground) map.ClearAllTiles();

        TileBase clover = AssetDatabase.LoadAssetAtPath<TileBase>(CloverBrushPath);
        var cells = new List<Vector3Int>();
        bool day = false;
        foreach (Vector3Int p in ground.cellBounds.allPositionsWithin)
        {
            TileBase tile = ground.GetTile(p);
            if (tile == null) continue;
            cells.Add(p);
            if (tile == clover || tile.name.StartsWith("GrassClover")) day = true;
        }
        if (clover == null || !day) return;
        ground.SetTiles(cells.ToArray(), Enumerable.Repeat(clover, cells.Count).ToArray());
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Garden Defense > Bake Planks in Open Scene (Tommy, 2026-11-25): turns the open scene's buttons
// into the kit's planks the scene would otherwise make when it starts, and saves the scene, so the
// editor shows them too. MainMenu: its six buttons (MainMenu.SkinButtons). LevelSelector: every level
// button (LevelSelector.SkinButtons) and the loadout's Next (LoadoutSelectionUI.BuildConfirmPlank).
// running it again only restyles them
public static class MainMenuPlanks
{
    [MenuItem("Tools/Garden Defense/Bake Planks in Open Scene")]
    private static void Bake()
    {
        MainMenu menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        LevelSelector selector = Object.FindAnyObjectByType<LevelSelector>(FindObjectsInactive.Include);
        LoadoutSelectionUI loadout = Object.FindAnyObjectByType<LoadoutSelectionUI>(FindObjectsInactive.Include);
        if (menu == null && selector == null && loadout == null)
        {
            EditorUtility.DisplayDialog("Bake Planks", "Open the MainMenu or the LevelSelector scene first.", "OK");
            return;
        }
        if (menu != null) menu.SkinButtons();
        if (selector != null) selector.SkinButtons();
        if (loadout != null) loadout.BuildConfirmPlank();

        GameObject any = menu != null ? menu.gameObject : selector != null ? selector.gameObject : loadout.gameObject;
        EditorSceneManager.MarkSceneDirty(any.scene);
        EditorSceneManager.SaveScene(any.scene);
        Debug.Log($"Planks baked into {any.scene.name} and the scene saved.");
    }
}

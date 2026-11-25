using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Garden Defense > Bake Main Menu Planks (Tommy, 2026-11-25): turns the open MainMenu scene's
// buttons into the big planks MainMenu would otherwise make when the scene starts (MainMenu.SkinButtons)
// and saves the scene, so the editor shows them too. running it again only restyles them
public static class MainMenuPlanks
{
    [MenuItem("Tools/Garden Defense/Bake Main Menu Planks")]
    private static void Bake()
    {
        MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Bake Main Menu Planks", "Open the MainMenu scene first.", "OK");
            return;
        }
        menu.SkinButtons();
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        EditorSceneManager.SaveScene(menu.gameObject.scene);
        Debug.Log("Main menu planks baked into the MainMenu scene and saved.");
    }
}

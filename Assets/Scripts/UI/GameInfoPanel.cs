using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Text;
using System.Collections.Generic;

// the active effects window, part of the real game (unlike the mulch test menu on n): press G
// to toggle. shows the current weather and temperature, every plant's mulches with their rarity,
// description and live status (waves left and so on), the level's fertilizer, and cumulative sun
// generated and spent this level.
// builds its own UI hierarchy at runtime rather than relying on a scene-dragged prefab, so it
// needs no Editor wiring - just press play and press G
public class GameInfoPanel : MonoBehaviour
{
    public static GameInfoPanel instance;

    private GameObject panelRoot;
    private TMP_Text bodyText;
    private readonly StringBuilder sb = new StringBuilder();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("GameInfoPanel");
        instance = go.AddComponent<GameInfoPanel>();
    }

    private void Awake()
    {
        // an old copy of this component still sits on SettingsManager.prefab (SettingsPanel >
        // GameInfoPanel), from when this was a text box inside the settings menu. it loads before
        // Bootstrap runs, so it used to claim the instance, and its Update only ran while settings
        // was open, exactly when G is blocked. any copy that isn't a root object is that leftover:
        // drop just this component (never its object) and let Bootstrap make the real one
        if (transform.parent != null) { Destroy(this); return; }
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.gKey.wasPressedThisFrame && CanToggle())
            SetOpen(!IsOpen);

        if (IsOpen)
            Refresh();
    }

    // same spirit as SettingsManager's own escape gating: don't pop this up over another modal,
    // and only while a level is actually being played
    private bool CanToggle()
    {
        if (GameManager.instance == null || !GameManager.instance.IsGameActive) return false;
        if (SettingsManager.instance != null && SettingsManager.instance.IsOpen) return false;
        if (LoadoutSelectionUI.instance != null && LoadoutSelectionUI.instance.IsOpen) return false;
        if (FertilizerSelectionUI.instance != null && FertilizerSelectionUI.instance.IsOpen) return false;
        if (MulchDebugMenu.instance != null && MulchDebugMenu.instance.IsOpen) return false;
        return true;
    }

    private void SetOpen(bool open)
    {
        panelRoot.SetActive(open);
        if (open) Refresh();
    }

    private void Refresh()
    {
        sb.Length = 0;

        sb.AppendLine("<b><size=150%>Active Effects</size></b>");
        sb.AppendLine();
        AppendWeatherSection();
        sb.AppendLine();
        AppendMulchSection();
        sb.AppendLine();
        AppendFertilizerSection();
        sb.AppendLine();
        AppendSunSection();

        // the panel stays live, it is built again every frame, but the text is only handed to
        // the text box (a new string, a new mesh and a new layout) when it changed
        if (shownText != null && Matches(sb, shownText)) return;
        shownText = sb.ToString();
        bodyText.text = shownText;
    }

    // the text the body last got
    private string shownText;

    // compares the builder with the shown string without making a new string
    private static bool Matches(StringBuilder builder, string text)
    {
        if (builder.Length != text.Length) return false;
        for (int i = 0; i < text.Length; i++)
            if (builder[i] != text[i]) return false;
        return true;
    }

    private void AppendWeatherSection()
    {
        sb.AppendLine("<b><size=120%>Weather</size></b>");
        if (WeatherManager.instance == null)
        {
            sb.AppendLine("<color=#888888>No weather manager in this scene.</color>");
            return;
        }

        bool any = false;
        foreach (WeatherEntry entry in WeatherManager.instance.GetActiveWeather())
        {
            any = true;
            sb.Append(WeatherManager.GetWeatherName(entry.type)).Append(" <color=#888888>(intensity ").Append(entry.intensity).AppendLine(")</color>");
            sb.Append("<size=85%><color=#c3c2b7>").Append(WeatherManager.GetWeatherDescription(entry.type, entry.intensity)).AppendLine("</color></size>");
        }
        if (!any)
            sb.AppendLine("<color=#888888>Clear skies.</color>");

        sb.AppendLine();
        sb.Append(WeatherManager.GetTemperatureName(WeatherManager.instance.temperature)).AppendLine(" <color=#888888>(temperature)</color>");
        sb.Append("<size=85%><color=#c3c2b7>").Append(WeatherManager.GetTemperatureDescription(WeatherManager.instance.temperature)).AppendLine("</color></size>");
    }

    // the level's fertilizer (picked before the level), with what it does and its live status
    private void AppendFertilizerSection()
    {
        sb.AppendLine("<b><size=120%>Fertilizer</size></b>");
        FertilizerManager fertilizers = FertilizerManager.instance;
        if (fertilizers == null)
        {
            sb.AppendLine("<color=#888888>No fertilizer manager in this scene.</color>");
            return;
        }
        FertilizerData active = fertilizers.Active;
        if (active == null)
        {
            sb.AppendLine("<color=#888888>No fertilizer this level.</color>");
            return;
        }
        sb.Append("<b>").Append(active.fertilizerName).AppendLine("</b>");
        sb.Append("<size=85%><color=#c3c2b7>").Append(active.GetDescription()).AppendLine("</color></size>");
        string status = fertilizers.ActiveStatus;
        if (!string.IsNullOrEmpty(status))
            sb.Append("<size=85%><color=#FFD27F>").Append(status).AppendLine("</color></size>");
    }

    // every living plant's equipped mulches, with rarity, description and live status, and how
    // full the inventory is
    private void AppendMulchSection()
    {
        sb.AppendLine("<b><size=120%>Mulches</size></b>");
        if (MulchManager.instance == null)
        {
            sb.AppendLine("<color=#888888>No mulch manager in this scene.</color>");
            return;
        }

        int held = 0;
        foreach (MulchItem item in MulchManager.instance.Inventory)
            if (item != null) held++;
        sb.Append("<color=#888888>Inventory: ").Append(held).Append('/').Append(MulchManager.instance.InventorySize).AppendLine("</color>");

        bool any = false;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant.mulches.Count == 0) continue;
            any = true;
            sb.AppendLine();
            sb.Append(plant.GetName()).Append(" <color=#888888>(").Append(plant.mulches.Count).Append('/').Append(MulchManager.instance.PlantSlots).AppendLine(")</color>");
            foreach (MulchEffect effect in plant.mulches)
            {
                if (effect == null) continue;
                sb.Append("<indent=4%><b>").Append(effect.Item.Name).Append("</b> <size=85%>").Append(MulchData.RarityLabel(effect.Rarity)).AppendLine("</size></indent>");
                sb.Append("<indent=4%><size=85%><color=#c3c2b7>").Append(effect.Data.GetDescription(effect.Item, effect)).AppendLine("</color></size></indent>");
                string status = effect.GetStatus();
                if (!string.IsNullOrEmpty(status))
                    sb.Append("<indent=4%><size=85%><color=#FFD27F>").Append(status).AppendLine("</color></size></indent>");
            }
        }
        if (!any)
            sb.AppendLine("<color=#888888>No plant has a mulch yet.</color>");
    }

    private void AppendSunSection()
    {
        sb.AppendLine("<b><size=120%>Sun</size></b>");
        if (GameManager.instance == null)
        {
            sb.AppendLine("<color=#888888>No game manager in this scene.</color>");
            return;
        }

        sb.Append("Current: <color=#FFD700><b>").Append(GameManager.instance.SunCount).AppendLine("</b></color>");
        sb.Append("Generated this level: <color=green><b>").Append(GameManager.instance.totalSunGenerated).AppendLine("</b></color>");
        sb.Append("Spent this level: <color=red><b>").Append(GameManager.instance.totalSunSpent).AppendLine("</b></color>");
    }

    // ── UI construction ─────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("GameInfoPanelCanvas");
        canvasGO.transform.SetParent(transform, false);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("Panel");
        panelRoot.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(560f, 840f);

        Image bg = panelRoot.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.04f, 0.88f);

        GameObject scrollGO = new GameObject("Scroll");
        scrollGO.transform.SetParent(panelRoot.transform, false);
        RectTransform scrollRect = scrollGO.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(12f, 12f);
        scrollRect.offsetMax = new Vector2(-12f, -12f);
        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        // clips by rectangle. a stencil Mask here hid every line: its graphic is fully transparent,
        // unity skips drawing fully transparent meshes, so the mask was never written and all the
        // text got clipped away. the clear image stays only so the mouse wheel scrolls anywhere
        scrollGO.AddComponent<RectMask2D>();
        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0f);

        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = scrollRect;

        bodyText = contentGO.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 22f;
        bodyText.color = Color.white;
        bodyText.richText = true;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;

        GameObject hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(panelRoot.transform, false);
        RectTransform hintRect = hintGO.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 4f);
        hintRect.sizeDelta = new Vector2(0f, 24f);
        TMP_Text hintText = hintGO.AddComponent<TextMeshProUGUI>();
        hintText.text = "<color=#888888>Press G to close</color>";
        hintText.fontSize = 16f;
        hintText.alignment = TextAlignmentOptions.Center;

        panelRoot.SetActive(false);
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Text;
using System.Collections.Generic;

// the active effects window, part of the real game (unlike the mulch test menu on n): press G
// to toggle. shows the current weather and temperature, every fertilizer in effect with its own
// stats (plus the combined total), every owned mulch with its tier, description and live status
// (waves left and so on), and cumulative sun generated and spent this level.
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
        if (MulchSelectionUI.instance != null && MulchSelectionUI.instance.IsOpen) return false;
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

        bodyText.text = sb.ToString();
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
            sb.AppendLine($"{WeatherManager.GetWeatherName(entry.type)} <color=#888888>(intensity {entry.intensity})</color>");
            sb.AppendLine($"<size=85%><color=#c3c2b7>{WeatherManager.GetWeatherDescription(entry.type, entry.intensity)}</color></size>");
        }
        if (!any)
            sb.AppendLine("<color=#888888>Clear skies.</color>");

        sb.AppendLine();
        sb.AppendLine($"{WeatherManager.GetTemperatureName(WeatherManager.instance.temperature)} <color=#888888>(temperature)</color>");
        sb.AppendLine($"<size=85%><color=#c3c2b7>{WeatherManager.GetTemperatureDescription(WeatherManager.instance.temperature)}</color></size>");
    }

    // each fertilizer on its own (the pre level pick, then every mid level one in pick order) with
    // its stats, then the combined total once there's more than one
    private void AppendFertilizerSection()
    {
        sb.AppendLine("<b><size=120%>Fertilizers</size></b>");
        FertilizerManager fertilizers = FertilizerManager.instance;
        if (fertilizers == null)
        {
            sb.AppendLine("<color=#888888>No fertilizer manager in this scene.</color>");
            return;
        }

        int count = 0;
        FertilizerData preLevel = fertilizers.ActiveFertilizer;
        if (preLevel != null)
        {
            count++;
            sb.AppendLine($"<b>{preLevel.fertilizerName}</b> {TierLabel(preLevel.tier)} <color=#888888>(picked before the level)</color>");
            string targets = preLevel.appliesToAll ? "All plants" : ElementList(preLevel.targetElementalTypes);
            if (!string.IsNullOrEmpty(targets))
                sb.AppendLine($"<size=85%><color=#c3c2b7>{targets}</color></size>");
            foreach ((StatType statType, float value) in SortedByName(fertilizers.GetPreLevelStats()))
                sb.AppendLine($"<indent=4%>{StatLine(statType, value)}</indent>");
        }

        foreach (GeneratedFertilizer generated in fertilizers.CommittedGenerated)
        {
            if (generated == null || generated.stats == null) continue;
            count++;
            sb.AppendLine($"<b>{TierLabel(generated.tier)} Fertilizer</b>");
            var stats = new List<(StatType statType, float value)>();
            var scopes = new Dictionary<StatType, string>();
            foreach (GeneratedFertilizerStat stat in generated.stats)
            {
                stats.Add((stat.statType, stat.value));
                scopes[stat.statType] = ScopeLabel(stat.scope);
            }
            foreach ((StatType statType, float value) in SortedByName(stats))
            {
                string scope = scopes.TryGetValue(statType, out string s) && !string.IsNullOrEmpty(s) ? $" <size=80%><color=#888888>({s})</color></size>" : "";
                sb.AppendLine($"<indent=4%>{StatLine(statType, value)}{scope}</indent>");
            }
        }

        if (count == 0)
        {
            sb.AppendLine("<color=#888888>No fertilizers active.</color>");
            return;
        }
        if (count < 2) return;

        sb.AppendLine("<b>Total</b>");
        foreach ((StatType statType, float value) in fertilizers.GetCombinedActiveStats())
            sb.AppendLine($"<indent=4%>{StatLine(statType, value)}</indent>");
    }

    private static string StatLine(StatType statType, float value)
    {
        bool isGood = value >= 0f;
        if (FertilizerFormat.IsInvertedStat(statType)) isGood = !isGood;
        string valueColor = isGood ? "green" : "red";
        string statColor = FertilizerFormat.GetStatColor(statType);
        return $"<color={statColor}><b>{FertilizerFormat.FormatStatName(statType)}</b></color>: <color={valueColor}><b>{FertilizerFormat.FormatValue(statType, value)}</b></color>";
    }

    private static List<(StatType statType, float value)> SortedByName(List<(StatType statType, float value)> stats)
    {
        StatType[] types = new StatType[stats.Count];
        for (int i = 0; i < stats.Count; i++) types[i] = stats[i].statType;
        var result = new List<(StatType statType, float value)>(stats.Count);
        foreach (int i in FertilizerFormat.SortedIndicesByName(types))
            result.Add(stats[i]);
        return result;
    }

    // fertilizer tiers share the mulch tier names and card colors
    private static string TierLabel(FertilizerTier tier) => MulchData.TierLabel((MulchTier)(int)tier);

    private static string ElementList(ElementalType[] elements)
    {
        if (elements == null || elements.Length == 0) return "";
        var names = new List<string>();
        foreach (ElementalType element in elements)
            names.Add($"<color={PlantData.ElementalColor(element)}>{element}</color>");
        return $"{string.Join(", ", names)} plants";
    }

    // who one mid level stat lands on, empty when it's every plant
    private static string ScopeLabel(FertilizerStatRules.StatScope scope)
    {
        if (scope.appliesToAll) return "";
        var parts = new List<string>();
        if (scope.requiredElement.HasValue)
            parts.Add($"<color={PlantData.ElementalColor(scope.requiredElement.Value)}>{scope.requiredElement.Value}</color> plants");
        if (scope.requiredDamageType.HasValue)
            parts.Add($"{scope.requiredDamageType.Value} damage plants");
        if (scope.requiredPlantNames != null && scope.requiredPlantNames.Count > 0)
            parts.Add(string.Join(", ", scope.requiredPlantNames));
        if (scope.excludeMelee)
            parts.Add("ranged plants");
        return string.Join(", ", parts);
    }

    // every owned mulch, in pick order: name and tier, its description with the live numbers, and
    // its status line (waves left and so on) when it has one
    private void AppendMulchSection()
    {
        sb.AppendLine("<b><size=120%>Mulches</size></b>");
        if (MulchManager.instance == null)
        {
            sb.AppendLine("<color=#888888>No mulch manager in this scene.</color>");
            return;
        }

        if (MulchManager.instance.ActiveMulches.Count == 0)
        {
            sb.AppendLine("<color=#888888>No mulches active.</color>");
            return;
        }

        foreach (MulchData mulch in MulchManager.instance.ActiveMulches)
        {
            sb.AppendLine($"<b>{mulch.mulchName}</b> <size=85%>{MulchData.TierLabel(mulch.tier)}</size>");
            sb.AppendLine($"<size=85%><color=#c3c2b7>{mulch.GetDescription()}</color></size>");
            string status = MulchManager.instance.GetStatus(mulch);
            if (!string.IsNullOrEmpty(status))
                sb.AppendLine($"<size=85%><color=#FFD27F>{status}</color></size>");
            sb.AppendLine();
        }
    }

    private void AppendSunSection()
    {
        sb.AppendLine("<b><size=120%>Sun</size></b>");
        if (GameManager.instance == null)
        {
            sb.AppendLine("<color=#888888>No game manager in this scene.</color>");
            return;
        }

        sb.AppendLine($"Current: <color=#FFD700><b>{GameManager.instance.SunCount}</b></color>");
        sb.AppendLine($"Generated this level: <color=green><b>{GameManager.instance.totalSunGenerated}</b></color>");
        sb.AppendLine($"Spent this level: <color=red><b>{GameManager.instance.totalSunSpent}</b></color>");
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
        Mask mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
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

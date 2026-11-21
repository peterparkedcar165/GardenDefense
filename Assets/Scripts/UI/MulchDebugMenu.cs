using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

// dev test menu, n in a level (Unity editor or a development build only, see GameManager): set the
// weather, switch the level's fertilizer to any fertilizer, put any mulch at any rarity straight
// into the inventory (or onto the ground when it's full), or pop a random drop next to the camera's
// centre to watch it hop and land. the game pauses while it's open. built in code on the panel this
// script sits on (the old mulch draft panel in Canvas.prefab)
public class MulchDebugMenu : MonoBehaviour
{
    public static MulchDebugMenu instance;

    [SerializeField] private GameObject panel;

    private GameObject window;
    private float _timeScaleBeforeShow = 1f;
    private static MulchTier giveRarity = MulchTier.Common;

    private const float MenuWidth = 1320f;
    private static readonly WeatherType[] DebugWeathers =
        { WeatherType.Clear, WeatherType.Sunny, WeatherType.Rain, WeatherType.Snow, WeatherType.Windy, WeatherType.Sandstorm };

    void Awake()
    {
        if (instance != null) return;
        instance = this;
        if (panel == null) panel = gameObject;
        // the old draft screen's own children (card row, header) stay hidden
        foreach (Transform child in panel.transform)
            child.gameObject.SetActive(false);
        panel.SetActive(false);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    // only runs while the panel is open: this script sits on the panel itself. the n key that opens
    // it is read in GameManager for that reason
    void Update()
    {
        if (!IsOpen || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame || TutorialManager.BlocksEscape) return;
        Close();
    }

    public void Show()
    {
        if (!panel.activeSelf)
        {
            _timeScaleBeforeShow = Time.timeScale;
            Time.timeScale = 0f;
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        Rebuild();
    }

    private void Close()
    {
        Time.timeScale = _timeScaleBeforeShow;
        if (window != null) Destroy(window);
        window = null;
        panel.SetActive(false);
    }

    public void CloseForSceneExit()
    {
        if (!IsOpen) return;
        Close();
    }

    private void Rebuild()
    {
        if (window != null) Destroy(window);
        MulchRegistry registry = MulchManager.instance != null ? MulchManager.instance.Registry : null;
        TMP_FontAsset font = registry != null ? registry.font : null;

        GameObject content = NewOverlayWindow(MenuWidth);
        AddText(content.transform, font, "<b>Mulch Test Menu</b>\n<size=55%>Editor and development builds only. Esc closes.</size>",
            36f, TextAlignmentOptions.Center);

        AddText(content.transform, font, "<b>Weather</b>", 26f, TextAlignmentOptions.MidlineLeft);
        Transform weatherRow = NewRow(content.transform);
        foreach (WeatherType type in DebugWeathers)
        {
            WeatherType weather = type;
            AddButton(weatherRow, font, $"<b>{WeatherManager.GetWeatherName(weather)}</b>", TextAlignmentOptions.Center,
                new Color(0.2f, 0.22f, 0.28f), new Color(0.32f, 0.35f, 0.45f), () => SetWeather(weather), 24f);
        }

        AddText(content.transform, font, "<b>Fertilizer</b> <size=70%>(click one to make it the level's fertilizer)</size>", 26f, TextAlignmentOptions.MidlineLeft);
        Transform fertilizerGrid = NewGrid(content.transform);
        FertilizerData active = FertilizerManager.instance != null ? FertilizerManager.instance.Active : null;
        if (registry != null && registry.fertilizers != null)
            foreach (FertilizerData fertilizer in registry.fertilizers)
            {
                if (fertilizer == null) continue;
                FertilizerData data = fertilizer;
                bool isActive = data == active;
                // fertilizers have no rarity, so one soil brown for all of them
                Color tier = new Color(0.6f, 0.42f, 0.22f);
                string label = isActive ? $"{data.fertilizerName} <size=70%><color=#AAAAAA>(active)</color></size>" : data.fertilizerName;
                AddButton(fertilizerGrid, font, label, TextAlignmentOptions.Center,
                    isActive ? new Color(0.25f, 0.25f, 0.25f) : Color.Lerp(tier, Color.black, 0.7f),
                    isActive ? new Color(0.38f, 0.38f, 0.38f) : Color.Lerp(tier, Color.black, 0.5f),
                    () => SetFertilizer(data), 22f);
            }

        AddText(content.transform, font, "<b>Mulches</b> <size=70%>(click one to put it in the inventory at the chosen rarity)</size>", 26f, TextAlignmentOptions.MidlineLeft);
        Transform rarityRow = NewRow(content.transform);
        foreach (MulchTier rarity in new[] { MulchTier.Common, MulchTier.Rare, MulchTier.Epic })
        {
            MulchTier r = rarity;
            Color color = MulchTierColors.Get(r);
            bool chosen = r == giveRarity;
            AddButton(rarityRow, font, chosen ? $"<b>{r}</b> <size=70%>(chosen)</size>" : $"<b>{r}</b>", TextAlignmentOptions.Center,
                Color.Lerp(color, Color.black, chosen ? 0.4f : 0.75f), Color.Lerp(color, Color.black, 0.3f),
                () => { giveRarity = r; Rebuild(); }, 24f);
        }
        AddButton(rarityRow, font, "<b>Random drop</b>", TextAlignmentOptions.Center,
            new Color(0.25f, 0.2f, 0.1f), new Color(0.4f, 0.32f, 0.16f), RandomDrop, 24f);

        Transform mulchGrid = NewGrid(content.transform);
        if (registry != null && registry.mulches != null)
            foreach (MulchData mulch in registry.mulches)
            {
                if (mulch == null) continue;
                MulchData data = mulch;
                Color color = MulchTierColors.Get(giveRarity);
                AddButton(mulchGrid, font, data.mulchName, TextAlignmentOptions.Center,
                    Color.Lerp(color, Color.black, 0.75f), Color.Lerp(color, Color.black, 0.55f), () => Give(data), 22f);
            }

        AddButton(content.transform, font, "Close", TextAlignmentOptions.Center,
            new Color(0.22f, 0.22f, 0.22f), new Color(0.35f, 0.35f, 0.35f), Close);
    }

    private void SetWeather(WeatherType type)
    {
        if (WeatherManager.instance != null)
        {
            WeatherManager.instance.ClearAllWeather();
            if (type != WeatherType.Clear) WeatherManager.instance.SetWeather(type, 1);
            Debug.Log($"[Mulch test] weather set to {type}");
        }
        Close();
    }

    private void SetFertilizer(FertilizerData data)
    {
        FertilizerManager.instance?.Commit(data);
        Debug.Log($"[Mulch test] fertilizer set to {data.name}");
        Close();
    }

    // stays open, so several can be given in a row
    private void Give(MulchData data)
    {
        if (MulchManager.instance == null) return;
        bool inInventory = MulchManager.instance.DebugGive(data, giveRarity);
        Debug.Log($"[Mulch test] gave {data.name} ({giveRarity}){(inInventory ? "" : ", inventory full so it dropped on the ground")}");
    }

    private void RandomDrop()
    {
        if (MulchManager.instance == null) return;
        MulchItem item = MulchManager.instance.RollItem(null, false);
        Vector3 at = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        at.z = 0f;
        // the map's middle sits left of the camera's (the plant panel takes the right of the screen)
        if (Camera.main != null) at = Camera.main.ViewportToWorldPoint(new Vector3(0.38f, 0.5f, 0f));
        at.z = 0f;
        MulchDrop.Spawn(item, at, false);
        Close();
    }

    // ── building ────────────────────────────────────────────────────────────

    // a dimmed full screen overlay with a centred window that grows to fit its rows
    private GameObject NewOverlayWindow(float width)
    {
        window = NewUIObject("MulchTestMenu", panel.transform);
        RectTransform overlay = (RectTransform)window.transform;
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        window.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        GameObject box = NewUIObject("Window", window.transform);
        RectTransform rect = (RectTransform)box.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0f);
        box.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.96f);
        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        box.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return box;
    }

    private static Transform NewRow(Transform parent)
    {
        GameObject row = NewUIObject("Row", parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return row.transform;
    }

    private static Transform NewGrid(Transform parent)
    {
        GameObject grid = NewUIObject("Grid", parent);
        GridLayoutGroup layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(310f, 44f);
        layout.spacing = new Vector2(8f, 8f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.UpperCenter;
        return grid.transform;
    }

    private static GameObject NewUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void AddText(Transform parent, TMP_FontAsset font, string text, float size, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = NewUIObject("Text", parent).AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.fontSize = size;
        label.richText = true;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = text;
    }

    // a row that sizes itself to its text. the image stays white and the button's colour tint
    // carries the actual colours, so hovering a row brightens it
    private static void AddButton(Transform parent, TMP_FontAsset font, string text, TextAlignmentOptions alignment,
        Color normal, Color highlighted, UnityAction onClick, float fontSize = 30f)
    {
        GameObject row = NewUIObject("Button", parent);
        Image image = row.AddComponent<Image>();
        image.color = Color.white;

        Button button = row.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.selectedColor = normal;
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.3f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 8, 8);
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        AddText(row.transform, font, text, fontSize, alignment);
    }
}

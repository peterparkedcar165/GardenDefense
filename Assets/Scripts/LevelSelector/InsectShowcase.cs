using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// the loadout screen's insect panel (Tommy, 2026-11-24): which insects come in this level, never how
// many or when. one card per insect, in the order they first come (portrait, name, description, tags,
// NEW until this save has fought it), the level's weather, temperature and wave count above them, and
// two tooltips: a card's shows the insect's description, ability, stats and resistances; a tag's or a
// condition's says exactly what it means. built in code from Tommy's pixel kit (InsectShowcaseStyle,
// the plant info panel's pieces), placed by hand in whole art pixels: 1 art pixel = 2 canvas units.
// the text shadows (PixelTextEffect) are sibling copies, so nothing here uses layout groups
public class InsectShowcase : MonoBehaviour
{
    private const float Px = 2f;
    // art pixels: the panel's inner padding, the gap between cards and chips
    private const float Pad = 6f;
    private const float CardGap = 3f;
    private const float ChipGap = 3f;
    private const float StatIcon = 16f;
    private const float ElementIcon = 12f;

    private class Entry
    {
        public InsectData data;
        public Insect insect;
        public RectTransform card;
    }

    private class TipText
    {
        public string title, category, body;
        public RectTransform rect;
    }

    private InsectShowcaseStyle style;
    private RectTransform host;
    private RectTransform rect;
    private RectTransform header;
    private RectTransform viewport;
    private RectTransform content;
    private ScrollRect scroll;
    private RectTransform cardTip;
    private RectTransform chipTip;

    private Entry hoveredCard;
    private TipText hoveredChip;
    private Entry shownCard;
    private TipText shownChip;

    // host: the loadout screen's panel. the insect panel sits on its left; the tooltips are drawn on it too
    public static InsectShowcase Create(RectTransform host)
    {
        GameObject go = new GameObject("InsectShowcase", typeof(RectTransform));
        go.layer = host.gameObject.layer;
        go.transform.SetParent(host, false);
        InsectShowcase showcase = go.AddComponent<InsectShowcase>();
        showcase.Build(host);
        return showcase;
    }

    private void Build(RectTransform hostRect)
    {
        style = InsectShowcaseStyle.Instance;
        host = hostRect;
        rect = (RectTransform)transform;
        // hangs from the top left corner; Show makes it as tall as its insects need, up to the screen's
        // full height (then the cards scroll)
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(style.margin * Px, -style.margin * Px);
        rect.sizeDelta = new Vector2(style.panelWidth * Px, Mathf.Max(0f, host.rect.height - 2f * style.margin * Px));

        Image body = gameObject.AddComponent<Image>();
        SetSprite(body, style.panelBase, style.panel, true);
        body.raycastTarget = true;
        Stretch(NewImage("Shine", rect, style.panelShine, Color.white, true).rectTransform);

        header = NewRect("Header", rect);
        TopLeft(header, 0f, 0f, style.panelWidth, 0f);

        viewport = NewRect("Cards", rect);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0.5f, 1f);
        Image catcher = viewport.gameObject.AddComponent<Image>();
        catcher.color = Color.clear;
        catcher.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();

        content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 30f;

        cardTip = NewTip("InsectTooltip");
        chipTip = NewTip("TagTooltip");
    }

    private RectTransform NewTip(string name)
    {
        RectTransform tip = NewRect(name, host);
        tip.anchorMin = tip.anchorMax = new Vector2(0f, 1f);
        tip.pivot = new Vector2(0f, 1f);
        Image image = tip.gameObject.AddComponent<Image>();
        SetSprite(image, style.panelBase, style.panel, true);
        image.raycastTarget = false;
        tip.gameObject.SetActive(false);
        return tip;
    }

    // ---------- the panel ----------

    // fills the panel for a level: its conditions and one card per insect its waves (and elites) spawn
    public void Show(LevelConfig config, GameObject[] insectPrefabs)
    {
        HideTips();
        hoveredCard = null;
        hoveredChip = null;
        Clear(header);
        Clear(content);

        float y = BuildHeader(config);
        viewport.offsetMin = new Vector2(Pad * Px, Pad * Px);
        viewport.offsetMax = new Vector2(-Pad * Px, -y * Px);

        float width = style.panelWidth - 2f * Pad;
        float cardY = 0f;
        var seen = new HashSet<InsectData>();
        if (insectPrefabs != null)
            foreach (GameObject prefab in insectPrefabs)
            {
                Insect insect = prefab != null ? prefab.GetComponent<Insect>() : null;
                if (insect == null || insect.data == null || !seen.Add(insect.data)) continue;
                cardY += BuildCard(new Entry { data = insect.data, insect = insect }, cardY, width) + CardGap;
            }

        float cardsHeight = Mathf.Max(0f, cardY - CardGap);
        content.sizeDelta = new Vector2(0f, cardsHeight * Px);
        content.anchoredPosition = Vector2.zero;

        // the panel fits its insects (Tommy, 2026-11-24): the header, every card and the bottom padding,
        // until that's taller than the screen; then it stops at the screen's full height and the cards
        // scroll
        float room = host.rect.height - 2f * style.margin * Px;
        float wanted = (y + cardsHeight + Pad) * Px;
        rect.sizeDelta = new Vector2(style.panelWidth * Px, room > 0f ? Mathf.Min(wanted, room) : wanted);
    }

    // "Insects", the seam under it and the level's conditions; returns the art pixel height it takes
    private float BuildHeader(LevelConfig config)
    {
        float titleHeight = AddText(header, "Insects", 9f, 5f, style.panelWidth - 18f, style.text, style.titleSize, false);
        float y = 5f + titleHeight + 2f;
        Image seam = NewImage("Seam", header, style.seam, Color.white, false);
        TopLeft(seam.rectTransform, 4f, y, style.panelWidth - 8f, 2f);
        y += 2f + 4f;
        if (config == null) return y + 2f;

        float x = Pad;
        float rowHeight = ChipHeight();
        float maxX = style.panelWidth - Pad;
        void Place(string label, TipText tip)
        {
            float w = ChipWidth(label);
            if (x > Pad && x + w > maxX) { x = Pad; y += rowHeight + ChipGap; }
            AddChip(header, label, Color.white, x, y, tip);
            x += w + ChipGap;
        }

        // every weather the level starts with; a level with none (or only Clear at 0) is Clear
        var listed = new List<WeatherType>();
        if (config.weather != null)
            foreach (WeatherEntry entry in config.weather)
                if (!listed.Contains(entry.type) && (entry.type != WeatherType.Clear || entry.intensity > 0)) listed.Add(entry.type);
        if (listed.Count == 0) listed.Add(WeatherType.Clear);
        foreach (WeatherType type in listed)
        {
            int intensity = 1;
            foreach (WeatherEntry entry in config.weather ?? System.Array.Empty<WeatherEntry>())
                if (entry.type == type) intensity = Mathf.Max(1, entry.intensity);
            string name = WeatherManager.GetWeatherName(type);
            Place(name, new TipText { title = name, category = "Weather", body = WeatherManager.GetWeatherDescription(type, intensity) });
        }

        string temperature = WeatherManager.GetTemperatureName(config.temperature);
        Place(temperature, new TipText { title = temperature, category = "Temperature", body = WeatherManager.GetTemperatureDescription(config.temperature) });

        string waves = $"{config.maxWaves} waves";
        Place(waves, new TipText { title = waves, category = "Level length", body = $"This level has {config.maxWaves} waves. You can call the next wave early." });

        return y + rowHeight + 5f;
    }

    // one insect's card; returns its height in art pixels
    private float BuildCard(Entry entry, float y, float width)
    {
        InsectData data = entry.data;
        Image cardImage = NewImage("Card " + data.displayName, content, style.insetBox, style.panel, true);
        cardImage.raycastTarget = true;
        RectTransform card = cardImage.rectTransform;
        entry.card = card;

        float p = style.portraitSize;
        Color nameColor = data.nameColor.a > 0f ? data.nameColor : style.text;
        nameColor.a = 1f;
        Color backdrop = Color.Lerp(nameColor, Color.white, style.portraitLighten);
        Image portrait = NewImage("Portrait", card, style.panelBase, backdrop, true);
        TopLeft(portrait.rectTransform, 4f, 4f, p, p);
        Stretch(NewImage("Shine", portrait.rectTransform, style.panelShine, Color.white, true).rectTransform);
        Image sprite = NewImage("Insect", portrait.rectTransform, data.sprite, Color.white, false);
        sprite.preserveAspect = true;
        TopLeft(sprite.rectTransform, 3f, 3f, p - 6f, p - 6f);

        if (SaveManager.instance != null && !SaveManager.instance.HasSeenInsect(data))
        {
            RectTransform badge = AddBadge(card, "NEW", style.textSize);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 1f);
            badge.pivot = new Vector2(0f, 1f);
            badge.anchoredPosition = new Vector2(1f * Px, -1f * Px);
        }

        float x = 4f + p + 5f;
        float columnWidth = width - x - 4f;
        float ty = 4f;
        ty += AddText(card, data.displayName, x, ty, columnWidth, nameColor, style.textSize, false) + 1f;
        string brief = entry.insect.GetShortDescription();
        if (brief.Length > 0) ty += AddText(card, brief, x, ty, columnWidth, style.text, style.textSize, true) + 2f;

        float chipX = x;
        float chipRow = ChipHeight();
        foreach (TipText tag in Tags(entry))
        {
            float w = ChipWidth(tag.title);
            if (chipX > x && chipX + w > x + columnWidth) { chipX = x; ty += chipRow + ChipGap; }
            AddChip(card, tag.title, TagColor(tag.category, tag.title), chipX, ty, tag);
            chipX += w + ChipGap;
        }
        ty += chipRow;

        float height = Mathf.Max(4f + p + 4f, ty + 4f);
        TopLeft(card, 0f, y, width, height);

        UIHoverRelay relay = card.gameObject.AddComponent<UIHoverRelay>();
        relay.onEnter = () => { hoveredCard = entry; Refresh(); };
        relay.onExit = () => { if (hoveredCard == entry) hoveredCard = null; Refresh(); };
        return height;
    }

    // ---------- tags: what each one means, as the game works it out ----------

    private IEnumerable<TipText> Tags(Entry entry)
    {
        InsectData data = entry.data;
        Insect insect = entry.insect;
        float range = data.baseTargetingRange;
        switch (data.aggressivity)
        {
            case Aggressivity.High:
                yield return new TipText { title = Insect.AggressivityName(Aggressivity.High), category = "Aggressivity",
                    body = $"Goes after plants within its targeting range ({range:0.#}) and keeps attacking them instead of walking on." };
                break;
            case Aggressivity.Low:
                yield return new TipText { title = Insect.AggressivityName(Aggressivity.Low), category = "Aggressivity",
                    body = $"Attacks plants within its targeting range ({range:0.#}). After each bite on a plant it ignores plants for " +
                           $"{Insect.PlantBiteCooldown:0} seconds and walks on." };
                break;
            default:
                string note = insect.AggressivityNote();
                yield return new TipText { title = Insect.AggressivityName(Aggressivity.None), category = "Aggressivity",
                    body = "Never goes after plants on its own. It walks the path to your garden and only fights a plant that taunts it." +
                           (string.IsNullOrEmpty(note) ? "" : " " + note) };
                break;
        }
        if (insect is FlyingInsect)
            yield return new TipText { title = "Flying", category = "Movement",
                body = "Melee plants like Acorn Knight can't target it, unless it is attacking that plant. Immobilization effects " +
                       "ground it for the effect's duration + 4 seconds (5 seconds if Knocked Up)." };
        if (data.baseMovementSpeed >= style.fastSpeed)
            yield return new TipText { title = "Fast", category = "Movement",
                body = $"Moves at {data.baseMovementSpeed:0.##}. Most insects move at 0.6 to 0.9." };
        else if (data.baseMovementSpeed <= style.slowSpeed)
            yield return new TipText { title = "Slow", category = "Movement",
                body = $"Moves at {data.baseMovementSpeed:0.##}. Most insects move at 0.6 to 0.9." };
        if (data.baseArmor >= style.highArmorAt)
        {
            int cap = Insect.MaxWaveArmorBonus();
            string growth = cap == int.MaxValue ? "Armor also grows by 1 each wave." : $"Armor also grows by 1 each wave, up to +{cap}.";
            yield return new TipText { title = "High armor", category = "Defense",
                body = $"{data.baseArmor:0} Armor: takes {Mathf.RoundToInt(Entity.ArmorReduction(data.baseArmor) * 100f)}% less Physical damage " +
                       $"(Armor / (150 + Armor)). Magic damage is checked against its Magic Armor instead ({data.baseMagicArmor:0}). {growth}" };
        }
    }

    private Color TagColor(string category, string title)
    {
        if (category == "Aggressivity")
            return title == Insect.AggressivityName(Aggressivity.High) ? style.aggressive
                 : title == Insect.AggressivityName(Aggressivity.Low) ? style.mildlyAggressive : style.passive;
        if (title == "Flying") return style.flying;
        if (title == "Fast") return style.fast;
        if (title == "Slow") return style.slow;
        if (title == "High armor") return style.highArmor;
        return style.text;
    }

    // ---------- hover ----------

    private void Refresh()
    {
        if (hoveredChip != null)
        {
            HideCardTip();
            ShowChipTip(hoveredChip);
        }
        else
        {
            HideChipTip();
            if (hoveredCard != null) ShowCardTip(hoveredCard);
            else HideCardTip();
        }
    }

    public void HideTips()
    {
        HideCardTip();
        HideChipTip();
    }

    private void HideCardTip()
    {
        shownCard = null;
        if (cardTip != null) cardTip.gameObject.SetActive(false);
    }

    private void HideChipTip()
    {
        shownChip = null;
        if (chipTip != null) chipTip.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        hoveredCard = null;
        hoveredChip = null;
        HideTips();
    }

    private void OnDestroy()
    {
        if (cardTip != null) Destroy(cardTip.gameObject);
        if (chipTip != null) Destroy(chipTip.gameObject);
    }

    // the insect's description, ability, stats, resistances, shared traits and aggressivity, to the
    // right of the panel and level with its card
    private void ShowCardTip(Entry entry)
    {
        if (shownCard == entry && cardTip.gameObject.activeSelf) return;
        shownCard = entry;
        // shown before it's filled: a text only measures its wrapped height on an active object, and
        // a hidden tooltip measured every text as one line, so the boxes under it overlapped (Tommy)
        cardTip.gameObject.SetActive(true);
        Clear(cardTip);
        Stretch(NewImage("Shine", cardTip, style.panelShine, Color.white, true).rectTransform);

        InsectData data = entry.data;
        Insect insect = entry.insect;
        float w = style.tooltipWidth;
        float inner = w - 2f * Pad;
        float lh = LineHeight(style.textSize);
        float y = 5f;

        Color nameColor = data.nameColor.a > 0f ? data.nameColor : style.text;
        nameColor.a = 1f;
        float nameHeight = AddText(cardTip, data.displayName, 8f, y, w - 16f, nameColor, style.textSize, false);
        AddText(cardTip, data.threatType.ToString(), 8f, y, w - 16f, style.mutedText, style.textSize, false, TextAlignmentOptions.TopRight);
        y += nameHeight + 2f;
        Image seam = NewImage("Seam", cardTip, style.seam, Color.white, false);
        TopLeft(seam.rectTransform, 4f, y, w - 8f, 2f);
        y += 2f + 4f;

        string brief = insect.GetShortDescription();
        if (brief.Length > 0) y += AddText(cardTip, brief, 8f, y, w - 16f, style.text, style.textSize, true) + 4f;

        // the ability, in its own box
        Image box = NewImage("Ability", cardTip, style.insetBox, style.panel, true);
        string ability = insect.GetAbilityDescription();
        float by = 3f;
        by += AddText(box.rectTransform, "Ability", 5f, by, inner - 10f, style.label, style.textSize, false) + 1f;
        by += string.IsNullOrEmpty(ability)
            ? AddText(box.rectTransform, "None", 5f, by, inner - 10f, style.mutedText, style.textSize, false)
            : AddText(box.rectTransform, ability, 5f, by, inner - 10f, style.text, style.textSize, true);
        by += 4f;
        TopLeft(box.rectTransform, Pad, y, inner, by);
        y += by + 4f;

        // stats, three columns read top then bottom: health over speed, armor over magic armor,
        // attack damage over attack speed (Tommy)
        float cellW = Mathf.Floor((inner - 2f * 3f) / 3f);
        float cellH = StatIcon + 4f;
        StatCell(0, 0, style.health, $"{data.baseMaxHealth:0}");
        StatCell(0, 1, style.movementSpeed, $"{data.baseMovementSpeed:0.##}");
        StatCell(1, 0, style.armor, $"{data.baseArmor:0}");
        StatCell(1, 1, style.magicArmor, $"{data.baseMagicArmor:0}");
        StatCell(2, 0, style.attackDamage, $"{data.baseAttackDamage:0}");
        StatCell(2, 1, style.attackSpeed, $"{data.baseAttackSpeed:0.##}");
        void StatCell(int col, int row, Sprite icon, string value)
        {
            Image cell = NewImage("Stat", cardTip, style.stripSunken, style.panel, true);
            TopLeft(cell.rectTransform, Pad + col * (cellW + 3f), y + row * (cellH + 3f), cellW, cellH);
            Image image = NewImage("Icon", cell.rectTransform, icon, Color.white, false);
            TopLeft(image.rectTransform, 2f, 2f, StatIcon, StatIcon);
            AddText(cell.rectTransform, value, StatIcon + 5f, Mathf.Floor((cellH - lh) / 2f), cellW - StatIcon - 7f, style.text, style.textSize, false);
        }
        y += 2f * cellH + 3f + 4f;

        // resistances (Tommy): one box headed "Resistances", then each element's icon, a colon and its
        // percentage, three to a row in his order: fire, water, grass, then poison, ice, wind
        Image resBox = NewImage("Resistances", cardTip, style.insetBox, style.panel, true);
        float ry = 3f;
        ry += AddText(resBox.rectTransform, "Resistances", 5f, ry, inner - 10f, style.label, style.textSize, false) + 2f;
        float colW = Mathf.Floor((inner - 10f) / 3f);
        float rowH = Mathf.Max(ElementIcon, lh);
        string colon = ColorUtility.ToHtmlStringRGB(style.text);
        ResEntry(0, ElementalType.Fire, data.baseFireResistance);
        ResEntry(1, ElementalType.Water, data.baseWaterResistance);
        ResEntry(2, ElementalType.Grass, data.baseGrassResistance);
        ResEntry(3, ElementalType.Poison, data.basePoisonResistance);
        ResEntry(4, ElementalType.Ice, data.baseIceResistance);
        ResEntry(5, ElementalType.Wind, data.baseWindResistance);
        void ResEntry(int i, ElementalType element, float value)
        {
            float cx = 5f + (i % 3) * colW;
            float cy = ry + (i / 3) * (rowH + 2f);
            float tx = cx;
            Sprite icon = style.ElementIcon(element);
            if (icon != null)
            {
                Image image = NewImage("Icon " + element, resBox.rectTransform, icon, Color.white, false);
                image.preserveAspect = true;
                TopLeft(image.rectTransform, cx, cy + Mathf.Floor((rowH - ElementIcon) / 2f), ElementIcon, ElementIcon);
                tx = cx + ElementIcon + 2f;
            }
            int percent = Mathf.RoundToInt(value * 100f);
            string shown = (percent > 0 ? "+" : percent < 0 ? "-" : "") + Mathf.Abs(percent) + "%";
            Color color = percent < 0 ? style.weak : percent > 0 ? style.resists : style.mutedText;
            string label = icon != null ? $"<color=#{colon}>:</color> {shown}" : $"<color=#{colon}>{element}:</color> {shown}";
            AddText(resBox.rectTransform, label, tx, cy + Mathf.Floor((rowH - lh) / 2f), cx + colW - tx - 2f, color, style.textSize, false);
        }
        ry += 2f * rowH + 2f + 4f;
        TopLeft(resBox.rectTransform, Pad, y, inner, ry);
        y += ry + 4f;

        y += AddText(cardTip, insect.GetTraitsAndAggressivityText(), 8f, y, w - 16f, style.text, style.textSize, true) + 6f;

        cardTip.sizeDelta = new Vector2(w * Px, y * Px);
        cardTip.SetAsLastSibling();
        cardTip.gameObject.SetActive(true);

        // level with the card's top, kept on screen
        Vector3[] corners = new Vector3[4];
        entry.card.GetWorldCorners(corners);
        Vector2 top = host.InverseTransformPoint(corners[1]);
        float yTop = top.y - host.rect.yMax;
        float lowest = -host.rect.height + style.margin * Px + y * Px;
        yTop = Mathf.Clamp(yTop, Mathf.Min(lowest, -style.margin * Px), -style.margin * Px);
        cardTip.anchoredPosition = new Vector2((style.margin + style.panelWidth + 6f) * Px, Mathf.Round(yTop / Px) * Px);
    }

    // a tag's or a condition's meaning, just above it (below it when there's no room above)
    private void ShowChipTip(TipText chip)
    {
        if (shownChip == chip && chipTip.gameObject.activeSelf) return;
        shownChip = chip;
        // shown before it's filled, so its wrapped text measures right (see ShowCardTip)
        chipTip.gameObject.SetActive(true);
        Clear(chipTip);
        Stretch(NewImage("Shine", chipTip, style.panelShine, Color.white, true).rectTransform);

        float w = style.chipTooltipWidth;
        float y = 5f;
        float titleHeight = AddText(chipTip, chip.title, 7f, y, w - 14f, style.text, style.textSize, false);
        AddText(chipTip, chip.category, 7f, y, w - 14f, style.mutedText, style.textSize, false, TextAlignmentOptions.TopRight);
        y += titleHeight + 3f;
        y += AddText(chipTip, chip.body, 7f, y, w - 14f, style.text, style.textSize, true) + 5f;

        chipTip.sizeDelta = new Vector2(w * Px, y * Px);
        chipTip.SetAsLastSibling();
        chipTip.gameObject.SetActive(true);

        Vector3[] corners = new Vector3[4];
        chip.rect.GetWorldCorners(corners);
        Vector2 bottomLeft = host.InverseTransformPoint(corners[0]);
        Vector2 topLeft = host.InverseTransformPoint(corners[1]);
        float x = Mathf.Clamp(topLeft.x - host.rect.xMin - 2f * Px, style.margin * Px, host.rect.width - (w + style.margin) * Px);
        float above = topLeft.y - host.rect.yMax + 4f * Px + y * Px;
        float yTop = above <= -style.margin * Px ? above : bottomLeft.y - host.rect.yMax - 4f * Px;
        chipTip.anchoredPosition = new Vector2(Mathf.Round(x / Px) * Px, Mathf.Round(yTop / Px) * Px);
    }

    // ---------- chips, badges and text ----------

    private float ChipHeight() => LineHeight(style.textSize) + 2f;

    private float ChipWidth(string label) => Mathf.Ceil(MeasureWidth(label, style.textSize) / Px) + 8f;

    private void AddChip(RectTransform parent, string label, Color color, float x, float y, TipText tip)
    {
        Image chip = NewImage("Chip " + tip.title, parent, style.stripSunken, style.panel, true);
        chip.raycastTarget = true;
        float w = ChipWidth(label);
        TopLeft(chip.rectTransform, x, y, w, ChipHeight());
        AddText(chip.rectTransform, label, 4f, 1f, w - 6f, color, style.textSize, false);
        tip.rect = chip.rectTransform;
        UIHoverRelay relay = chip.gameObject.AddComponent<UIHoverRelay>();
        relay.onEnter = () => { hoveredChip = tip; Refresh(); };
        relay.onExit = () => { if (hoveredChip == tip) hoveredChip = null; Refresh(); };
    }

    // a small gold badge (NEW on an insect card, Recommended on a plant square), sized to its word.
    // the caller places it; it never takes the pointer
    public static RectTransform AddBadge(RectTransform parent, string label, float textSize)
    {
        InsectShowcaseStyle s = InsectShowcaseStyle.Instance;
        GameObject go = new GameObject("Badge " + label, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform badge = (RectTransform)go.transform;
        badge.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        SetSprite(image, s.panelBase, s.badge, true);
        image.raycastTarget = false;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.layer = go.layer;
        textGO.transform.SetParent(badge, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        if (s.font != null) text.font = s.font;
        text.fontSize = textSize;
        text.color = s.badgeText;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
        text.text = label;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        float padX = Mathf.Round(textSize * 0.2f) * Px;
        float width = Mathf.Ceil((text.GetPreferredValues(label).x + 2f * padX) / Px) * Px;
        float height = Mathf.Ceil((LineHeightUnits(s, textSize) + 2f * Px) / Px) * Px;
        badge.sizeDelta = new Vector2(width, Mathf.Max(height, 8f * Px));
        return badge;
    }

    // a text at (x, y) art pixels from the parent's top left, width art pixels wide (wrapped or one
    // line), with the panel's 1 pixel shadow; returns its height in art pixels
    private float AddText(RectTransform parent, string value, float x, float y, float width, Color color, float size, bool wrap,
        TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (style.font != null) text.font = style.font;
        text.fontSize = size;
        text.color = color;
        text.richText = true;
        text.raycastTarget = false;
        text.alignment = align;
        text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        text.text = value;
        float height = Mathf.Max(LineHeight(size), Mathf.Ceil(text.GetPreferredValues(value, width * Px, 0f).y / Px));
        TopLeft(text.rectTransform, x, y, width, height);
        PixelTextEffect shadow = go.AddComponent<PixelTextEffect>();
        shadow.Color = style.textShadow;
        return height;
    }

    // one empty, invisible text measures widths the way the real ones draw, rich text tags included
    private TextMeshProUGUI measurer;

    private float MeasureWidth(string value, float size)
    {
        if (measurer == null)
        {
            RectTransform r = NewRect("Measure", rect);
            measurer = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (style.font != null) measurer.font = style.font;
            measurer.richText = true;
            measurer.textWrappingMode = TextWrappingModes.NoWrap;
            measurer.raycastTarget = false;
            measurer.color = Color.clear;
            measurer.text = "";
        }
        measurer.fontSize = size;
        return measurer.GetPreferredValues(value).x;
    }

    // one line of text at this size, in whole art pixels (from the font's own line height)
    private float LineHeight(float size) => Mathf.Ceil(LineHeightUnits(style, size) / Px);

    private static float LineHeightUnits(InsectShowcaseStyle s, float size)
    {
        if (s.font == null || s.font.faceInfo.pointSize <= 0f) return size * 1.2f;
        return s.font.faceInfo.lineHeight * size / s.font.faceInfo.pointSize;
    }

    // ---------- plumbing ----------

    private static RectTransform NewRect(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color, bool sliced)
    {
        RectTransform r = NewRect(name, parent);
        Image image = r.gameObject.AddComponent<Image>();
        SetSprite(image, sprite, color, sliced);
        image.raycastTarget = false;
        return image;
    }

    private static void SetSprite(Image image, Sprite sprite, Color color, bool sliced)
    {
        image.sprite = sprite;
        image.color = color;
        image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
    }

    // x, y, w, h in art pixels from the parent's top left
    private static void TopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(x * Px, -y * Px);
        r.sizeDelta = new Vector2(w * Px, h * Px);
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Clear(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }
}

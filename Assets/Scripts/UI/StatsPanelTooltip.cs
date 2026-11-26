using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;

public class StatsPanelTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text statsText;

    // fewest dots between a label and its value, on the widest line
    private const int MinDots = 2;
    // the VerticalLayoutGroup's padding on each side (20 left, 20 right, 20 top, 20 bottom)
    private const float Padding = 40f;

    private const string Header   = "<size=+8><color=white><b><u>Core</u></b></color></size>";
    private const string Fire     = "<color=orange>";
    private const string Water    = "<color=#4FC3F7>";
    private const string Grass   = "<color=green>";
    private const string Ice      = "<color=#00FFFF>";
    private const string Poison   = "<color=purple>";
    private const string Wind     = "<color=#B2EBF2>";
    private const string Magic    = "<color=#FFB6C1>";
    private const string Physical = "<color=#A0522D>";
    private const string Crit     = "<color=#FFD700>";
    private const string HealCol  = "<color=#FF6B81>";
    private const string DropCol  = "<color=#C9A227>";
    private const string End      = "</color>";

    // one line of the panel: the label as shown (with its colour) and the value as shown. a null
    // label is an empty line between groups
    private struct Row
    {
        public string label, value;
    }
    private readonly List<Row> _rows = new List<Row>();
    private readonly StringBuilder _key = new StringBuilder();
    private string lastKey;

    // while collecting, BuildCore only records each line's current and base value here (in line
    // order) and makes no text. the lines were last built from _builtValues for _builtEntity
    private bool _collecting;
    private readonly List<float> _values = new List<float>(96);
    private readonly List<float> _builtValues = new List<float>(96);
    private Entity _builtEntity;
    private bool _built;

    private bool SameValues()
    {
        if (_values.Count != _builtValues.Count) return false;
        for (int i = 0; i < _values.Count; i++)
            if (!_values[i].Equals(_builtValues[i])) return false;
        return true;
    }

    // the panel keeps its right edge (next to the plant panel) while its width follows the text,
    // and its middle where the prefab put it unless that would put part of it off screen
    private RectTransform _panelRect;
    private float _rightEdge, _baseY;
    private bool _edgeKnown;
    private readonly Vector3[] _corners = new Vector3[4];
    // share of the screen's height the panel may use, and the edge kept free above and below it
    private const float MaxScreenShare = 0.975f;

    private void Awake()
    {
        // every line is laid out by hand (Compose), so the text never wraps. with wrapping on, its
        // height was measured at the width it had before the panel resized, lines wider than that
        // counted as wrapped, and the panel came out taller than the stats (Tommy, 2026-11-22)
        if (statsText != null) statsText.textWrappingMode = TextWrappingModes.NoWrap;
        // Tommy's flower frame (2026-11-26)
        TooltipFrameLook.Apply(GetComponent<UnityEngine.UI.Image>());
    }

    private string Col(float current, float baseValue, string formatted)
    {
        if (current > baseValue) return $"<color=green>{formatted}</color>";
        if (current < baseValue) return $"<color=red>{formatted}</color>";
        return formatted;
    }

    private void Update()
    {
        if (statsText == null) return;

        PlantUpgradeUI ui = PlantUpgradeUI.instance;
        Plant plant = ui != null ? ui.GetSelectedPlant() : null;
        Entity entity = plant != null ? (Entity)plant : (ui != null ? ui.GetSelectedInsect() : null);

        // first only the raw numbers behind every line are read (no text made). the lines are
        // built again only when one of them, or the selection, changed since the last build
        _values.Clear();
        _collecting = true;
        if (entity != null) BuildCore(entity);
        _collecting = false;
        if (_built && ReferenceEquals(entity, _builtEntity) && SameValues()) return;
        _built = true;
        _builtEntity = entity;
        _builtValues.Clear();
        _builtValues.AddRange(_values);

        _rows.Clear();
        if (entity != null) BuildCore(entity);

        // only measure and resize when something shown actually changed
        _key.Clear();
        foreach (Row r in _rows) _key.Append(r.label).Append('\u0001').Append(r.value).Append('\u0002');
        string key = _key.ToString();
        if (key == lastKey) return;
        lastKey = key;

        statsText.text = entity != null ? Compose() : "";
        FitPanel();
    }

    // every line comes out the same width: each label and value is measured in the real font, the
    // gap is filled with as many dots as fit, and the value is placed with <pos> so all the values
    // end on the same right edge. the widest line gets MinDots dots and sets the width for the rest
    private string Compose()
    {
        // measured by difference, never one piece on its own: a lone string's preferred width
        // carries a little extra at its end, which made one dot read wider than a dot in a row and
        // left lines short of dots (worse the more dots they had). a run of 60 dots minus a run of
        // 10 is exactly 50 dot steps, and label plus 10 dots minus 10 dots is exactly where the
        // label ends and its dots start
        string tenDots = new string('.', 10);
        float tenDotsW = Measure(tenDots);
        float dot = Mathf.Max(0.01f, (Measure(new string('.', 60)) - tenDotsW) / 50f);
        int n = _rows.Count;
        float[] labelW = new float[n], valueW = new float[n];
        float content = 0f;
        for (int i = 0; i < n; i++)
        {
            if (_rows[i].label == null) continue;
            labelW[i] = Measure(_rows[i].label + tenDots) - tenDotsW;
            valueW[i] = Measure(_rows[i].value);
            content = Mathf.Max(content, labelW[i] + MinDots * dot + valueW[i]);
        }

        var sb = new StringBuilder();
        sb.Append(Header).Append('\n');
        for (int i = 0; i < n; i++)
        {
            Row r = _rows[i];
            if (r.label == null) { sb.Append('\n'); continue; }
            int dots = Mathf.Max(MinDots, Mathf.FloorToInt((content - labelW[i] - valueW[i]) / dot));
            float valueAt = content - valueW[i];
            sb.Append(r.label).Append('.', dots)
              .Append("<pos=").Append(valueAt.ToString("F2", CultureInfo.InvariantCulture)).Append('>')
              .Append(r.value).Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    private float Measure(string richText) => statsText.GetPreferredValues(richText).x;

    // same pattern as StatusEffectPanel: the panel hugs the text (plus the layout padding) but never
    // exceeds the screen. width now hugs too, keeping the right edge where the prefab put it. the
    // limit is in canvas units (the canvas is always 1080 tall), not screen pixels, and a panel that
    // would reach past the top or bottom of the screen moves in until it fits (Tommy, 2026-11-22:
    // with every stat listed its top went off screen)
    private void FitPanel()
    {
        if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
        if (!_edgeKnown)
        {
            _rightEdge = _panelRect.anchoredPosition.x + _panelRect.rect.width * (1f - _panelRect.pivot.x);
            _baseY = _panelRect.anchoredPosition.y;
            _edgeKnown = true;
        }

        RectTransform screen = statsText.canvas != null ? statsText.canvas.rootCanvas.transform as RectTransform : null;
        float screenHeight = screen != null ? screen.rect.height : Screen.height;

        Canvas.ForceUpdateCanvases();
        float width = Mathf.Ceil(statsText.preferredWidth) + Padding;
        float height = Mathf.Min(Mathf.Ceil(statsText.preferredHeight) + Padding, screenHeight * MaxScreenShare);
        _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        _panelRect.anchoredPosition = new Vector2(_rightEdge - width * (1f - _panelRect.pivot.x), _baseY);
        if (screen != null) KeepOnScreen(screen);
    }

    // moves the panel straight down (or up) by however far it reaches past the screen's top (or
    // bottom), leaving the same small edge MaxScreenShare leaves
    private void KeepOnScreen(RectTransform screen)
    {
        _panelRect.GetWorldCorners(_corners);
        float bottom = screen.InverseTransformPoint(_corners[0]).y;
        float top = screen.InverseTransformPoint(_corners[1]).y;
        float edge = screen.rect.height * (1f - MaxScreenShare) * 0.5f;
        float shift = 0f;
        if (top > screen.rect.yMax - edge) shift = screen.rect.yMax - edge - top;
        else if (bottom < screen.rect.yMin + edge) shift = screen.rect.yMin + edge - bottom;
        if (Mathf.Approximately(shift, 0f)) return;
        // from the screen's units to the units the panel's position is in
        float scale = _panelRect.parent != null
            ? screen.lossyScale.y / Mathf.Max(0.0001f, _panelRect.parent.lossyScale.y)
            : 1f;
        _panelRect.anchoredPosition += new Vector2(0f, shift * scale);
    }

    private void BuildCore(Entity e)
    {
        // the icon row above (QuickStatRow) shows a few of these too; this list has them all (Tommy,
        // 2026-11-22), with the same number formats as the icon row
        // each value is written in its format ("F0" and so on); Pct lines show it times 100 with a
        // percent sign; the int lines write the whole number as it is
        ElemLine(HealCol, "Max Health:", e.maxHealth, e.baseMaxHealth, "F0");
        Line("Attack Damage:", e.attackDamage, e.baseAttackDamage, "F0");
        Line("Attack Speed:", e.attackSpeed, e.baseAttackSpeed, "F2");
        Line("Attack Range:", e.attackRange, e.baseAttackRange, "F1");
        if (e is Insect mover)
            Line("Movement Speed:", mover.movementSpeed, mover.baseMovementSpeed, "F2");
        Blank();
        ElemLine(Crit, "Crit Chance:", e.criticalChance, e.baseCriticalChance, "F1", Pct);
        ElemLine(Crit, "Crit Damage:", e.criticalDamage, e.baseCriticalDamage, "F0", Pct);
        Blank();
        ElemLine(Magic, "Maximum Damage:", e.maximumDamage, e.baseMaximumDamage, "F0", Pct);
        ElemLine(Magic, "Minimum Damage:", e.minimumDamage, e.baseMinimumDamage, "F0", Pct);
        Blank();
        ElemLine(Magic, "Magic Power:", e.magicPower, e.baseMagicPower, "F0");
        Blank();
        ElemIntLine(Physical, "Armor:", e.armor, e.baseArmor);
        IntLine("<color=#FF69B4>Magic Armor:</color>", e.magicArmor, e.baseMagicArmor);
        Blank();
        ElemLine(Physical, "Armor Penetration:", e.armorPenFlat,    e.baseArmorPenFlat,    "F0");
        ElemLine(Physical, "Armor Shred:",       e.armorPenPercent, e.baseArmorPenPercent, "F0", Pct);
        ElemLine(Magic,    "Magic Penetration:", e.magicPenFlat,    e.baseMagicPenFlat,    "F0");
        ElemLine(Magic,    "Magic Armor Shred:", e.magicPenPercent, e.baseMagicPenPercent, "F0", Pct);
        if (e is Shooter shooter)
            ElemIntLine(Grass, "Piercing:", shooter.piercing, shooter.basePiercing);
        Blank();
        Line("Evasion:", e.evasion, e.baseEvasion, "F1", Pct);
        ElemLine(Grass, "Accuracy:", e.accuracy, e.baseAccuracy, "F1", Pct);
        Line("Visibility:", e.visibility, e.baseVisibility, "F0", Pct);
        Line("<color=grey>Tenacity:</color>", e.tenacity, e.baseTenacity, "F0", Pct);
        ElemLine(HealCol, "Heals & Shield Bonus:", e.healingBonus, e.baseHealingBonus, "F0", Pct);
        // mulch drop stats, in percents: Mulch Drop Rate adds to the base drop chance, Rare Item Drop
        // Rate weighs Rare and Epic more
        if (e is Plant dropper)
        {
            DropLine(MulchDropLabel, dropper.mulchDropRate);
            DropLine(RareDropLabel, dropper.rareItemDropRate);
        }
        Blank();
        if (e is Insect)
        {
            ElemLine(Fire,   "Fire Resistance:",   e.fireResistance,   e.baseFireResistance,   "F0", Pct);
            ElemLine(Water,  "Water Resistance:",  e.waterResistance,  e.baseWaterResistance,  "F0", Pct);
            ElemLine(Grass,  "Grass Resistance:",  e.grassResistance,  e.baseGrassResistance,  "F0", Pct);
            ElemLine(Ice,    "Ice Resistance:",    e.iceResistance,    e.baseIceResistance,    "F0", Pct);
            ElemLine(Poison, "Poison Resistance:", e.poisonResistance, e.basePoisonResistance, "F0", Pct);
            ElemLine(Wind,   "Wind Resistance:",   e.windResistance,   e.baseWindResistance,   "F0", Pct);
        }
        else
        {
            ElemLine(Grass,  "Elemental Affinity:", e.elementalAffinity, e.baseelementalAffinity, "F0", Pct);
            ElemLine(Fire,   "Fire Damage:",   e.fireDamage,   e.baseFireDamage,   "F0", Pct);
            ElemLine(Water,  "Water Damage:",  e.waterDamage,  e.baseWaterDamage,  "F0", Pct);
            ElemLine(Grass,  "Grass Damage:",  e.grassDamage,  e.baseGrassDamage,  "F0", Pct);
            ElemLine(Ice,    "Ice Damage:",    e.iceDamage,    e.baseIceDamage,    "F0", Pct);
            ElemLine(Poison, "Poison Damage:", e.poisonDamage, e.basePoisonDamage, "F0", Pct);
            ElemLine(Wind,   "Wind Damage:",   e.windDamage,   e.baseWindDamage,   "F0", Pct);
        }
        // the old text trimmed trailing empty lines, so a closing blank never showed
        while (!_collecting && _rows.Count > 0 && _rows[_rows.Count - 1].label == null) _rows.RemoveAt(_rows.Count - 1);
    }

    private const bool Pct = true;
    private const string MulchDropLabel = DropCol + "Mulch Drop Rate:" + End;
    private const string RareDropLabel = DropCol + "Rare Item Drop Rate:" + End;

    private void Blank()
    {
        if (_collecting) return;
        _rows.Add(new Row());
    }

    private static string DropRateText(float percent) =>
        percent > 0f ? $"+{percent:0.#}%" : "0%";

    // true while only collecting: the two values are recorded and no text is made
    private bool Collect(float current, float baseVal)
    {
        if (!_collecting) return false;
        _values.Add(current);
        _values.Add(baseVal);
        return true;
    }

    // the same text the old "{value:format}" and "{value * 100:format}%" made
    private static string Format(float current, string format, bool percent) =>
        percent ? (current * 100).ToString(format) + "%" : current.ToString(format);

    private void Line(string label, float current, float baseVal, string format, bool percent = false)
    {
        if (Collect(current, baseVal)) return;
        AddRow(label, current, baseVal, Format(current, format, percent));
    }

    private void ElemLine(string colorTag, string label, float current, float baseVal, string format, bool percent = false)
    {
        if (Collect(current, baseVal)) return;
        AddRow($"{colorTag}{label}{End}", current, baseVal, Format(current, format, percent));
    }

    private void IntLine(string label, int current, int baseVal)
    {
        if (Collect(current, baseVal)) return;
        AddRow(label, current, baseVal, current.ToString());
    }

    private void ElemIntLine(string colorTag, string label, int current, int baseVal)
    {
        if (Collect(current, baseVal)) return;
        AddRow($"{colorTag}{label}{End}", current, baseVal, current.ToString());
    }

    private void DropLine(string label, float percent)
    {
        if (Collect(percent, 0f)) return;
        AddRow(label, percent, 0f, DropRateText(percent));
    }

    private void AddRow(string label, float current, float baseVal, string plain) =>
        _rows.Add(new Row { label = label, value = Col(current, baseVal, $"<b>{plain}</b>") });
}

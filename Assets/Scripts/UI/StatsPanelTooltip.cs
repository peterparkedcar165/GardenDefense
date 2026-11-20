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

    // the panel keeps its right edge (next to the plant panel) while its width follows the text
    private RectTransform _panelRect;
    private float _rightEdge;
    private bool _edgeKnown;

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
        float dot = Mathf.Max(0.01f, Measure("."));
        int n = _rows.Count;
        float[] labelW = new float[n], valueW = new float[n];
        float content = 0f;
        for (int i = 0; i < n; i++)
        {
            if (_rows[i].label == null) continue;
            labelW[i] = Measure(_rows[i].label);
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
    // exceeds the screen. width now hugs too, keeping the right edge where the prefab put it
    private void FitPanel()
    {
        if (_panelRect == null) _panelRect = GetComponent<RectTransform>();
        if (!_edgeKnown)
        {
            _rightEdge = _panelRect.anchoredPosition.x + _panelRect.rect.width * (1f - _panelRect.pivot.x);
            _edgeKnown = true;
        }

        Canvas.ForceUpdateCanvases();
        float width = Mathf.Ceil(statsText.preferredWidth) + Padding;
        float height = Mathf.Min(statsText.preferredHeight + Padding, Screen.height * 0.975f);
        _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        Vector2 pos = _panelRect.anchoredPosition;
        pos.x = _rightEdge - width * (1f - _panelRect.pivot.x);
        _panelRect.anchoredPosition = pos;
    }

    private void BuildCore(Entity e)
    {
        // attack damage, armor, and (plant: attack speed/range | insect: movement speed/magic
        // armor) show in the icon row above (QuickStatRow) - not duplicated here
        ElemLine(HealCol, "Max Health:", $"{e.maxHealth:F0}", e.maxHealth, e.baseMaxHealth);
        ElemLine(Crit, "Crit Chance:", $"{e.criticalChance * 100:F1}%", e.criticalChance, e.baseCriticalChance);
        ElemLine(Crit, "Crit Damage:", $"{e.criticalDamage * 100:F0}%", e.criticalDamage, e.baseCriticalDamage);
        Blank();
        ElemLine(Magic, "Maximum Damage:", $"{e.maximumDamage * 100:F0}%", e.maximumDamage, e.baseMaximumDamage);
        ElemLine(Magic, "Minimum Damage:", $"{e.minimumDamage * 100:F0}%", e.minimumDamage, e.baseMinimumDamage);
        Blank();
        ElemLine(Magic, "Magic Power:", $"{e.magicPower:F0}", e.magicPower, e.baseMagicPower);
        Blank();
        // Armor shows in the icon row for both plants and insects; Magic Armor only for insects
        if (e is Plant)
            Line("<color=#FF69B4>Magic Armor:</color>", $"{e.magicArmor}", e.magicArmor, e.baseMagicArmor);
        Blank();
        ElemLine(Physical, "Armor Penetration:", $"{e.armorPenFlat:F0}",           e.armorPenFlat,    e.baseArmorPenFlat);
        ElemLine(Physical, "Armor Shred:",       $"{e.armorPenPercent * 100:F0}%", e.armorPenPercent, e.baseArmorPenPercent);
        ElemLine(Magic,    "Magic Penetration:", $"{e.magicPenFlat:F0}",           e.magicPenFlat,    e.baseMagicPenFlat);
        ElemLine(Magic,    "Magic Armor Shred:", $"{e.magicPenPercent * 100:F0}%", e.magicPenPercent, e.baseMagicPenPercent);
        if (e is Shooter shooter)
            ElemLine(Grass, "Piercing:", $"{shooter.piercing}", shooter.piercing, shooter.basePiercing);
        Blank();
        Line("Evasion:", $"{e.evasion * 100:F1}%", e.evasion, e.baseEvasion);
        ElemLine(Grass, "Accuracy:", $"{e.accuracy * 100:F1}%", e.accuracy, e.baseAccuracy);
        Line("<color=grey>Tenacity:</color>", $"{e.tenacity * 100:F0}%", e.tenacity, e.baseTenacity);
        ElemLine(HealCol, "Heals & Shield Bonus:", $"{e.healingBonus * 100:F0}%", e.healingBonus, e.baseHealingBonus);
        Blank();
        if (e is Insect)
        {
            ElemLine(Fire,   "Fire Resistance:",   $"{e.fireResistance * 100:F0}%",   e.fireResistance,   e.baseFireResistance);
            ElemLine(Water,  "Water Resistance:",  $"{e.waterResistance * 100:F0}%",  e.waterResistance,  e.baseWaterResistance);
            ElemLine(Grass,  "Grass Resistance:",  $"{e.grassResistance * 100:F0}%",  e.grassResistance,  e.baseGrassResistance);
            ElemLine(Ice,    "Ice Resistance:",    $"{e.iceResistance * 100:F0}%",    e.iceResistance,    e.baseIceResistance);
            ElemLine(Poison, "Poison Resistance:", $"{e.poisonResistance * 100:F0}%", e.poisonResistance, e.basePoisonResistance);
            ElemLine(Wind,   "Wind Resistance:",   $"{e.windResistance * 100:F0}%",   e.windResistance,   e.baseWindResistance);
        }
        else
        {
            ElemLine(Grass,  "Elemental Affinity:", $"{e.elementalAffinity * 100:F0}%", e.elementalAffinity, e.baseelementalAffinity);
            ElemLine(Fire,   "Fire Damage:",   $"{e.fireDamage * 100:F0}%",   e.fireDamage,   e.baseFireDamage);
            ElemLine(Water,  "Water Damage:",  $"{e.waterDamage * 100:F0}%",  e.waterDamage,  e.baseWaterDamage);
            ElemLine(Grass,  "Grass Damage:",  $"{e.grassDamage * 100:F0}%",  e.grassDamage,  e.baseGrassDamage);
            ElemLine(Ice,    "Ice Damage:",    $"{e.iceDamage * 100:F0}%",    e.iceDamage,    e.baseIceDamage);
            ElemLine(Poison, "Poison Damage:", $"{e.poisonDamage * 100:F0}%", e.poisonDamage, e.basePoisonDamage);
            ElemLine(Wind,   "Wind Damage:",   $"{e.windDamage * 100:F0}%",   e.windDamage,   e.baseWindDamage);
        }
        // the old text trimmed trailing empty lines, so a closing blank never showed
        while (_rows.Count > 0 && _rows[_rows.Count - 1].label == null) _rows.RemoveAt(_rows.Count - 1);
    }

    private void Blank() => _rows.Add(new Row());

    private void Line(string label, string plain, float current, float baseVal) =>
        _rows.Add(new Row { label = label, value = Col(current, baseVal, $"<b>{plain}</b>") });

    private void ElemLine(string colorTag, string label, string plain, float current, float baseVal) =>
        _rows.Add(new Row { label = $"{colorTag}{label}{End}", value = Col(current, baseVal, $"<b>{plain}</b>") });
}

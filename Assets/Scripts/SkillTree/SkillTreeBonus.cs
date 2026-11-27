using System.Collections.Generic;
using System.Text;
using UnityEngine;

// the Growth Bonus section in a plant's Attack, Passive and Skill tooltips (Tommy, 2026-11-27; the skill tree is called
// Plant Growth in the game, its points Growth Points, so this was the Skill Tree Bonus): every node the
// player has a point in, under the description its bonus belongs to. a stat node shows its total (its value a point
// times the points), a node that changes how the plant works shows its name and its own text. which description: the
// node's bonusIn, or by what it changes (Auto: its first stat, or the Skill when it changes the skill)
public static class SkillTreeBonus
{
    public const string Heading = "Growth Bonus:";

    // path: 1 attack, 2 passive, 3 skill. null when there's nothing to show
    public static string SectionFor(Plant plant, int path)
    {
        if (plant == null || plant.data == null || plant.data.skillTree == null || plant.data.skillTree.steps == null) return null;
        if (Plant.BuildingPreview) return null;
        string plantName = plant.data.plantName;
        StringBuilder text = null;
        foreach (SkillTreeStep step in plant.data.skillTree.steps)
        {
            if (step == null || step.nodes == null) continue;
            foreach (SkillTreeNode node in step.nodes)
            {
                if (node == null) continue;
                int rank = SkillTreeManager.GetRank(plantName, node.id);
                if (rank <= 0 || PathOf(node) != path) continue;
                foreach (string line in LinesFor(node, rank))
                {
                    if (text == null) text = new StringBuilder(Heading);
                    text.Append('\n').Append(line);
                }
            }
        }
        return text?.ToString();
    }

    // which description a node's bonus shows in: 1 attack, 2 passive, 3 skill, 0 none
    public static int PathOf(SkillTreeNode node)
    {
        switch (node.bonusIn)
        {
            case SkillTreeBonusIn.Attack: return 1;
            case SkillTreeBonusIn.Passive: return 2;
            case SkillTreeBonusIn.Skill: return 3;
            case SkillTreeBonusIn.Hidden: return 0;
        }
        if (node.modifiesSkill) return 3;
        if (!string.IsNullOrEmpty(node.unlockId) && node.unlockId.EndsWith("_instant_skill")) return 3;
        if (node.effects == null || node.effects.Count == 0) return 0;
        return PathOfStat(node.effects[0].statType);
    }

    private static int PathOfStat(StatType t)
    {
        switch (t)
        {
            case StatType.AttackDamageFlat:
            case StatType.AttackDamageTotalMultiplier:
            case StatType.AttackRange:
            case StatType.AttackRangeFlat:
            case StatType.AttackSpeedFlat:
            case StatType.CriticalChance:
            case StatType.FireDamage:
            case StatType.GrassDamage:
            case StatType.ProjectileSpeedFlat:
            case StatType.HealAmountFlat:
            case StatType.Path1LevelAdder:
                return 1;
            case StatType.Path2LevelAdder:
            case StatType.PassiveCooldownFlat:
            case StatType.PassiveDurationMultiplier:
            case StatType.MaxSlowStacksFlat:
            case StatType.IlluminationRangeAdder:
            case StatType.SnowMarkPercentFlat:
            case StatType.ToxicSporeDurationFlat:
            case StatType.Armor:
            case StatType.ArmorTotalMultiplier:
            case StatType.MaxHealth:
                return 2;
            case StatType.Path3LevelAdder:
            case StatType.SkillCooldownFlat:
            case StatType.SkillDurationAdder:
            case StatType.SkillDamage:
            case StatType.SkillChargeRate:
            case StatType.MagicPower:
            case StatType.HealingBonus:
                return 3;
            default:
                // the sun cost (it's the price, shown on the plant) and anything new
                return 0;
        }
    }

    private static IEnumerable<string> LinesFor(SkillTreeNode node, int rank)
    {
        // a node that changes how the plant works: its name and its own text (which names any stat it adds too)
        if (!string.IsNullOrEmpty(node.unlockId))
        {
            yield return $"<b>{node.nodeName}</b>: {node.description}";
            yield break;
        }
        if (node.effects == null) yield break;
        foreach (SkillNodeEffect effect in node.effects)
        {
            string line = StatLine(effect.statType, effect.valuePerRank * rank);
            if (line != null) yield return line;
        }
    }

    // the number as the stat reads it, signed, in the tooltips' bold green
    private static string Num(float v, string unit = "") =>
        $"<color=green><b>{(v >= 0f ? "+" : "")}{v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}{unit}</b></color>";

    // a stat kept as a share (0.05) reads as a percent (5%)
    private static string Pct(float v) => Num(v * 100f, "%");

    private static string Points(float v, string path) => Num(v) + $" Effective {path} Point" + (Mathf.Approximately(v, 1f) ? "" : "s");

    private static string StatLine(StatType t, float v)
    {
        switch (t)
        {
            case StatType.AttackDamageFlat: return Num(v) + " Attack Damage";
            case StatType.AttackDamageTotalMultiplier: return Pct(v) + " Total Attack Damage";
            case StatType.AttackRange: return Pct(v) + " Attack Range";
            case StatType.AttackRangeFlat: return Num(v) + " Attack Range";
            case StatType.AttackSpeedFlat: return Num(v) + " Attack Speed";
            case StatType.CriticalChance: return Pct(v) + " Critical Chance";
            case StatType.FireDamage: return Pct(v) + " Fire Damage";
            case StatType.GrassDamage: return Pct(v) + " Grass Damage";
            case StatType.ProjectileSpeedFlat: return Num(v) + " Projectile Speed";
            case StatType.HealAmountFlat: return Num(v) + " Healing";
            case StatType.Path1LevelAdder: return Points(v, "Attack");
            case StatType.Path2LevelAdder: return Points(v, "Passive");
            case StatType.Path3LevelAdder: return Points(v, "Skill");
            case StatType.PassiveCooldownFlat: return Num(-v, "s") + " Passive Cooldown";
            case StatType.PassiveDurationMultiplier: return Pct(v) + " Passive Duration";
            case StatType.MaxSlowStacksFlat: return Num(v) + " Soaked Stack Cap";
            case StatType.IlluminationRangeAdder: return Num(v) + " Illumination Range";
            case StatType.SnowMarkPercentFlat: return Pct(v) + " Snow Mark Max Health Damage";
            case StatType.ToxicSporeDurationFlat: return Num(v, "s") + " Toxic Spore Duration";
            case StatType.Armor: return Num(v) + " Armor";
            case StatType.ArmorTotalMultiplier: return Pct(v) + " Total Armor";
            case StatType.MaxHealth: return Pct(v) + " Max Health";
            case StatType.SkillCooldownFlat: return Num(-v, "s") + " Skill Cooldown";
            case StatType.SkillDurationAdder: return Num(v, "s") + " Skill Duration";
            case StatType.SkillDamage: return Num(v) + " Skill Damage";
            case StatType.SkillChargeRate: return Pct(v) + " Skill Charge Rate";
            case StatType.MagicPower: return Num(v) + " Magic Power";
            case StatType.HealingBonus: return Pct(v) + " Heals & Shields Bonus";
            default: return null;
        }
    }
}

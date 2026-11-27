using UnityEngine;
using System.Collections.Generic;

// one asset per plant, an ordered chain of steps read left to right
// a step with one node is a normal stop, a step with two nodes is an exclusive fork
[CreateAssetMenu(fileName = "SkillTreeData", menuName = "Scriptable Objects/Skill Tree")]
public class SkillTreeData : ScriptableObject
{
    public List<SkillTreeStep> steps = new List<SkillTreeStep>();
}

[System.Serializable]
public class SkillTreeStep
{
    // one node for a normal step, two nodes for a choose one fork
    public List<SkillTreeNode> nodes = new List<SkillTreeNode>();

    // total ranks that must be bought in this step before the next step opens
    public int ranksToUnlockNext = 1;
}

[System.Serializable]
public class SkillTreeNode
{
    // unique within the tree, stored in save data
    public string id;
    public string nodeName;
    [TextArea] public string description;
    public int maxRank = 3;
    public int costPerRank = 1;

    // stat bonuses applied per rank when a plant of this tree spawns
    public List<SkillNodeEffect> effects = new List<SkillNodeEffect>();

    // optional behavior unlock checked in code via SkillTreeManager.HasUnlock
    public string unlockId;

    // the node changes the plant's skill: it shows the plant's skill icon (Tommy, 2026-11-27). a node raising a
    // skill stat and the instant skill unlock show it anyway (SkillNodeButton.ShowsSkillIcon)
    [Tooltip("this node changes the plant's skill, so it shows the skill's icon")]
    public bool modifiesSkill;

    // which of the plant's Attack, Passive and Skill tooltips shows this node's Skill Tree Bonus once it has a point
    // (Tommy, 2026-11-27; SkillTreeBonus). Auto goes by its first stat, or the Skill when it changes the skill
    [Tooltip("which tooltip shows this node's bonus once it has a point: Auto goes by its stat (or the Skill when it changes the skill)")]
    public SkillTreeBonusIn bonusIn;
}

public enum SkillTreeBonusIn { Auto, Attack, Passive, Skill, Hidden }

[System.Serializable]
public struct SkillNodeEffect
{
    public StatType statType;
    public float valuePerRank;
}

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
}

[System.Serializable]
public struct SkillNodeEffect
{
    public StatType statType;
    public float valuePerRank;
}

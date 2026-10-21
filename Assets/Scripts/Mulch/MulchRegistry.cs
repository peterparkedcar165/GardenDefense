using UnityEngine;

// mirrors PlantRegistry - the universal mulch pool available in every level by default.
// LevelConfig.mulchPool is for level-exclusive mulches only; anything not tied to a
// specific level belongs here instead, so adding a new universal mulch never requires
// touching every level asset
[CreateAssetMenu(fileName = "MulchRegistry", menuName = "Scriptable Objects/Mulch Registry")]
public class MulchRegistry : ScriptableObject
{
    public MulchData[] mulches;
}

using UnityEngine;

[CreateAssetMenu(fileName = "BudMulchData", menuName = "Scriptable Objects/Mulch/Bud")]
public class BudMulchData : MulchData
{
    [Tooltip("whole waves the free plant can't target insects, counted from the first wave that starts after it's placed")]
    public int dormantWaves = 2;

    public override MulchEffect CreateEffect() => new BudMulchEffect(this);

    public override string GetDescription() =>
        $"Your next plant is <b><color=green>free</color></b>, but it can't target insects for <b><color=red>{dormantWaves}</color></b> waves. " +
        $"It can still be hit, and its <b><color=#FFD700>Sun</color></b> and anything it does for other plants keep working.";
}

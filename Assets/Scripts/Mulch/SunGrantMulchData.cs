using UnityEngine;

// fertile start, rich loam and black gold: one data class, three assets, one per tier. the sun
// lands the moment the mulch is picked
[CreateAssetMenu(fileName = "SunGrantMulchData", menuName = "Scriptable Objects/Mulch/Sun Grant")]
public class SunGrantMulchData : MulchData
{
    [Tooltip("Sun given the moment this mulch is picked")]
    public int sunAmount = 80;

    public override MulchEffect CreateEffect() => new SunGrantMulchEffect(this);

    public override string GetDescription() =>
        $"Gain <b><color=green>{sunAmount}</color></b> <b><color=#FFD700>Sun</color></b> right now.";
}

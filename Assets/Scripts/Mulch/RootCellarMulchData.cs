using UnityEngine;

[CreateAssetMenu(fileName = "RootCellarMulchData", menuName = "Scriptable Objects/Mulch/Root Cellar")]
public class RootCellarMulchData : MulchData
{
    [Tooltip("share of the banked Sun paid as interest at the end of each wave, e.g. 0.15 = 15%. no cap")]
    public float interestPercent = 0.15f;

    public override MulchEffect CreateEffect() => new RootCellarMulchEffect(this);

    public override string GetDescription() =>
        $"At the end of each wave, gain <b><color=green>{interestPercent * 100f:F0}%</color></b> of your banked <b><color=#FFD700>Sun</color></b> as interest. No cap.";
}

using UnityEngine;

[CreateAssetMenu(fileName = "BargainSeedsMulchData", menuName = "Scriptable Objects/Mulch/Bargain Seeds")]
public class BargainSeedsMulchData : MulchData
{
    [Tooltip("flat Sun taken off the first plant placed each wave")]
    public int discount = 15;

    public override MulchEffect CreateEffect() => new BargainSeedsMulchEffect(this);

    public override string GetDescription() =>
        $"The first plant you place each wave costs <b><color=green>{discount}</color></b> less <b><color=#FFD700>Sun</color></b>.";
}

using UnityEngine;

// nothing to do during the level: at level end Plant.CommitPendingExpToSave asks
// MulchManager.WisdomBonusFor each plant type, which adds up Bonus over its living plants
public class WisdomLeafMulchEffect : MulchEffect
{
    private readonly WisdomLeafMulchData typedData;

    public WisdomLeafMulchEffect(WisdomLeafMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public float Bonus => V(typedData.expBonus);

    public override void Activate() {}
    public override void Deactivate() {}

    // what it would add to its plant type's exp if the level ended now (it needs to be alive then)
    public override string GetStatus()
    {
        if (Owner == null || Owner.data == null) return null;
        int exp = Plant.LevelExpSoFar(Owner.data.plantName);
        return $"Extra EXP so far: +{Mathf.RoundToInt(exp * Bonus)}";
    }
}

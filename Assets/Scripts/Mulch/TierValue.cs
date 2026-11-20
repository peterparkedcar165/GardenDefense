using UnityEngine;

// one number of a mulch at each rarity, set per mulch in the Inspector. the defaults follow
// Tommy's starting rule (Common x1, Rare x2, Epic x3); every one is meant to be hand tuned later
[System.Serializable]
public struct TierValue
{
    public float common;
    public float rare;
    public float epic;

    public TierValue(float common, float rare, float epic)
    {
        this.common = common;
        this.rare = rare;
        this.epic = epic;
    }

    // the starting rule: Rare is twice Common, Epic three times
    public static TierValue Scaled(float common) => new TierValue(common, common * 2f, common * 3f);

    public float Get(MulchTier rarity) => rarity switch
    {
        MulchTier.Rare => rare,
        MulchTier.Epic => epic,
        _              => common
    };

    public int GetInt(MulchTier rarity) => Mathf.RoundToInt(Get(rarity));
}

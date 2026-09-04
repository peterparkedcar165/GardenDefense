using UnityEngine;

public class Snail : Insect
{
    private SnailData SData => data as SnailData;

    private float ArmorBonusWhileShielded       => SData?.armorBonusWhileShielded       ?? 150f;
    private float MoveSpeedBonusWhileUnshielded => SData?.moveSpeedBonusWhileUnshielded  ?? 0.5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        bool shielded = HasEffect<ShieldEffect>();
        float armorBonus     = shielded ? ArmorBonusWhileShielded       : 0f;
        float moveSpeedBonus = shielded ? 0f : MoveSpeedBonusWhileUnshielded;
        armorAdder += armorBonus;
        movementSpeedMultiplier += moveSpeedBonus;
        base.UpdateStats();
        armorAdder -= armorBonus;
        movementSpeedMultiplier -= moveSpeedBonus;
    }

    public override string GetDescription() =>
        $"Slow, passive, but tough insect. Spawns with a shield of <b>{(data != null ? data.startingShield : 0f):F0}</b>.\n\n" +
        $"While Shielded, the Snail gains <color=#00CED1><b>{ArmorBonusWhileShielded:F0}</b></color> Armor.\n\n" +
        $"While Unshielded, the Snail gains <color=green><b>{MoveSpeedBonusWhileUnshielded * 100f:F0}%</b></color> Movement Speed." + AggressivityLine();
}

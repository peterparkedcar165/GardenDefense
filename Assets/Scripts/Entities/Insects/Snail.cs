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
}

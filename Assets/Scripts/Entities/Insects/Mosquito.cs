public class Mosquito : FlyingInsect
{
    public float biteDPS = 12f;
    public float biteDuration = 4f;
    public float biteHealingReductionPercent = 0.5f;
    public float bloodsuckerHealPerSecond = 12f;
    public float bloodsuckerHealPercent = 0.02f;

    private MosquitoData MData => data as MosquitoData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.Low;
        if (MData != null)
        {
            biteDPS = MData.biteDPS;
            biteDuration = MData.biteDuration;
            biteHealingReductionPercent = MData.biteHealingReductionPercent;
            bloodsuckerHealPerSecond = MData.bloodsuckerHealPerSecond;
            bloodsuckerHealPercent = MData.bloodsuckerHealPercent;
        }
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        bool hit = current.ReceiveAttack(attackDamage, this);
        Entity victim = current as Entity;
        if (!hit || victim == null || !current.IsAlive) return;

        victim.ApplyEffect(new MosquitoBiteEffect(victim, biteDuration, 1, this, biteDPS, biteHealingReductionPercent));
        ApplyEffect(new BloodsuckerEffect(this, biteDuration, 1, this, bloodsuckerHealPerSecond, bloodsuckerHealPercent));
    }

    public override string GetDescription() =>
        $"Mildly aggressive insect. Attacks apply <color=#8B0000><b>Mosquito Bite</b></color>, dealing <color=green><b>{(MData?.biteDPS ?? biteDPS):F0}</b></color> " +
        $"<color=#6B8E23>Grass</color> True damage per second and reducing incoming healing by <color=red><b>{(MData?.biteHealingReductionPercent ?? biteHealingReductionPercent) * 100f:F0}%</b></color> for <b>{(MData?.biteDuration ?? biteDuration):F0}</b> seconds.\n\n" +
        $"While biting, Mosquito gains <color=#8B0000><b>Bloodsucker</b></color>, healing <color=green><b>{(MData?.bloodsuckerHealPerSecond ?? bloodsuckerHealPerSecond):F0}</b></color> + <color=green><b>{(MData?.bloodsuckerHealPercent ?? bloodsuckerHealPercent) * 100f:F0}%</b></color> max health per second." +
        FlyingLine() + AggressivityLine();
}

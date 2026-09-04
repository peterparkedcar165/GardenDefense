using UnityEngine;

// fast cave insect with high evasion. all tuning (speed, evasion) is set in its InsectData SO.
// "Silver Ellusivity": doubles its total (post-formula) Evasion stat while in darkness
public class Silverfish : Insect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        bool inDarkness = DarknessManager.instance != null && !DarknessManager.instance.IsIlluminated(transform.position);
        if (inDarkness) evasion *= 2f;
    }

    public override string GetDescription() =>
        "Passive but fast and ellusive insect. <color=green><b>Silver Ellusivity</b></color>: doubles the total " +
        "<color=green><b>Evasion</b></color> stat while in darkness." + AggressivityLine();
}

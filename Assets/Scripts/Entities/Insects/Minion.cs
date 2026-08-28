using UnityEngine;

// a friendly combatant summoned onto the path by a Burgeon plant. it reuses all of Insect
// (movement, melee, health, effects) but is on the Friendly team: it holds its post, lets
// enemies come to it, and engages them. it drops no sun/exp, never reaches the objective,
// and despawns after its lifetime. it lives in Insect.friendlyInsects, not allInsects, so
// the player's own plants and AoE never target it
public class Minion : Insect
{
    [SerializeField] protected Transform rangeCircle;     // a circle child shown on hover/select

    public float lifetime = 12f;
    private float _lifeTimer;
    private bool _hovered;
    private Plant _owner;

    // the plant that summoned it. assigning it also makes the plant the source/credit for the
    // minion's damage (exp, sun yield, elemental reaction power)
    public Plant owner
    {
        get => _owner;
        set { _owner = value; attackSourceOverride = value; }
    }

    protected override bool ChasesTarget => false;   // holds position, lets enemies come to it
    protected override bool ScalesWithWave => false;  // minion health does not scale with the wave

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        SetTeam(Team.Friendly);
    }

    // called by the summoning plant right after Instantiate
    public void Initialize(float lifetime)
    {
        this.lifetime = lifetime;
    }

    protected override void OnHover()     { base.OnHover();     _hovered = true; }
    protected override void OnHoverExit() { base.OnHoverExit(); _hovered = false; }

    protected override void Update()
    {
        base.Update();
        if (owner == null || !owner.IsAlive) { Kill(); return; }
        UpdateRangeCircle();
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= lifetime) Kill();
    }

    // sizes the range circle to a radius (diameter = radius * 2) and hides it. call once stats are known
    protected void SetRangeCircle(float radius)
    {
        if (rangeCircle == null) return;
        rangeCircle.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        rangeCircle.gameObject.SetActive(false);
    }

    // the circle is centered on the minion's visual and shows only while this minion, or its
    // owner plant, is hovered or selected
    private void UpdateRangeCircle()
    {
        if (rangeCircle == null) return;
        if (visual != null) rangeCircle.position = visual.position;   // center on the visual, not the root
        bool selected   = PlantUpgradeUI.instance != null && PlantUpgradeUI.instance.GetSelectedInsect() == this;
        bool ownerShown = owner != null && (owner.IsHovered || owner.IsSelected);
        bool show = _hovered || selected || ownerShown;
        if (rangeCircle.gameObject.activeSelf != show)
            rangeCircle.gameObject.SetActive(show);
    }

    // minions deal their attack damage boosted by their owner plant's minionDamage multiplier
    public override void Attack()
    {
        if (target == null) return;
        float mult = owner != null ? 1f + owner.minionDamage : 1f;
        target.ReceiveAttack(attackDamage * mult, this);
    }

    protected override void ReachObjective() { }   // minions never reach the player's objective

    // death is handled by Insect.Kill's friendly branch (QuietDeath): no sun, exp, or events

    public override string GetName() => data != null ? data.displayName : "Minion";
}

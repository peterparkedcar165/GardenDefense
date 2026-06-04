using UnityEngine;

// a friendly combatant summoned onto the path by a Burgeon plant. it reuses all of Insect
// (movement, melee, health, effects) but is on the Friendly team: it holds its post, lets
// enemies come to it, and engages them. it drops no sun/exp, never reaches the objective,
// and despawns after its lifetime. it lives in Insect.friendlyInsects, not allInsects, so
// the player's own plants and AoE never target it
public class Minion : Insect
{
    public float lifetime = 12f;
    private float _lifeTimer;

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

    protected override void Update()
    {
        base.Update();
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= lifetime) Kill();
    }

    protected override void ReachObjective() { }   // minions never reach the player's objective

    // death is handled by Insect.Kill's friendly branch (QuietDeath): no sun, exp, or events

    public override string GetName() => data != null ? data.displayName : "Minion";
}

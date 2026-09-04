using UnityEngine;
using System.Collections;

// a flyer that ferries a ground insect past danger: while carrying, the passenger rides on its
// back, frozen in place and excluded from single-target selection (see the carriedBy exclusion
// in Plant.FindNearest/FindFirst/FindLast/FindStrongest) but still vulnerable to AoE/splash, and
// only ever gets dropped if the Duskdarter itself is grounded or killed (see the ICarrierInsect
// hooks in Insect.cs). also innately faster/tougher than a typical flyer, stronger in darkness,
// and immune to Taunt so it can't be dragged into fighting plants mid-delivery
public class Duskdarter : FlyingInsect, ICarrierInsect
{
    private DuskdarterData DDData => data as DuskdarterData;

    private Insect carriedInsect;
    public Insect CarriedInsect => carriedInsect;
    public override float CarryPickupHeight => DDData?.carryPickupHeight ?? 0.9f;

    private float pickupCheckTimer;
    private SpriteRenderer _carriedRenderer;
    private int _carriedOriginalSortingOrder;
    private bool _isLandingForPickup;
    // true for the whole land->pause->carry->takeoff sequence, not just once carriedInsect is
    // finally set at the end of it - without this, a periodic pickup check landing mid-sequence
    // would see carriedInsect still null and start a second, competing PickUpSequence coroutine
    private bool _isPickingUp;

    protected override bool FallDamageImmune => _isLandingForPickup;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void UpdateStats()
    {
        bool inDarkness = DarknessManager.instance != null && !DarknessManager.instance.IsIlluminated(transform.position);
        float spdBonus = inDarkness ? (DDData?.darkMovementSpeedBonus ?? 0.25f) : 0f;
        float evaBonus = inDarkness ? (DDData?.darkEvasionBonus      ?? 0.15f) : 0f;

        movementSpeedMultiplier += spdBonus;
        evasionAdder            += evaBonus;
        base.UpdateStats();
        movementSpeedMultiplier -= spdBonus;
        evasionAdder            -= evaBonus;
    }

    public override string GetDescription() =>
        "May pick up an insect, allowing it to travel at a quicker rate while flying. Gains " +
        $"<color=green><b>{(DDData?.darkMovementSpeedBonus ?? 0.25f) * 100f:F0}%</b></color> Movement Speed, and " +
        $"<color=green><b>{(DDData?.darkEvasionBonus ?? 0.15f) * 100f:F0}%</b></color> Evasion when in darkness. " +
        "Immune to plant-based Taunt effects." + FlyingLine() + AggressivityLine();

    protected override void Update()
    {
        base.Update();

        if (carriedInsect != null && !carriedInsect.IsAlive)
            carriedInsect = null;

        if (carriedInsect != null || _isPickingUp) return;

        pickupCheckTimer -= Time.deltaTime;
        if (pickupCheckTimer <= 0f)
        {
            pickupCheckTimer = DDData?.carryPickupCheckInterval ?? 1f;
            TryPickUpNearbyInsect();
        }
    }

    // immune to a Taunt that redirects attacks onto a Plant directly (Cactus's shield taunt,
    // etc: source and taunter are both the plant itself). AcornBomb's taunt still lands: its
    // source is the AcornSprout that spawned it, but the taunter (what insects actually get
    // redirected onto) is the AcornBomb minion prop, not a Plant, so it isn't a "taunt by a plant"
    // in the sense that matters here
    //
    // separately: a genuine hard CC landing mid-descent during its own voluntary pickup landing
    // cancels that landing's fall-damage grace, so a CC can't get "absorbed for free" just
    // because it happened to land during a scripted descent
    public override void ApplyEffect(StatusEffect effect)
    {
        if (effect is TauntEffect taunt && taunt.source is Plant && taunt.taunter is Plant) return;
        if (effect is HardCrowdControl) _isLandingForPickup = false;
        base.ApplyEffect(effect);
    }

    // grounded (hard CC, knocked down, etc.): the passenger falls off, same as on death
    public override void SetFlight(bool newState)
    {
        base.SetFlight(newState);
        if (!newState && !_isLandingForPickup) DropCarriedInsect();
    }

    // targets the highest carryPriority un-carried ground insect within range, then starts the
    // land -> pause -> carry -> take off sequence instead of picking it up instantly. insects
    // that should naturally get ferried past danger (slow, tanky, or otherwise worth protecting,
    // e.g. Snail) are tuned via InsectData.carryPriority rather than being inferred from speed.
    // no longer requires the candidate to be at/ahead of this Duskdarter's own path progress -
    // an insect that stops periodically to attack (e.g. Bombardier Beetle) would otherwise fall
    // behind and become permanently ineligible once a Duskdarter passes it
    private void TryPickUpNearbyInsect()
    {
        float range = DDData?.carryPickupRange ?? 3f;
        Insect best = null;
        int bestPriority = int.MinValue;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect == this) continue;
            if (insect is FlyingInsect || insect is ICarrierInsect || insect.carriedBy != null || insect.movementPaused) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > range) continue;
            if (insect.carryPriority > bestPriority) { bestPriority = insect.carryPriority; best = insect; }
        }
        if (best != null) StartCoroutine(PickUpSequence(best));
    }

    private IEnumerator PickUpSequence(Insect target)
    {
        _isPickingUp = true;
        movementPaused = true;
        target.movementPaused = true;

        _isLandingForPickup = true;
        SetFlight(false);
        yield return new WaitUntil(() => isDying || isOnGround);

        if (!isDying)
            yield return new WaitForSeconds(DDData?.carryPickupDelay ?? 0.5f);

        if (!isDying && target != null && target.IsAlive)
            TryCarry(target);

        if (target != null) target.movementPaused = false;

        _isLandingForPickup = false;
        if (!isDying)
        {
            SetFlight(true);
            movementPaused = false;
        }
        _isPickingUp = false;
    }

    // public so a wave spawner can also call this right after spawning both insects, to have a
    // Duskdarter start a level already carrying a specific passenger
    public bool TryCarry(Insect target)
    {
        if (target == null || !target.IsAlive || carriedInsect != null || target.carriedBy != null) return false;
        carriedInsect = target;
        target.carriedBy = this;

        // sit visually above and behind: higher on screen, but drawn behind this Duskdarter's
        // own sprite, as if riding tucked against its back
        _carriedRenderer = target.visual != null ? target.visual.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer ownRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (_carriedRenderer != null && ownRenderer != null)
        {
            _carriedOriginalSortingOrder = _carriedRenderer.sortingOrder;
            _carriedRenderer.sortingOrder = ownRenderer.sortingOrder - 1;
        }
        return true;
    }

    public void DropCarriedInsect()
    {
        if (carriedInsect == null) return;
        carriedInsect.fallDamageSource = this;
        carriedInsect.carriedBy = null;
        if (_carriedRenderer != null) _carriedRenderer.sortingOrder = _carriedOriginalSortingOrder;
        _carriedRenderer = null;
        carriedInsect = null;
    }

    // reaching the objective while still carrying: the base takes damage from both insects
    protected override void ReachObjective()
    {
        if (carriedInsect != null)
        {
            DamagePlayerBase((int)carriedInsect.baseAttackDamage);
            Insect.allInsects.Remove(carriedInsect);
            Destroy(carriedInsect.gameObject);
            carriedInsect = null;
        }
        base.ReachObjective();
    }
}

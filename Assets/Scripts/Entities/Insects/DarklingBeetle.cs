using UnityEngine;
using System.Collections;

// a ground hit-sponge that carries another ground insect on its back, keeping it fully immune to
// new damage (single-target or AoE) via Insect.ShieldsCarriedPassenger - unlike Duskdarter, whose
// passenger is only excluded from single-target selection and still takes AoE/splash damage. any
// DoT already ticking on the passenger before pickup is unaffected (see Insect.Damage overrides).
// pickup itself follows the exact same pause-then-delay-then-carry process as Duskdarter (see
// PickUpSequence), just without the flying-specific land/takeoff steps, since it never leaves
// the ground - the same process is intentional so every carrier behaves consistently
public class DarklingBeetle : Insect, ICarrierInsect
{
    private DarklingBeetleData DBData => data as DarklingBeetleData;

    private Insect carriedInsect;
    public Insect CarriedInsect => carriedInsect;
    private SpriteRenderer _carriedRenderer;
    private int _carriedOriginalSortingOrder;

    private float pickupCheckTimer;
    // true for the whole pause->delay->carry sequence, not just once carriedInsect is finally
    // set at the end of it - same re-entrancy guard Duskdarter uses, for the same reason: a
    // periodic pickup check landing mid-sequence would otherwise see carriedInsect still null
    // and start a second, competing PickUpSequence coroutine
    private bool _isPickingUp;

    protected override bool ShieldsCarriedPassenger => true;
    public override float CarryPickupHeight => DBData?.carryPickupHeight ?? 0.4f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.Medium;
    }

    protected override void Update()
    {
        base.Update();

        if (carriedInsect != null && !carriedInsect.IsAlive)
            DropCarriedInsect();

        if (carriedInsect != null || _isPickingUp) return;

        pickupCheckTimer -= Time.deltaTime;
        if (pickupCheckTimer <= 0f)
        {
            pickupCheckTimer = DBData?.carryPickupCheckInterval ?? 1f;
            TryPickUpNearbyInsect();
        }
    }

    // same highest-carryPriority selection convention as Duskdarter
    private void TryPickUpNearbyInsect()
    {
        float range = DBData?.carryPickupRange ?? 1.5f;
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

    // same pause -> wait carryPickupDelay -> carry -> resume process as Duskdarter.PickUpSequence,
    // minus the land/takeoff steps (there's no flight to interrupt here)
    private IEnumerator PickUpSequence(Insect target)
    {
        _isPickingUp = true;
        movementPaused = true;
        target.movementPaused = true;

        yield return new WaitForSeconds(DBData?.carryPickupDelay ?? 0.5f);

        if (!isDying && target != null && target.IsAlive)
            TryCarry(target);

        if (target != null) target.movementPaused = false;

        movementPaused = false;
        _isPickingUp = false;
    }

    public bool TryCarry(Insect target)
    {
        if (target == null || !target.IsAlive || carriedInsect != null || target.carriedBy != null) return false;
        carriedInsect = target;
        target.carriedBy = this;

        // sit visually tucked behind this beetle's own sprite, same trick Duskdarter uses
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
        carriedInsect.carriedBy = null;
        if (_carriedRenderer != null) _carriedRenderer.sortingOrder = _carriedOriginalSortingOrder;
        _carriedRenderer = null;
        carriedInsect = null;
    }

    public override string GetDescription() =>
        "Mildly aggressive and bulky insect.\n\n" +
        "Has drastically increased Armor, and slightly increased Magic Armor.\n\n" +
        "May carry an insect and keep it from harm as it travels towards its destination." + AggressivityLine();
}

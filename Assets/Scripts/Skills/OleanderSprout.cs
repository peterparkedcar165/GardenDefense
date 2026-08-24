using UnityEngine;
using System.Collections.Generic;

// Nerium Oleander's skill: a stationary totem that petals can bounce through for free, and
// that curses nearby insects with reduced Poison Resistance for as long as it stands.
// tracked in a static list so petals (from any Oleander) and the caster's own targeting can
// find nearby sprouts without needing a direct reference
public class OleanderSprout : MonoBehaviour
{
    public static readonly List<OleanderSprout> allSprouts = new List<OleanderSprout>();

    public NeriumOleander owner;
    private float lifetime;
    private float curseReduction;

    // mirrors the owner's attack range live, so an attack range buff gained while the sprout
    // is already standing widens its aura immediately. falls back to the radius captured at
    // cast time if the owner is gone (e.g. the oleander died mid-duration)
    private float radiusAtCast;
    private float CurrentRadius => owner != null ? owner.attackRange : radiusAtCast;

    // the sprout is an indestructible totem, not a real combatant: it can't take damage or be
    // affected by any status effect. these exist so it reads as "can't die" rather than having
    // no health concept at all, not because anything currently deals damage to it
    public readonly int maxHealth = 1;
    public readonly int health = 1;
    public bool IsAlive => true;

    private const float CurseTickInterval = 0.5f;
    private float curseTickTimer;
    private const float PositionCircleScale = 0.2f;
    private const float PositionHitboxRadius = PositionCircleScale / 2f;

    // scaled in Initialize(): one shows the curse aura's true extent, the other marks the
    // sprout's own small hitbox that petals bounce off of. assign in the prefab
    [SerializeField] private Transform rangeCircleVisual;
    [SerializeField] private Transform positionCircleVisual;

    public void Initialize(NeriumOleander owner, float radius, float lifetime, float curseReduction)
    {
        this.owner = owner;
        this.radiusAtCast = radius;
        this.lifetime = lifetime;
        this.curseReduction = curseReduction;

        // small physical hitbox so petals can detect/bounce off the sprout; unrelated to the
        // (much larger) curse aura radius, which is checked manually each tick below
        CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = PositionHitboxRadius;

        // Unity 2D only fires trigger callbacks between colliders if at least one side has a
        // Rigidbody2D; without this, a static sprout collider and a static petal collider never
        // generate OnTriggerEnter2D at all, and the petal flies straight through
        Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // both circles stay visible for as long as the sprout stands, no hover/selection needed
        if (rangeCircleVisual != null)
            rangeCircleVisual.gameObject.SetActive(true);
        if (positionCircleVisual != null)
        {
            positionCircleVisual.localScale = new Vector3(PositionCircleScale, PositionCircleScale, 1f);
            positionCircleVisual.gameObject.SetActive(true);
        }
    }

    private void OnEnable()  => allSprouts.Add(this);
    private void OnDisable() => allSprouts.Remove(this);

    private void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float currentRadius = CurrentRadius;
        if (rangeCircleVisual != null)
        {
            float s = currentRadius * 2f;
            rangeCircleVisual.localScale = new Vector3(s, s, 1f);
        }

        curseTickTimer += Time.deltaTime;
        if (curseTickTimer < CurseTickInterval) return;
        curseTickTimer -= CurseTickInterval;

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= currentRadius)
                insect.ApplyEffect(new OleandicCurseEffect(insect, CurseTickInterval * 1.5f, 1, owner, curseReduction));
        }
    }
}

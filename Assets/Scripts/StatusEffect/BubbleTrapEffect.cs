using UnityEngine;

public class BubblePrisonEffect : Airborne
{
    private Transform visual;
    private float knockUpHeight;
    private float storedVisualY;
    private float baseY;
    private float phase;
    private GameObject bubbleVisual;
    private const float bobSpeed = 2f;
    private const float bobAmplitude = 0.08f;
    private readonly bool _rising;
    // extra damage the imprisoned insect takes from every source while trapped (Entity.damageTakenBonus)
    private readonly float _damageTakenBonus;
    // waterlily skill max bonus: on pop, the insect is left at its source's full Soaked stacks
    private readonly bool _popSoak;
    private const float riseSpeed = 0.5f;

    // skill tree nodes 5.1/5.2
    private const float BacktrackSpeed = 0.3f;
    private const float PopDamagePercentMaxHealth = 0.1f;
    private readonly bool _hasBacktrack;
    private readonly bool _hasPopDamage;
    private static readonly DamageTag[] popDamageTags = { DamageTag.SkillDamage };

    public BubblePrisonEffect(Entity target, float duration, int level, Entity source, bool rising = false, float damageTakenBonus = 0f, bool popSoak = false)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Water;
        _rising = rising;
        _damageTakenBonus = damageTakenBonus;
        _popSoak = popSoak;
        Plant sourcePlant = source as Plant;
        _hasBacktrack = sourcePlant != null && SkillTreeManager.HasUnlock(sourcePlant, Waterlily.BubbleBacktrackUnlock);
        _hasPopDamage = sourcePlant != null && SkillTreeManager.HasUnlock(sourcePlant, Waterlily.BubblePopDamageUnlock);
    }

    public override string GetName() => "<color=#4FC3F7>Bubble Prison</color>";
    public override string GetDescription() =>
        $"Hanging in the air, imprisoned within a bubble. Takes <color=red><b>{_damageTakenBonus * 100f:F0}%</b></color> increased damage from all sources.";

    public override void OnApply()
    {
        target.damageTakenBonus += _damageTakenBonus;

        Insect insect = target as Insect;
        if (insect == null) return;
        visual = insect.transform.Find("Visual");

        Sprite bubbleSprite = Resources.Load<Sprite>("bubblePrisonTrapped");
        if (bubbleSprite != null && visual != null)
        {
            bubbleVisual = new GameObject("BubblePrisonVisual");
            bubbleVisual.transform.SetParent(visual);
            bubbleVisual.transform.localPosition = Vector3.zero;
            bubbleVisual.transform.localScale = Vector3.one * 4f; // SIZE OF BUBBLE
            SpriteRenderer sr = bubbleVisual.AddComponent<SpriteRenderer>();
            sr.sprite = bubbleSprite;
            SpriteRenderer insectSR = visual.GetComponent<SpriteRenderer>();
            if (insectSR != null)
            {
                sr.sortingLayerID = insectSR.sortingLayerID;
                sr.sortingOrder = insectSR.sortingOrder + 1;
            }
        }

        if (target is FlyingInsect fi)
        {
            storedVisualY = visual != null ? visual.localPosition.y : 0f;
            fi.SetFlight(false);
        }
        else
        {
            knockUpHeight = Random.Range(0.3f, 0.5f);
        }

        if (_rising && target is Insect ri)
            ri.fallDamageSource = source;
    }

    public override void OnTick(float deltaTime)
    {
        if (_hasBacktrack && target is Insect backtrackInsect)
            backtrackInsect.PullBackTowardPreviousWaypoint(BacktrackSpeed);

        if (visual == null) return;

        Insect insect = target as Insect;
        if (insect != null && insect.verticalVelocity != 0f)
        {
            if (target is FlyingInsect)
                storedVisualY = visual.localPosition.y;
            else
            {
                baseY = visual.localPosition.y;
                knockUpHeight = visual.localPosition.y;
            }

            if (insect.HasEffect<KnockUpEffect>()) return; // geyser overrides , let ApplyGravity control Y
            if (insect.affectedByGravity) return;
            insect.verticalVelocity = 0f;          // bubble caught at apex: snap vV so bob runs
        }

        if (_rising)
        {
            if (target is FlyingInsect)
                storedVisualY += riseSpeed * deltaTime;
            else
                knockUpHeight += riseSpeed * deltaTime;
        }

        phase += bobSpeed * deltaTime;
        float bob = Mathf.Sin(phase) * bobAmplitude;

        Vector3 pos = visual.localPosition;
        if (target is FlyingInsect)
            pos.y = storedVisualY + bob;
        else
        {
            baseY = Mathf.MoveTowards(baseY, knockUpHeight, 8f * deltaTime);
            pos.y = baseY + bob;
        }
        visual.localPosition = pos;
    }

    public override void OnExpire()
    {
        if (bubbleVisual != null)
            UnityEngine.Object.Destroy(bubbleVisual);

        // skill tree node 5.2: an extra hit of 10% max health when the bubble pops, on top of
        // whatever damage the skill's own impact already dealt on cast
        if (_hasPopDamage && target is Insect popInsect && popInsect.IsAlive)
        {
            float popDamage = target.maxHealth * PopDamagePercentMaxHealth;
            Plant sourcePlant = source as Plant;
            if (sourcePlant != null)
                target.Damage(popDamage, sourcePlant.damageType, sourcePlant.elementalType, source, true, popDamageTags);
            else
                target.Damage(popDamage, DamageType.Physical, elementalType, popDamageTags);
        }

        // taken back after the pop so the pop hit itself still gets the bonus
        target.damageTakenBonus -= _damageTakenBonus;

        if (_popSoak && source is Waterlily popWaterlily && target is Insect soakInsect && soakInsect.IsAlive)
            popWaterlily.ApplyFullSoak(soakInsect);

        if (visual == null) return;

        if (target is FlyingInsect)
        {
            // never shorten a longer ground that is already running (e.g. the pop soak above)
            GroundedEffect existingGround = target.GetEffect<GroundedEffect>();
            if (existingGround == null || existingGround.duration < 5f)
                target.ApplyEffect(new GroundedEffect(target, 5f, 1, source));
        }
        else
        {
            // gravity takes over naturally from current visual height
        }
    }
}

using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DoTAggregator : MonoBehaviour
{
    // one aggregator per (target entity, elemental type) pair
    private static readonly Dictionary<(Entity, ElementalType), DoTAggregator> active = new();
    // ordered list per entity so stacking positions can be recalculated when one expires
    private static readonly Dictionary<Entity, List<DoTAggregator>> entityStacks = new();

    private Entity target;
    private ElementalType myElement;
    private float currentOffset;     // visual offset, lerps toward target slot
    private TMP_Text tmpText;
    private float totalDamage = 0f;
    private float fadeTimer = 0f;    // resets to 0 on every new tick
    private bool detached = false;   // true after entity death; stops following, lets fade finish
    private Vector3 detachedPosition;

    private const float fadeDuration = 1f;    // non-time-scaled seconds from last tick to gone
    private const float baseOffset = 1.1f;
    private const float stackSpacing = 0.3f;
    private const float offsetLerpSpeed = 8f;
    private const float baseSize = 4f;
    private const float sizeScale = 0.04f;
    private const float maxSize = 6f;

    public static void AddDamage(Entity entity, float amount, ElementalType elementalType)
    {
        if (entity == null || amount <= 0f) return;

        var key = (entity, elementalType);

        if (!active.TryGetValue(key, out DoTAggregator agg) || agg == null)
        {
            if (!entityStacks.TryGetValue(entity, out var stack))
            {
                stack = new List<DoTAggregator>();
                entityStacks[entity] = stack;
            }

            float spawnOffset = baseOffset + stack.Count * stackSpacing;
            GameObject go = Object.Instantiate(
                Resources.Load<GameObject>("DoTAggregator"),
                entity.transform.position + Vector3.up * spawnOffset,
                Quaternion.identity);
            agg = go.GetComponent<DoTAggregator>();
            if (agg == null) return;
            agg.Init(entity, elementalType, spawnOffset);
            active[key] = agg;
            stack.Add(agg);
        }

        agg.Accumulate(amount);
    }

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    private void Init(Entity entity, ElementalType element, float offset)
    {
        target = entity;
        myElement = element;
        currentOffset = offset;
        Color col = GetElementColor(element);
        col.a = 1f;
        tmpText.color = col;
    }

    private void Accumulate(float amount)
    {
        totalDamage += amount;
        fadeTimer = 0f;
        transform.localScale = Vector3.one;
        Refresh();
    }

    private void Refresh()
    {
        float preservedAlpha = tmpText.color.a;
        Color col = GetElementColor(myElement);
        col.a = preservedAlpha;
        tmpText.color = col;
        tmpText.text = $"<b>{totalDamage:F0}</b>";
        tmpText.fontSize = Mathf.Min(baseSize + totalDamage * sizeScale, maxSize);
    }

    private void Update()
    {
        // detect entity death the first frame target becomes null
        if (!detached && target == null)
        {
            detached = true;
            detachedPosition = transform.position;
            // remove from tracking so no new damage can be routed here
            // note: Unity null-destroyed objects retain a valid GetHashCode / C# reference,
            // so the dictionary lookup still works correctly here
            RemoveFromTracking();
        }

        if (!detached)
        {
            // follow target at its assigned stack slot
            float targetOffset = GetTargetOffset();
            currentOffset = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * offsetLerpSpeed);
            transform.position = target.transform.position + Vector3.up * currentOffset;
        }
        else
        {
            // entity is dead; stay at the last known position
            transform.position = detachedPosition;
        }

        // fade: starts immediately, quadratic ease-in, resets to 0 on each new tick
        fadeTimer += Time.deltaTime;
        float t = Mathf.Clamp01(fadeTimer / fadeDuration);
        float alpha = 1f - (t * t);

        Color c = tmpText.color;
        c.a = alpha;
        tmpText.color = c;
        transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, alpha);

        if (alpha <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Refresh();
    }

    private float GetTargetOffset()
    {
        if (!entityStacks.TryGetValue(target, out var stack)) return baseOffset;
        int idx = stack.IndexOf(this);
        return idx < 0 ? currentOffset : baseOffset + idx * stackSpacing;
    }

    private void RemoveFromTracking()
    {
        // uses C# reference equality for dict lookup, safe even on Unity-destroyed objects
        var key = (target, myElement);
        if (active.TryGetValue(key, out var stored) && stored == this)
            active.Remove(key);

        if (entityStacks.TryGetValue(target, out var stack))
        {
            stack.Remove(this);
            if (stack.Count == 0) entityStacks.Remove(target);
        }
    }

    private void OnDestroy()
    {
        // safety net: called when the aggregator itself is destroyed (scene unload, etc.)
        if (!detached && target != null)
            RemoveFromTracking();
    }

    // must match DamageIndicator exactly
    private static Color GetElementColor(ElementalType type)
    {
        return type switch
        {
            ElementalType.Fire    => new Color(1f, 0.4f, 0f),
            ElementalType.Water   => new Color(0.2f, 0.6f, 1f),
            ElementalType.Nature  => new Color(0.3f, 1f, 0.2f),
            ElementalType.Ice     => new Color(0f, 1f, 1f),
            ElementalType.Poison  => new Color(0.6f, 0.1f, 0.8f),
            ElementalType.Wind    => new Color(0.85f, 1f, 0.85f),
            _                     => new Color(0.9f, 0.9f, 0.9f),
        };
    }
}

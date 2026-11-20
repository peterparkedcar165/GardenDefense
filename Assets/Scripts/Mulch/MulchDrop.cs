using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// a mulch lying on the map, waiting to be clicked. it pops out where the insect died, hops up and
// lands a random distance to the side, always inside the map and never under the mulch inventory
// bar or any other on screen UI (a drop from a leaked insect hops back toward the map instead). it
// stays MulchRegistry.groundSeconds of real time (frozen while paused, never sped up), flashing
// near the end. clicking it with a full inventory leaves it where it is, flashing "Inventory full"
public class MulchDrop : MonoBehaviour
{
    private static readonly List<MulchDrop> all = new List<MulchDrop>();

    public MulchItem Item { get; private set; }

    private MulchRegistry registry;
    private SpriteRenderer body;
    private SpriteRenderer shadow;
    private Vector3 start, landing;
    private float age;
    private float fullFlash;
    private bool hovered;

    private const string SortingLayer = "Weather";
    private const int SortingOrder = 60;
    // in front of tiles, plants and insects, so a click lands on the drop first
    private const float DropZ = -1f;

    public static void ClearAll()
    {
        foreach (MulchDrop drop in new List<MulchDrop>(all))
            if (drop != null) Destroy(drop.gameObject);
        all.Clear();
        MulchTooltip.Hide();
    }

    public static MulchDrop Spawn(MulchItem item, Vector3 from, bool leaked)
    {
        if (item == null || item.data == null) return null;
        MulchRegistry registry = MulchManager.instance != null ? MulchManager.instance.Registry : null;
        if (registry == null) return null;

        GameObject go = new GameObject("MulchDrop");
        MulchDrop drop = go.AddComponent<MulchDrop>();
        drop.Init(item, registry, from, PickLanding(from, leaked, registry));
        MulchManager.OnDropSpawned();
        return drop;
    }

    private void Init(MulchItem item, MulchRegistry reg, Vector3 from, Vector3 to)
    {
        Item = item;
        registry = reg;
        from.z = DropZ;
        to.z = DropZ;
        start = from;
        landing = to;
        transform.position = from;

        Sprite sprite = item.data.icon != null ? item.data.icon : reg.placeholderIcon;
        Color tint = MulchTierColors.Get(item.rarity);

        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.SetParent(transform, false);
        shadowGO.transform.localPosition = new Vector3(0.06f, -0.06f, 0.01f);
        shadow = shadowGO.AddComponent<SpriteRenderer>();
        shadow.sprite = sprite;
        shadow.color = new Color(0f, 0f, 0f, 0.55f);
        shadow.sortingLayerName = SortingLayer;
        shadow.sortingOrder = SortingOrder - 1;

        body = gameObject.AddComponent<SpriteRenderer>();
        body.sprite = sprite;
        body.color = tint;
        body.sortingLayerName = SortingLayer;
        body.sortingOrder = SortingOrder;

        float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        float scale = spriteSize > 0f ? reg.dropSize / spriteSize : 1f;
        transform.localScale = new Vector3(scale, scale, 1f);

        CircleCollider2D hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.radius = spriteSize * 0.6f;
        hitbox.isTrigger = true;

        all.Add(this);
    }

    private void OnDestroy()
    {
        all.Remove(this);
        if (hovered) MulchTooltip.Hide();
    }

    private void Update()
    {
        float dt = MulchManager.DropDeltaTime;
        age += dt;

        float hop = registry.hopSeconds > 0f ? Mathf.Clamp01(age / registry.hopSeconds) : 1f;
        Vector3 position = Vector3.Lerp(start, landing, hop);
        position.y += registry.hopHeight * 4f * hop * (1f - hop);
        transform.position = position;

        float left = registry.groundSeconds - age;
        if (left <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        bool visible = true;
        if (fullFlash > 0f)
        {
            fullFlash -= dt;
            visible = Mathf.FloorToInt(fullFlash / 0.08f) % 2 == 0;
        }
        else if (left <= registry.flashSeconds)
        {
            // flashes faster the closer it gets to vanishing
            float period = Mathf.Lerp(0.07f, 0.2f, left / Mathf.Max(0.01f, registry.flashSeconds));
            visible = Mathf.FloorToInt(left / period) % 2 == 0;
        }
        body.enabled = visible;
        shadow.enabled = visible;
    }

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        if (MulchManager.instance == null) return;

        if (MulchManager.instance.TryCollect(Item))
        {
            if (GameManager.instance != null && GameManager.instance.plantSelect != null)
                GameManager.instance.PlaySound(GameManager.instance.plantSelect);
            Destroy(gameObject);
            return;
        }
        fullFlash = 0.64f;
        StatusIndicator.Spawn(transform.position + new Vector3(0f, 0.35f, 0f), "Inventory full", new Color(1f, 0.45f, 0.4f));
    }

    private void OnMouseEnter()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        hovered = true;
        MulchTooltip.Show(Item, null, "Click to pick it up.");
    }

    private void OnMouseExit()
    {
        if (!hovered) return;
        hovered = false;
        MulchTooltip.Hide();
    }

    // ── landing ─────────────────────────────────────────────────────────────

    private static Vector3 PickLanding(Vector3 from, bool leaked, MulchRegistry reg)
    {
        Bounds map = MapBounds();
        Vector3 origin = from;
        origin.z = 0f;
        if (leaked)
        {
            Vector3 toMap = map.center - origin;
            toMap.z = 0f;
            if (toMap.sqrMagnitude > 0.0001f) origin += toMap.normalized * reg.leakHopDistance;
        }

        // a random sideways spot first, then wider and wider circles, until one is clear
        for (int i = 0; i < 30; i++)
        {
            Vector3 candidate;
            if (i < 12)
            {
                float side = Random.Range(reg.sideOffsetMin, reg.sideOffsetMax) * (Random.value < 0.5f ? -1f : 1f);
                candidate = origin + new Vector3(side, Random.Range(-0.3f, 0.3f), 0f);
            }
            else
            {
                candidate = origin + (Vector3)(Random.insideUnitCircle * (1.2f + (i - 12) * 0.25f));
            }
            candidate = ClampInside(candidate, map);
            if (IsClear(candidate, reg)) return candidate;
        }

        // nothing nearby was clear: the closest clear tile
        Vector3 best = ClampInside(origin, map);
        float bestDistance = float.MaxValue;
        foreach (Tile tile in Tile.allTiles.Values)
        {
            if (tile == null) continue;
            Vector3 p = tile.transform.position;
            p.z = 0f;
            float distance = (p - origin).sqrMagnitude;
            if (distance >= bestDistance || !IsClear(p, reg)) continue;
            bestDistance = distance;
            best = p;
        }
        return best;
    }

    // the placement tiles' area, plus half a tile around it
    private static Bounds MapBounds()
    {
        bool any = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;
        foreach (Tile tile in Tile.allTiles.Values)
        {
            if (tile == null) continue;
            Vector3 p = tile.transform.position;
            if (!any) { min = max = p; any = true; continue; }
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        if (!any) return new Bounds(Vector3.zero, new Vector3(20f, 14f, 1f));
        Bounds bounds = new Bounds();
        bounds.SetMinMax(new Vector3(min.x - 0.5f, min.y - 0.5f, -1f), new Vector3(max.x + 0.5f, max.y + 0.5f, 1f));
        return bounds;
    }

    private static Vector3 ClampInside(Vector3 p, Bounds map)
    {
        const float margin = 0.35f;
        p.x = Mathf.Clamp(p.x, map.min.x + margin, map.max.x - margin);
        p.y = Mathf.Clamp(p.y, map.min.y + margin, map.max.y - margin);
        p.z = 0f;
        return p;
    }

    // on screen, and no UI over the drop's middle or any of its corners
    private static bool IsClear(Vector3 p, MulchRegistry reg)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 viewport = cam.WorldToViewportPoint(p);
        if (viewport.x < 0.02f || viewport.x > 0.98f || viewport.y < 0.02f || viewport.y > 0.98f) return false;

        float half = reg.dropSize * 0.6f;
        Vector3[] points =
        {
            p, p + new Vector3(half, half, 0f), p + new Vector3(-half, half, 0f),
            p + new Vector3(half, -half, 0f), p + new Vector3(-half, -half, 0f),
        };
        foreach (Vector3 point in points)
            if (UnderUI(cam.WorldToScreenPoint(point))) return false;
        return true;
    }

    private static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private static bool UnderUI(Vector2 screenPoint)
    {
        if (EventSystem.current == null) return false;
        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        // UI graphics only, never world colliders a physics raycaster might add
        foreach (RaycastResult hit in uiHits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
}

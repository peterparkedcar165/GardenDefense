using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Collections.Generic;

// a mulch lying on the map, waiting to be clicked. it pops out where the insect died, hops up and
// lands a random distance to the side, always inside the map and never under the mulch inventory
// bar or any other on screen UI (a drop from a leaked insect hops back toward the map instead). it
// stays MulchRegistry.groundSeconds of real time (frozen while paused, never sped up), flashing
// near the end. clicking it flies it into its inventory slot (MulchInventoryUI.FlyToSlot); with a
// full inventory it stays where it is, flashing "Inventory full". it can also be dragged straight
// onto a plant (or onto the inventory bar): the drop fades and stays where it lies while an opaque
// copy follows the pointer, and let go over nothing the copy glides back onto it (Tommy, 2026-11-20).
//
// built like insects and projectiles: this root is the spot on the ground, with a Shadow child that
// stays on the ground and a Visual child that rises off it during the hop. the visual is unlit, so
// it shows at full brightness in the dark. once it lands it sparkles with white glints (unlit too).
// it gives off no light (Tommy, 2026-11-20: being unlit already makes it show in the dark)
public class MulchDrop : MonoBehaviour
{
    private static readonly List<MulchDrop> all = new List<MulchDrop>();

    public MulchItem Item { get; private set; }

    private MulchRegistry registry;
    private Transform visual;
    // the paper bag icon's layers (MulchIcon): bag, label and symbol, one sorting group so drops
    // that overlap never interleave. label and symbol are null without the bag in the registry
    private SpriteRenderer body;
    private SpriteRenderer label;
    private SpriteRenderer symbol;
    private bool bagIcon;
    // the outline in the rarity's colour (Tommy, 2026-11-22): like a selected plant's (Entity's
    // highlight), silhouettes of the bag in a ring behind it, thinner, and always on. null: none
    private SpriteRenderer[] outline;
    private static Material silhouetteMaterial;
    private const int OutlineCopies = 8;
    private SpriteRenderer shadow;
    private Vector3 shadowBaseScale;
    private ParticleSystem glitter;
    private CircleCollider2D hitbox;
    private Vector3 start, landing;
    private float restHeight;
    private float age;
    private float fullFlash;
    // the fade in and out while it's about to expire: where in the wave it is, and the alpha it
    // gives every part (icon layers and shadow)
    private float expiryPhase;
    private float expiryAlpha = 1f;
    // seconds since it landed, on the drop's clock: it rises to its float and bobs (Tommy,
    // 2026-11-20: like a dropped item in minecraft, without the spin). visual only
    private float floatAge;
    private bool hovered;
    private bool landed;
    // pressed: the mouse went down on it. dragging: it then moved far enough to be a drag, not a click
    private bool pressed;
    private bool dragging;
    private bool faded;
    private Vector2 pressPosition;
    // any drop being dragged: other drops don't pop their tooltips while it passes over them
    private static MulchDrop draggedDrop;
    // how faded the drop is while its copy is being dragged
    private const float FadedAlpha = 0.35f;

    private const string DropLayer = "Weather";
    private const int SortingOrder = 60;
    // the shadow sits with the insects' shadows, under every plant and insect body
    private const string ShadowLayer = "Insects";
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
        // the drop stands on the ground: its middle sits this far above the root
        restHeight = reg.dropSize * 0.5f;

        BuildShadow();
        BuildVisual();
        BuildGlitter();

        hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.radius = reg.dropSize * 0.6f;
        hitbox.isTrigger = true;

        all.Add(this);
        PlaceParts(0f);
    }

    private void BuildShadow()
    {
        GameObject go = new GameObject("Shadow");
        go.transform.SetParent(transform, false);
        shadow = go.AddComponent<SpriteRenderer>();
        shadow.sprite = registry.shadowSprite;
        if (registry.shadowMaterial != null) shadow.sharedMaterial = registry.shadowMaterial;
        shadow.color = registry.shadowColor;
        shadow.sortingLayerName = ShadowLayer;
        shadow.sortingOrder = -1;
        Vector2 spriteSize = registry.shadowSprite != null ? (Vector2)registry.shadowSprite.bounds.size : Vector2.one;
        shadowBaseScale = new Vector3(
            registry.shadowSize.x / Mathf.Max(0.01f, spriteSize.x),
            registry.shadowSize.y / Mathf.Max(0.01f, spriteSize.y), 1f);
        go.transform.localScale = shadowBaseScale;
    }

    private void BuildVisual()
    {
        GameObject go = new GameObject("Visual");
        go.transform.SetParent(transform, false);
        visual = go.transform;
        SortingGroup group = go.AddComponent<SortingGroup>();
        group.sortingLayerName = DropLayer;
        group.sortingOrder = SortingOrder;
        bagIcon = registry.iconBag != null;
        Sprite sprite = bagIcon ? registry.iconBag : MulchIcon.OldIcon(Item);
        body = AddLayer(go, sprite, 0);
        if (bagIcon)
        {
            label = AddLayer(NewChild("Label"), registry.iconLabel, 1);
            Sprite symbolSprite = registry.SymbolSprite(Item.data.symbol);
            if (symbolSprite != null) symbol = AddLayer(NewChild("Symbol"), symbolSprite, 2);
        }
        float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        float scale = spriteSize > 0f ? registry.dropSize / spriteSize : 1f;
        visual.localScale = new Vector3(scale, scale, 1f);
        BuildOutline(sprite, scale);
        ApplyColors();
    }

    // eight silhouettes of the bag, each moved outlineWidth world units in another direction, under
    // the bag in its sorting group. the visual is scaled, so the offsets are divided by its scale
    private void BuildOutline(Sprite sprite, float scale)
    {
        if (sprite == null || registry.outlineWidth <= 0f) return;
        if (silhouetteMaterial == null)
        {
            Shader shader = Shader.Find("Custom/SpriteSilhouette");
            if (shader == null) return;
            silhouetteMaterial = new Material(shader);
        }
        float offset = registry.outlineWidth / Mathf.Max(0.0001f, scale);
        outline = new SpriteRenderer[OutlineCopies];
        for (int i = 0; i < OutlineCopies; i++)
        {
            float angle = i * (360f / OutlineCopies) * Mathf.Deg2Rad;
            GameObject go = NewChild("Outline");
            go.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * offset;
            SpriteRenderer copy = go.AddComponent<SpriteRenderer>();
            copy.sprite = sprite;
            copy.sharedMaterial = silhouetteMaterial;
            copy.sortingLayerName = DropLayer;
            copy.sortingOrder = -1;
            outline[i] = copy;
        }
    }

    // white glints around the drop, unlit so they shine in the dark. made in code like the ambient
    // fireflies, all numbers from the registry. not emitting until it lands
    private void BuildGlitter()
    {
        if (registry.glitterMaterial == null) return;
        GameObject go = new GameObject("Glitter");
        go.transform.SetParent(transform, false);
        glitter = go.AddComponent<ParticleSystem>();
        glitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = glitter.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(registry.glitterLifetime.x, registry.glitterLifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(registry.glitterSize.x, registry.glitterSize.y);
        main.startColor = registry.glitterColor;
        main.startRotation = 0f;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        // the drop's own clock: real time, so a game speed up doesn't speed the sparkle (paused in Update)
        main.useUnscaledTime = true;

        ParticleSystem.EmissionModule emission = glitter.emission;
        emission.rateOverTime = registry.glitterRate;

        ParticleSystem.ShapeModule shape = glitter.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = registry.glitterRadius;
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = glitter.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // each glint pops in, holds a moment and shrinks away: a twinkle
        ParticleSystem.SizeOverLifetimeModule size = glitter.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.6f, 0.8f), new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule color = glitter.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = registry.glitterMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingLayerName = DropLayer;
        renderer.sortingOrder = SortingOrder + 1;
    }

    private void OnDestroy()
    {
        all.Remove(this);
        if (hovered) MulchTooltip.Hide();
        if (dragging) MulchInventoryUI.instance?.EndGroundDrag();
        if (draggedDrop == this) draggedDrop = null;
    }

    private void Update()
    {
        float dt = MulchManager.DropDeltaTime;
        age += dt;

        float hop = registry.hopSeconds > 0f ? Mathf.Clamp01(age / registry.hopSeconds) : 1f;
        Vector3 ground = Vector3.Lerp(start, landing, hop);
        transform.position = ground;
        float height = registry.hopHeight * 4f * hop * (1f - hop);
        if (landed)
        {
            floatAge += dt;
            height += FloatOffset();
        }
        PlaceParts(height);
        if (!landed && hop >= 1f)
        {
            landed = true;
            if (glitter != null) glitter.Play();
        }

        // the glints keep the drop's clock: frozen while paused
        if (glitter != null && landed)
        {
            if (Time.timeScale <= 0f && !glitter.isPaused) glitter.Pause();
            else if (Time.timeScale > 0f && glitter.isPaused) glitter.Play();
        }

        float left = registry.groundSeconds - age;
        if (left <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        bool visible = true;
        expiryAlpha = 1f;
        if (fullFlash > 0f)
        {
            fullFlash -= dt;
            visible = Mathf.FloorToInt(fullFlash / 0.08f) % 2 == 0;
        }
        else if (left <= registry.flashSeconds)
        {
            // about to expire: fades smoothly out to nothing and back in (Tommy, 2026-11-20: it used
            // to blink), faster the closer it gets to vanishing: one fade out and in takes
            // flashCycleStart seconds when it starts and shrinks to flashCycleEnd at the very end
            float share = Mathf.Clamp01(left / Mathf.Max(0.01f, registry.flashSeconds));
            float cycle = Mathf.Max(0.02f, Mathf.Lerp(registry.flashCycleEnd, registry.flashCycleStart, share));
            expiryPhase += dt * 2f * Mathf.PI / cycle;
            expiryAlpha = 0.5f + 0.5f * Mathf.Cos(expiryPhase);
        }
        else expiryPhase = 0f;
        body.enabled = visible;
        if (label != null) label.enabled = visible;
        if (symbol != null) symbol.enabled = visible;
        if (outline != null)
            foreach (SpriteRenderer copy in outline) copy.enabled = visible;
        shadow.enabled = visible;
        ApplyColors();
    }

    private SpriteRenderer AddLayer(GameObject go, Sprite sprite, int order)
    {
        SpriteRenderer layer = go.AddComponent<SpriteRenderer>();
        layer.sprite = sprite;
        if (registry.spriteMaterial != null) layer.sharedMaterial = registry.spriteMaterial;
        layer.sortingLayerName = DropLayer;
        layer.sortingOrder = order;
        return layer;
    }

    private GameObject NewChild(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(visual, false);
        return go;
    }

    // bag brown, label in the rarity colour, symbol pale (MulchIcon); without the bag the old
    // icon tinted by rarity. faded while its copy is being dragged
    private void ApplyColors()
    {
        float alpha = (faded ? FadedAlpha : 1f) * expiryAlpha;
        body.color = Tint(bagIcon ? MulchIcon.BagColor : MulchTierColors.Get(Item.rarity), alpha);
        if (label != null) label.color = Tint(MulchIcon.LabelColor(Item.rarity), alpha);
        if (symbol != null) symbol.color = Tint(MulchIcon.SymbolColor(Item.rarity), alpha);
        if (outline != null)
        {
            Color rim = Tint(MulchTierColors.Get(Item.rarity), alpha);
            foreach (SpriteRenderer copy in outline) copy.color = rim;
        }
    }

    private static Color Tint(Color c, float alpha) { c.a = alpha; return c; }

    // how high the landed drop's picture floats: it eases up to floatHeight, bobbing bobHeight up and
    // down around it once every bobSeconds
    private float FloatOffset()
    {
        float rise = registry.floatRiseSeconds > 0f ? Mathf.Clamp01(floatAge / registry.floatRiseSeconds) : 1f;
        rise = rise * rise * (3f - 2f * rise);
        float bob = registry.bobSeconds > 0f ? Mathf.Sin(floatAge * 2f * Mathf.PI / registry.bobSeconds) : 0f;
        return rise * (registry.floatHeight + registry.bobHeight * bob);
    }

    // the visual rides height above its resting spot; the shadow stays on the ground and shrinks
    // and fades the higher the drop is, like a projectile's
    private void PlaceParts(float height)
    {
        Vector3 visualOffset = new Vector3(0f, restHeight + height, 0f);
        visual.localPosition = visualOffset;
        // the glints are born around the picture wherever it floats; once born they stay put
        if (glitter != null) glitter.transform.localPosition = visualOffset;
        if (hitbox != null) hitbox.offset = visualOffset;

        float lift = registry.hopHeight > 0f ? Mathf.Clamp01(height / registry.hopHeight) : 0f;
        shadow.transform.localPosition = new Vector3(0f, 0.02f, 0.01f);
        shadow.transform.localScale = shadowBaseScale * Mathf.Lerp(1f, 0.65f, lift);
        Color c = registry.shadowColor;
        c.a *= Mathf.Lerp(1f, 0.55f, lift) * (faded ? FadedAlpha : 1f) * expiryAlpha;
        shadow.color = c;
    }

    // a press decides on release: moved past the drag threshold, it was a drag; otherwise a click
    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        if (MulchManager.instance == null || Mouse.current == null) return;
        pressed = true;
        dragging = false;
        pressPosition = Mouse.current.position.ReadValue();
    }

    private void OnMouseDrag()
    {
        if (!pressed || Mouse.current == null) return;
        Vector2 pointer = Mouse.current.position.ReadValue();
        if (!dragging)
        {
            float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
            if ((pointer - pressPosition).sqrMagnitude < threshold * threshold) return;
            dragging = true;
            draggedDrop = this;
            faded = true;
            hovered = false;
            MulchTooltip.Hide();
            MulchInventoryUI.instance?.BeginGroundDrag(Item, pointer, visual.position, registry.dropSize);
        }
        MulchInventoryUI.instance?.MoveGroundDrag(pointer);
    }

    private void OnMouseUp()
    {
        if (!pressed) return;
        pressed = false;
        if (!dragging)
        {
            Collect(visual.position);
            return;
        }
        dragging = false;
        if (draggedDrop == this) draggedDrop = null;
        MulchInventoryUI ui = MulchInventoryUI.instance;
        Vector2 pointer = Mouse.current != null ? Mouse.current.position.ReadValue() : pressPosition;
        Plant plant = null;
        MulchInventoryUI.GroundDropTarget target = MulchInventoryUI.GroundDropTarget.Nothing;
        if (ui != null) target = ui.TargetUnder(pointer, out plant);

        if (target == MulchInventoryUI.GroundDropTarget.Plant && MulchManager.instance != null)
        {
            MulchManager.EquipResult result = MulchManager.instance.Equip(Item, plant);
            if (result == MulchManager.EquipResult.Equipped || result == MulchManager.EquipResult.Merged)
            {
                ui.EndGroundDrag();
                Destroy(gameObject);
                return;
            }
            MulchManager.ShowRefusal(result, plant);
        }
        else if (target == MulchInventoryUI.GroundDropTarget.Inventory && Camera.main != null)
        {
            Vector3 at = Camera.main.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -Camera.main.transform.position.z));
            at.z = visual.position.z;
            if (Collect(at))
            {
                ui.EndGroundDrag();
                return;
            }
        }

        // nothing took it: the dragged copy glides back and the drop shows again where it lies
        if (ui != null) ui.ReturnGroundDrag(visual.position, () => { if (this != null) faded = false; });
        else faded = false;
    }

    // into the first free inventory slot, flying there from `from`. false (and the drop stays,
    // flashing "Inventory full") when the inventory is full
    private bool Collect(Vector3 from)
    {
        if (MulchManager.instance == null) return false;
        int slot = MulchManager.instance.TryCollect(Item);
        if (slot >= 0)
        {
            if (GameManager.instance != null && GameManager.instance.plantSelect != null)
                GameManager.instance.PlaySound(GameManager.instance.plantSelect);
            MulchInventoryUI.instance?.FlyToSlot(Item, slot, from, registry.dropSize);
            Destroy(gameObject);
            return true;
        }
        fullFlash = 0.64f;
        StatusIndicator.Spawn(visual.position + new Vector3(0f, 0.35f, 0f), "Inventory full", new Color(1f, 0.45f, 0.4f));
        return false;
    }

    private void OnMouseEnter()
    {
        if (draggedDrop != null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        hovered = true;
        MulchTooltip.Show(Item, null, "Click to pick it up, or drag it onto a plant.");
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

    // on screen, and no UI over the drop (its resting spot sits half its size above the ground
    // point): its middle and corners are all checked
    private static bool IsClear(Vector3 ground, MulchRegistry reg)
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 p = ground + new Vector3(0f, reg.dropSize * 0.5f, 0f);
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

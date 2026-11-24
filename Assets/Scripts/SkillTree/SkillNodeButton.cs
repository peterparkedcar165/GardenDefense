using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;
using System.Text.RegularExpressions;

// one node in a plant's chain. spawned at runtime by SkillTreePlantPanel (one shared prefab for
// every plant), which passes in which slot of the tree this instance represents - so editing
// this prefab's look, or the panel's layout, changes every plant's tree at once instead of
// needing per-plant hand-duplicated copies
//
// left click stages a purchase (SkillTreeSession.TryStage); right click un-stages it if it
// hasn't been confirmed yet. a confirmed node no longer responds to right click at all
//
// look (Tommy's pixel kit, 2026-11-24): the plant's element vine skill frame (40 art px) round a 32 px
// window coloured by state, with an icon for what the node raises; a locked node is dimmed and vined
// over with a padlock, like the plant info panel's locked skill square. sizes and colours are in
// SkillTreeStyle; the node's place in the chain is the prefab's and the panel's, unchanged
public class SkillNodeButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Refs")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text rankText;

    private const float Px = 2f;
    // art pixels: the vine frame, its window, the biggest an icon is drawn (at a whole number scale)
    private const float FrameSize = 40f;
    private const float WindowSize = 32f;
    private const float IconMax = 24f;

    private SkillTreeUI ui;
    private SkillTreeData tree;
    private PlantData plant;
    private string plantName;
    private int stepIndex;
    private SkillTreeNode node;

    private GameObject lockCover;
    private Image vines;
    private Image padlock;
    private bool hovering;

    // called by SkillTreePlantPanel right after Instantiate, with the exact slot this instance
    // represents in the plant's tree
    public void Init(SkillTreeUI ui, SkillTreeData tree, PlantData plant, int stepIndex, int nodeIndexInStep)
    {
        this.ui = ui;
        this.tree = tree;
        this.plant = plant;
        plantName = plant.plantName;
        this.stepIndex = stepIndex;
        node = tree.steps[stepIndex].nodes[nodeIndexInStep];
        Skin();
    }

    // the panel's vine links read these
    public bool Locked => node != null && IsLocked(out _, out _);
    public bool Taken => node != null && SkillTreeSession.GetDisplayRank(plantName, node.id) > 0;
    public RectTransform Wrapper => (RectTransform)transform.parent;
    public static float HalfWidth => FrameSize * Px * 0.5f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (node == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!SkillTreeSession.TryStage(tree, plantName, stepIndex, node)) return;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!SkillTreeSession.TryUnstage(plantName, node.id)) return;
        }
        else return;

        Refresh();
        if (hovering) ui?.ShowTooltip(BuildTooltip(), plant.elementalType);
    }

    // a multi-rank node can already have some ranks confirmed while further ranks sit staged
    // on top - it must still read as "pending", not "confirmed", until those extra ranks are
    // actually confirmed too, or the button falsely looks auto-saved
    private bool IsLocked(out bool confirmed, out bool pendingPick)
    {
        pendingPick = SkillTreeSession.IsPending(plantName, node.id);
        confirmed = !pendingPick && SkillTreeSession.IsConfirmed(plantName, node.id);
        return !confirmed && !pendingPick &&
               (!SkillTreeSession.IsStepUnlockedDisplay(tree, plantName, stepIndex)
             || SkillTreeSession.IsExclusiveLockedDisplay(tree.steps[stepIndex], plantName, node));
    }

    private static string StripRichText(string text) => Regex.Replace(text, "<.*?>", "");

    public void Refresh()
    {
        if (node == null) return;
        SkillTreeStyle s = SkillTreeStyle.Instance;

        bool locked = IsLocked(out bool confirmed, out bool pendingPick);

        int displayRank = SkillTreeSession.GetDisplayRank(plantName, node.id);
        if (rankText != null)
        {
            rankText.text = $"{displayRank}/{node.maxRank}";
            rankText.color = confirmed ? s.rankConfirmed : pendingPick ? s.rankPending : locked ? s.rankLocked : s.rankAvailable;
        }

        if (background != null)
            background.color = confirmed ? s.windowConfirmed
                              : pendingPick ? s.windowPending
                              : locked ? s.windowLocked
                              : PlantData.ElementBackdropColor(plant.elementalType);

        if (lockCover != null) lockCover.SetActive(locked);
        ApplyHover();
    }

    // the frame, window, icon, rank and lock cover, made once on the prefab's own objects: the
    // window is the old square (it still takes the clicks, out to the frame's edge), the frame the old
    // outline, the rank the old rank text
    private void Skin()
    {
        SkillTreeStyle s = SkillTreeStyle.Instance;
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        RectTransform wrapper = Wrapper;

        if (background != null && s.white != null)
        {
            background.sprite = s.white;
            background.type = Image.Type.Simple;
            background.rectTransform.sizeDelta = new Vector2(WindowSize * Px, WindowSize * Px);
            float reach = (FrameSize - WindowSize) * 0.5f * Px;
            background.raycastPadding = new Vector4(-reach, -reach, -reach, -reach);

            Sprite iconSprite = IconFor(out Color tint);
            if (iconSprite != null)
            {
                // the biggest whole number scale that fits, so the icon's pixels stay square
                float longest = Mathf.Max(iconSprite.rect.width, iconSprite.rect.height);
                float scale = Mathf.Max(1f, Mathf.Floor(IconMax * Px / longest));
                RectTransform icon = NewImage("Icon", background.transform, iconSprite, tint);
                icon.sizeDelta = new Vector2(iconSprite.rect.width * scale, iconSprite.rect.height * scale);
            }
        }

        Transform outline = wrapper != null ? wrapper.Find("Outline") : null;
        Image frame = outline != null ? outline.GetComponent<Image>() : null;
        Sprite frameSprite = look.FrameFor(plant.elementalType).frame;
        if (frame != null && frameSprite != null)
        {
            frame.sprite = frameSprite;
            frame.type = Image.Type.Simple;
            frame.color = Color.white;
            frame.raycastTarget = false;
            frame.rectTransform.sizeDelta = new Vector2(FrameSize * Px, FrameSize * Px);
        }

        if (rankText != null)
        {
            rankText.fontSize = 20f;
            rankText.textWrappingMode = TextWrappingModes.NoWrap;
            rankText.overflowMode = TextOverflowModes.Overflow;
            rankText.raycastTarget = false;
            if (InsectShowcaseStyle.Instance.font != null) rankText.font = InsectShowcaseStyle.Instance.font;
            if (rankText.GetComponent<PixelTextEffect>() == null)
                rankText.gameObject.AddComponent<PixelTextEffect>().Color = s.outline;
        }

        // the lock cover, over the frame: black, the round 6 vines, the bigger padlock
        if (wrapper != null)
        {
            GameObject cover = new GameObject("LockCover", typeof(RectTransform));
            cover.layer = wrapper.gameObject.layer;
            cover.transform.SetParent(wrapper, false);
            cover.transform.SetAsLastSibling();
            NewImage("Dim", cover.transform, s.white, new Color(0f, 0f, 0f, s.lockedDim)).sizeDelta = new Vector2(FrameSize * Px, FrameSize * Px);
            RectTransform v = NewImage("Vines", cover.transform, s.vinesSquare, look.lockedVines);
            v.sizeDelta = new Vector2(FrameSize * Px, FrameSize * Px);
            vines = v.GetComponent<Image>();
            if (look.slotPadlock != null)
            {
                RectTransform p = NewImage("Padlock", cover.transform, look.slotPadlock, look.lockedText);
                p.sizeDelta = new Vector2(look.slotPadlock.rect.width * Px, look.slotPadlock.rect.height * Px);
                padlock = p.GetComponent<Image>();
            }
            lockCover = cover;
        }
    }

    // what the node raises, by its first effect: the stat icons, the sun for a cheaper plant, a path's
    // bloom pip in its colour for a path or its cooldown. anything else gets a pip in the element colour
    private Sprite IconFor(out Color tint)
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        tint = Color.white;
        if (node.effects == null || node.effects.Count == 0)
        {
            tint = PlantData.ElementPanelColor(plant.elementalType);
            return look.pipFilled;
        }
        switch (node.effects[0].statType)
        {
            case StatType.AttackDamageFlat:
            case StatType.AttackDamageTotalMultiplier:
            case StatType.FireDamage:
            case StatType.GrassDamage:
            case StatType.CriticalChance:
                return kit.attackDamage;
            case StatType.AttackSpeedFlat:
                return kit.attackSpeed;
            case StatType.AttackRange:
            case StatType.AttackRangeFlat:
                return SkillTreeStyle.Instance.attackRange;
            case StatType.Armor:
            case StatType.ArmorTotalMultiplier:
                return kit.armor;
            case StatType.MaxHealth:
            case StatType.HealingBonus:
            case StatType.HealAmountFlat:
                return kit.health;
            case StatType.SunCostReduction:
                tint = look.normal.sun;
                return look.sunGlyph;
            case StatType.Path1LevelAdder:
                tint = look.pathAttack;
                return look.pipFilled;
            case StatType.Path2LevelAdder:
            case StatType.PassiveCooldownFlat:
            case StatType.PassiveDurationMultiplier:
                tint = look.pathPassive;
                return look.pipFilled;
            case StatType.Path3LevelAdder:
            case StatType.SkillCooldownFlat:
            case StatType.SkillDurationAdder:
            case StatType.SkillChargeRate:
            case StatType.SkillDamage:
                tint = look.pathSkill;
                return look.pipFilled;
            default:
                tint = PlantData.ElementPanelColor(plant.elementalType);
                return look.pipFilled;
        }
    }

    // the lock cover brightens while the pointer is over the node, like the info panel's locked skill
    private void ApplyHover()
    {
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        if (vines != null) vines.color = hovering ? look.lockedVinesHover : look.lockedVines;
        if (padlock != null) padlock.color = hovering ? look.lockedTextHover : look.lockedText;
    }

    private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return r;
    }

    // name, description, and a cost line - matches the same things the in-level skill/passive
    // tooltip shows, nothing more
    private string BuildTooltip()
    {
        bool locked = IsLocked(out _, out _);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(locked
            ? $"<b><color=#9C9C9C>{StripRichText(node.nodeName)}</color></b>"
            : $"<b><color=#FFD84A>{node.nodeName}</color></b>");
        if (!string.IsNullOrEmpty(node.description))
            sb.AppendLine(locked ? $"<color=#B4B4B4>{StripRichText(node.description)}</color>" : node.description);
        sb.AppendLine();

        bool canAfford = SkillTreeSession.DisplaySkillPoints(plantName) >= node.costPerRank;
        string cost = canAfford ? $"<color=#5ED45E><b>{node.costPerRank}</b></color>" : $"<color=#FF7A66>{node.costPerRank}</color>";
        sb.AppendLine($"[COST]: {cost} point{(node.costPerRank == 1 ? "" : "s")}.");

        return sb.ToString().TrimEnd();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ApplyHover();
        ui?.ShowTooltip(BuildTooltip(), plant.elementalType);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyHover();
        ui?.HideTooltip();
    }
}

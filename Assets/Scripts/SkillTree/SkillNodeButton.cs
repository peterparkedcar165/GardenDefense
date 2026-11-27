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
    // art pixels: the skill frame (Tommy's gold and wood one, 2026-11-25; its dark window is 33), the
    // coloured window drawn in it, the biggest an icon is drawn (at a whole number scale), the locked
    // cover's vines (their own 40)
    private const float FrameSize = 46f;
    private const float WindowSize = 32f;
    // the frame's own dark window: a skill icon covers all of it (Tommy, 2026-11-27), over the coloured window
    private const float FrameWindowSize = 33f;
    private const float IconMax = 24f;
    private const float VinesSize = 40f;
    // canvas units: the rank's capitals start this far under the frame
    private const float RankGap = 4f;

    private SkillTreeUI ui;
    private SkillTreeData tree;
    private PlantData plant;
    private string plantName;
    private int stepIndex;
    private SkillTreeNode node;

    private GameObject lockCover;
    private Image vines;
    private Image padlock;
    // the icon, darkened while the node is locked (Tommy, 2026-11-25: like a locked plant slot,
    // PlantSlotLook.LockedTint; the icon only, the frame stays as it is), with its own colour to go back to
    private Image iconImage;
    private Color iconTint = Color.white;
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
        if (hovering) ShowTooltip();
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
        if (iconImage != null) iconImage.color = iconTint * (locked ? PlantSlotLook.LockedTint : Color.white);
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
                // a skill icon fills the frame's whole window, its picture as the node (Tommy, 2026-11-27)
                if (plant != null && iconSprite == plant.skillIcon)
                {
                    icon.sizeDelta = new Vector2(FrameWindowSize * Px, FrameWindowSize * Px);
                    icon.GetComponent<Image>().preserveAspect = false;
                }
                iconImage = icon.GetComponent<Image>();
                iconTint = tint;
            }
        }

        // the frame: the wood in the element's panel colour with the gold trim and its dark window on top,
        // both behind the coloured window and the icon (the old outline object, moved to the back)
        Transform outline = wrapper != null ? wrapper.Find("Outline") : null;
        Image frame = outline != null ? outline.GetComponent<Image>() : null;
        if (frame != null && look.skillFrameBase != null)
        {
            frame.sprite = look.skillFrameBase;
            frame.type = Image.Type.Simple;
            frame.preserveAspect = false;
            frame.color = PlantData.ElementPanelColor(plant.elementalType);
            frame.raycastTarget = false;
            frame.rectTransform.sizeDelta = new Vector2(FrameSize * Px, FrameSize * Px);
            outline.SetAsFirstSibling();
            if (look.skillFrameTrim != null && outline.Find("Trim") == null)
            {
                RectTransform trim = NewImage("Trim", outline, look.skillFrameTrim, Color.white);
                trim.anchorMin = Vector2.zero;
                trim.anchorMax = Vector2.one;
                trim.offsetMin = trim.offsetMax = Vector2.zero;
            }
        }

        if (rankText != null)
        {
            // under the bigger frame, a gap of RankGap to its capitals (top aligned: they start 6 under
            // the rect's top at the native size)
            RectTransform rank = rankText.rectTransform;
            float rectTop = -FrameSize * Px * 0.5f - RankGap + 6f;
            rank.anchoredPosition = new Vector2(rank.anchoredPosition.x, rectTop - rank.sizeDelta.y * (1f - rank.pivot.y));
            rankText.fontSize = 20f;
            rankText.textWrappingMode = TextWrappingModes.NoWrap;
            rankText.overflowMode = TextOverflowModes.Overflow;
            rankText.raycastTarget = false;
            if (InsectShowcaseStyle.Instance.font != null) rankText.font = InsectShowcaseStyle.Instance.font;
            if (rankText.GetComponent<PixelTextEffect>() == null)
                rankText.gameObject.AddComponent<PixelTextEffect>().Color = s.outline;
        }

        // the lock cover, over the frame: the round 6 vines and the padlock (the icon under it is darkened in
        // Refresh, like a locked plant slot, instead of a black square over the frame and icon)
        if (wrapper != null)
        {
            GameObject cover = new GameObject("LockCover", typeof(RectTransform));
            cover.layer = wrapper.gameObject.layer;
            cover.transform.SetParent(wrapper, false);
            cover.transform.SetAsLastSibling();
            RectTransform v = NewImage("Vines", cover.transform, s.vinesSquare, look.lockedVines);
            v.sizeDelta = new Vector2(VinesSize * Px, VinesSize * Px);
            vines = v.GetComponent<Image>();
            // Tommy's padlock (2026-11-25), as on the info panel's locked skill square
            Sprite lockSprite = look.Padlock;
            if (lockSprite != null)
            {
                RectTransform p = NewImage("Padlock", cover.transform, lockSprite, look.PadlockColor(false));
                p.sizeDelta = new Vector2(lockSprite.rect.width * Px, lockSprite.rect.height * Px);
                padlock = p.GetComponent<Image>();
            }
            lockCover = cover;
        }
    }

    // a node that changes the plant's skill shows the skill's own icon (Tommy, 2026-11-27): one flagged so in its tree
    // (modifiesSkill), the instant skill unlock, or one whose first effect is a skill stat (its points, cooldown,
    // duration, charge or damage)
    private bool ShowsSkillIcon
    {
        get
        {
            if (node.modifiesSkill) return true;
            if (!string.IsNullOrEmpty(node.unlockId) && node.unlockId.EndsWith("_instant_skill")) return true;
            if (node.effects == null || node.effects.Count == 0) return false;
            switch (node.effects[0].statType)
            {
                case StatType.Path3LevelAdder:
                case StatType.SkillCooldownFlat:
                case StatType.SkillDurationAdder:
                case StatType.SkillChargeRate:
                case StatType.SkillDamage:
                    return true;
                default:
                    return false;
            }
        }
    }

    // what the node raises, by its first effect: the stat icons, the sun for a cheaper plant, a path's
    // bloom pip in its colour for a path or its cooldown, the plant's skill icon for its skill. anything else gets a
    // pip in the element colour
    private Sprite IconFor(out Color tint)
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        tint = Color.white;
        if (ShowsSkillIcon && plant != null && plant.skillIcon != null) return plant.skillIcon;
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
        if (padlock != null) padlock.color = look.PadlockColor(hovering);
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

    // the name as the tooltip's title (bold, a seam under it), then the description and a cost line -
    // the same things the in-level skill/passive tooltip shows, nothing more
    private void ShowTooltip()
    {
        bool locked = IsLocked(out _, out _);
        string title = locked
            ? $"<b><color=#9C9C9C>{StripRichText(node.nodeName)}</color></b>"
            : $"<b><color=#FFD84A>{node.nodeName}</color></b>";

        StringBuilder sb = new StringBuilder();
        if (!string.IsNullOrEmpty(node.description))
        {
            sb.AppendLine(locked ? $"<color=#B4B4B4>{StripRichText(node.description)}</color>" : node.description);
            sb.AppendLine();
        }
        bool canAfford = SkillTreeSession.DisplaySkillPoints(plantName) >= node.costPerRank;
        string cost = canAfford ? $"<color=#5ED45E><b>{node.costPerRank}</b></color>" : $"<color=#FF7A66>{node.costPerRank}</color>";
        sb.AppendLine($"[COST]: {cost} point{(node.costPerRank == 1 ? "" : "s")}.");

        ui?.ShowTooltip(title, sb.ToString().TrimEnd(), plant.elementalType);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        ApplyHover();
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        ApplyHover();
        ui?.HideTooltip();
    }
}

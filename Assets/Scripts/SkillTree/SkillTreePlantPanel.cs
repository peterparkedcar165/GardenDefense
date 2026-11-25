using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// self-contained panel for one plant's skill tree: sprite, name, exp, level, and its node chain.
// the nodes are NOT hand-placed - this builds them at runtime from plantData.skillTree, spawning
// nodeButtonPrefab once per node, so the whole node chain (button look, spacing, fork layout)
// only ever needs editing in ONE place (this script's constants and the shared prefab) rather
// than being redone per plant. duplicating a plant's panel is then just swapping plantData
//
// the panel hides itself entirely unless the plant is both unlocked (in SaveData.unlockedPlants)
// and has an authored SkillTreeData assigned, so newly added plants simply appear once both are
// true, with no manual scene work needed per plant beyond duplicating this panel
//
// look (Tommy's pixel kit, 2026-11-24): a card in the plant's element colour behind the points, the
// portrait and the lines under it, the plant bar's bevel portrait, the points in a sunken box, a seam
// under the name, the exp on the kit's bar, and vine links from the card through every node. nothing
// here moves a node, a text or the portrait: it is all drawn round where the prefab and the layout put
// them. colours in SkillTreeStyle
public class SkillTreePlantPanel : MonoBehaviour
{
    [SerializeField] private PlantData plantData;

    [Header("Refs")]
    [SerializeField] private Image plantSprite;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text pointsText;

    [Header("Node Building")]
    [SerializeField] private Transform nodesContainer;
    // the prefab's root is now the SkillNode wrapper (SkillNode -> SkillNodeButton/Rank/Outline),
    // not the SkillNodeButton component itself - a GameObject reference here instantiates the
    // whole wrapper (and its siblings) instead of just the SkillNodeButton child's own subtree
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private float stepSpacing = 24f;
    [SerializeField] private float forkNodeSpacing = 12f;

    private readonly List<SkillNodeButton> nodeButtons = new List<SkillNodeButton>();
    // the node buttons step by step, for the vine links between them
    private readonly List<List<SkillNodeButton>> columns = new List<List<SkillNodeButton>>();

    // called by SkillTreeUI right after Instantiate, before this panel's own Start runs - lets a
    // spawner assign the plant instead of hand-setting it in the Inspector per duplicated panel
    public void Bind(PlantData data) => plantData = data;

    private void Start()
    {
        if (!ShouldShow())
        {
            gameObject.SetActive(false);
            return;
        }

        SkillTreeUI.instance?.RegisterPanel(this);

        if (plantSprite != null) plantSprite.sprite = plantData.icon;
        if (nameText != null) nameText.text = string.IsNullOrEmpty(plantData.displayName) ? plantData.plantName : plantData.displayName;

        BuildNodes();
        Skin();
        Refresh();
    }

    private bool ShouldShow()
    {
        if (plantData == null || plantData.skillTree == null) return false;
        if (nodeButtonPrefab == null || nodesContainer == null) return false;
        if (SaveManager.instance == null) return false;
        return SaveManager.instance.saveData.unlockedPlants.Contains(plantData.plantName);
    }

    // one column per step (a plain RectTransform with a VerticalLayoutGroup, so a fork's two
    // nodes stack instead of sitting side by side), each column holding that step's node
    // button(s). this is the only place any plant's node buttons get created
    private void BuildNodes()
    {
        nodeButtons.Clear();
        columns.Clear();
        SkillTreeData tree = plantData.skillTree;

        HorizontalLayoutGroup rowLayout = nodesContainer.GetComponent<HorizontalLayoutGroup>();
        if (rowLayout == null) rowLayout = nodesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = stepSpacing;
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // grows the row to fit the tallest column (a fork step) rather than clipping it or
        // leaving mismatched columns awkwardly off-center within a too-small fixed rect
        ContentSizeFitter rowFitter = nodesContainer.GetComponent<ContentSizeFitter>();
        if (rowFitter == null) rowFitter = nodesContainer.gameObject.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int stepIndex = 0; stepIndex < tree.steps.Count; stepIndex++)
        {
            SkillTreeStep step = tree.steps[stepIndex];
            List<SkillNodeButton> stepButtons = new List<SkillNodeButton>();
            columns.Add(stepButtons);

            GameObject column = new GameObject($"Step{stepIndex + 1}", typeof(RectTransform));
            column.transform.SetParent(nodesContainer, false);
            VerticalLayoutGroup colLayout = column.AddComponent<VerticalLayoutGroup>();
            colLayout.spacing = forkNodeSpacing;
            colLayout.childAlignment = TextAnchor.MiddleCenter;
            colLayout.childControlWidth = false;
            colLayout.childControlHeight = false;
            colLayout.childForceExpandWidth = false;
            colLayout.childForceExpandHeight = false;

            // without this the column keeps its default 100x100 RectTransform regardless of
            // whether it holds 1 or 2 nodes, so a fork's pair (which needs more height than a
            // single node) gets centered inside a box sized for one - throwing off the symmetry
            // between the top node's gap to the midline and the bottom node's gap to it
            ContentSizeFitter colFitter = column.AddComponent<ContentSizeFitter>();
            colFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            colFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int nodeIndex = 0; nodeIndex < step.nodes.Count; nodeIndex++)
            {
                GameObject nodeObj = Instantiate(nodeButtonPrefab, column.transform);
                SkillNodeButton button = nodeObj.GetComponentInChildren<SkillNodeButton>();
                button.Init(SkillTreeUI.instance, tree, plantData, stepIndex, nodeIndex);
                nodeButtons.Add(button);
                stepButtons.Add(button);
            }
        }

        // settled now, so the card and the links can measure where the nodes really are. it takes two
        // passes: the row spaces its columns by their current width before each column shrinks to its
        // node, so after one pass they're still spaced as unity's default 100 wide columns, which put the
        // first node about 115 units left of where it ends up (the card stopped mid portrait)
        RectTransform container = (RectTransform)nodesContainer;
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }

    public void Refresh()
    {
        if (plantData == null) return;
        SkillTreeStyle s = SkillTreeStyle.Instance;

        if (SaveManager.instance != null)
        {
            int level = SaveManager.instance.saveData.GetPlantLevel(plantData.plantName);
            int exp = SaveManager.instance.saveData.GetPlantExp(plantData.plantName);
            bool max = level >= PlantLevelCurve.MaxLevel;
            int next = max ? 0 : PlantLevelCurve.ExpForNextLevel(level);

            // the exp on the kit's bar: the number inside (without the word when it wouldn't fit), the
            // fill as far as the next level, full at the top level
            if (expText != null)
            {
                string numbers = max ? $"{exp}" : $"{exp}/{next}";
                string withWord = "Exp " + numbers;
                expText.text = expFill == null || expText.GetPreferredValues(withWord).x <= ExpBarWidth - 12f ? withWord : numbers;
            }
            if (expFill != null)
            {
                float share = max ? 1f : next > 0 ? Mathf.Clamp01((float)exp / next) : 0f;
                float width = Mathf.Round(expFillWidth * share / Px) * Px;
                expFill.rectTransform.sizeDelta = new Vector2(width, expFill.rectTransform.sizeDelta.y);
                expFill.enabled = width > 0f;
            }

            if (levelText != null)
                levelText.text = $"Level <color=#{ColorUtility.ToHtmlStringRGB(s.points)}>{level}</color>/{PlantLevelCurve.MaxLevel}";
        }

        if (pointsText != null)
        {
            int points = SkillTreeSession.DisplaySkillPoints(plantData.plantName);
            pointsText.text = points.ToString();
            pointsText.color = points > 0 ? s.points : s.mutedText;
        }

        foreach (SkillNodeButton button in nodeButtons)
            button.Refresh();

        // a link lights up once the node it leads into can be picked; lit ones are drawn last, so a
        // lit link covers a dark one where they meet
        foreach (Link link in links)
        {
            bool lit = link.lit();
            link.core.color = lit ? s.linkLit : s.linkDark;
            if (lit) link.core.transform.SetAsLastSibling();
        }
    }

    // ---- the look ----

    private const float Px = 2f;
    // canvas units: space round the card's contents, the exp bar (88 by 11 art px), the links' core
    // (2 art px) and with their outline (4)
    private const float CardPad = 10f;
    private const float ExpBarWidth = 176f;
    private const float ExpBarHeight = 22f;
    private const float LinkCore = 4f;
    private const float LinkOutline = 8f;
    // the shortest vine between the card and the first node (16 art px)
    private const float MinStem = 32f;

    private struct Link
    {
        public Image core;
        public System.Func<bool> lit;
    }

    private readonly List<Link> links = new List<Link>();
    private Image expFill;
    private float expFillWidth;

    private void Skin()
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        PlantInfoPanelStyle look = PlantInfoPanelStyle.Instance;
        SkillTreeStyle s = SkillTreeStyle.Instance;
        ElementalType element = plantData.elementalType;
        Color panel = PlantData.ElementPanelColor(element);
        Color shadow = PlantData.ElementTextShadowColor(element);
        RectTransform root = (RectTransform)transform;

        foreach (TMP_Text t in new[] { nameText, expText, levelText })
            if (t != null) SetText(t, 20f, kit.text);
        if (pointsText != null) SetText(pointsText, 40f, s.points);

        // the portrait: the plant bar's bevel frame on its backdrop, in the element's colours, exactly
        // where the plant's old square was
        Rect portrait = new Rect(-50f, -50f, 100f, 100f);
        if (plantSprite != null)
        {
            RectTransform p = plantSprite.rectTransform;
            Vector2 centre = root.InverseTransformPoint(p.TransformPoint(p.rect.center));
            portrait = new Rect(centre - p.rect.size * 0.5f, p.rect.size);
            plantSprite.enabled = false;
        }

        // the card's contents (points, portrait, name, exp and level) move up together so the card's
        // middle sits on the nodes' middle line (Tommy, 2026-11-24): the card runs from just above the
        // portrait to just under the level line
        float top = portrait.yMax + 8f;
        float bottom = levelText != null ? CapTop(levelText) - 7f * Px - 10f : portrait.yMin - 8f;
        RectTransform nodesRect = (RectTransform)nodesContainer;
        float nodesY = root.InverseTransformPoint(nodesRect.TransformPoint(nodesRect.rect.center)).y;
        float shift = Mathf.Round((nodesY - (top + bottom) * 0.5f) / Px) * Px;
        portrait.y += shift;
        top += shift;
        bottom += shift;
        foreach (TMP_Text t in new[] { nameText, expText, levelText, pointsText })
            if (t != null) t.rectTransform.anchoredPosition += new Vector2(0f, shift);

        // as wide on both sides of the portrait (Tommy): wide enough for the points box on its left, the
        // name and the exp bar under it
        Rect chip = default;
        bool hasChip = pointsText != null;
        float nameWidth = nameText != null ? nameText.GetPreferredValues(nameText.text).x : 0f;
        float half = Mathf.Max(portrait.width * 0.5f, nameWidth * 0.5f, ExpBarWidth * 0.5f) + CardPad;
        if (hasChip)
        {
            Vector2 at = pointsText.rectTransform.anchoredPosition;
            chip = new Rect(at.x - 38f, at.y - 38f, 76f, 76f);
            half = Mathf.Max(half, portrait.center.x - chip.xMin + CardPad);
        }
        half = Mathf.Ceil(half / Px) * Px;
        float left = portrait.center.x - half;
        float right = portrait.center.x + half;

        // the chain moves right when its first node would sit closer than a short vine to the card
        float firstNodeLeft = FirstNodeLeft(root);
        if (!float.IsNaN(firstNodeLeft) && firstNodeLeft < right + MinStem)
            nodesRect.anchoredPosition += new Vector2(Mathf.Ceil((right + MinStem - firstNodeLeft) / Px) * Px, 0f);
        RectTransform card = NewImage("Card", root, kit.panelBase, panel, true);
        Place(card, new Rect(left, bottom, right - left, top - bottom));
        card.SetSiblingIndex(0);
        Stretch(NewImage("Shine", card, kit.panelShine, Color.white, true));
        int next = 1;

        RectTransform backdrop = NewImage("PortraitBackdrop", root, look.slotBackdrop, PlantData.ElementBackdropColor(element), false);
        Place(backdrop, new Rect(portrait.center - portrait.size * (32f / 72f), portrait.size * (64f / 72f)));
        backdrop.SetSiblingIndex(next++);
        // the plant's still icon in the frame's window, which clips it (an idle animation here was tried
        // and dropped, Tommy 2026-11-24)
        GameObject windowObject = new GameObject("PortraitWindow", typeof(RectTransform), typeof(RectMask2D));
        windowObject.layer = root.gameObject.layer;
        RectTransform window = (RectTransform)windowObject.transform;
        window.SetParent(root, false);
        Place(window, new Rect(portrait.center - portrait.size * (32f / 72f), portrait.size * (64f / 72f)));
        window.SetSiblingIndex(next++);
        RectTransform plantImage = NewImage("PortraitPlant", window, plantData.icon, Color.white, false);
        Image plantPicture = plantImage.GetComponent<Image>();
        plantPicture.preserveAspect = true;
        Place(plantImage, new Rect(-portrait.size * 0.45f, portrait.size * 0.9f));
        RectTransform frame = NewImage("PortraitFrame", root, look.slotFrame, PlantData.ElementColor(element), false);
        Place(frame, portrait);
        frame.SetSiblingIndex(next++);

        // the points in a sunken box: the word Points, the number under it
        if (hasChip)
        {
            RectTransform box = NewImage("PointsBox", root, kit.stripSunken, panel, true);
            Place(box, chip);
            box.SetSiblingIndex(next++);
            TextMeshProUGUI word = NewText("PointsLabel", root, "Points", 20f, s.mutedText);
            PlaceText(word.rectTransform, chip.center.x, chip.yMax - 10f, 20f, chip.width);
            word.gameObject.AddComponent<PixelTextEffect>().Color = shadow;
            pointsText.alignment = TextAlignmentOptions.Top;
            PlaceText(pointsText.rectTransform, chip.center.x, chip.yMax - 36f, 40f, chip.width);
        }

        // a seam between the name and the exp bar
        if (nameText != null && expText != null)
        {
            float seamY = Mathf.Round(((CapTop(nameText) - 7f * Px - 4f) + (CapTop(expText) + 4f)) * 0.25f) * Px;
            RectTransform seam = NewImage("Seam", root, kit.seam, Color.white, false);
            Place(seam, new Rect(left + 8f, seamY - 2f, right - left - 16f, 4f));
            seam.SetSiblingIndex(next++);
        }

        // the exp on the kit's bar, the number inside it in white with a dark shadow
        if (expText != null)
        {
            Vector2 at = expText.rectTransform.anchoredPosition;
            float capTop = CapTop(expText);
            RectTransform bar = NewImage("ExpBar", root, kit.barFrame, Color.white, true);
            Place(bar, new Rect(at.x - ExpBarWidth * 0.5f, capTop + 4f - ExpBarHeight, ExpBarWidth, ExpBarHeight));
            bar.SetSiblingIndex(expText.transform.GetSiblingIndex());
            RectTransform fill = NewImage("Fill", bar, kit.barFill, s.expFill, false);
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0f, 0.5f);
            expFillWidth = ExpBarWidth - 2f * Px;
            fill.anchoredPosition = new Vector2(Px, 0f);
            fill.sizeDelta = new Vector2(expFillWidth, ExpBarHeight - 2f * Px);
            Stretch(NewImage("Shine", fill, kit.barFillShine, Color.white, false));
            expFill = fill.GetComponent<Image>();
            expText.color = Color.white;
            expText.gameObject.AddComponent<PixelTextEffect>().Color = new Color32(0x14, 0x14, 0x14, 0xFF);
        }
        if (nameText != null) nameText.gameObject.AddComponent<PixelTextEffect>().Color = shadow;
        if (levelText != null) levelText.gameObject.AddComponent<PixelTextEffect>().Color = shadow;
        if (pointsText != null) pointsText.gameObject.AddComponent<PixelTextEffect>().Color = shadow;

        BuildLinks(root, right);
    }

    // the vine links: from the card to the first node, then node to node; a fork's two nodes branch
    // off and join again halfway between the columns. every piece has a 1 px dark outline, all drawn
    // first, so the pieces join cleanly
    private void BuildLinks(RectTransform root, float cardRight)
    {
        links.Clear();
        if (columns.Count == 0) return;
        RectTransform container = (RectTransform)nodesContainer;

        RectTransform outlines = NewLayer("LinkOutlines", container);
        RectTransform cores = NewLayer("LinkCores", container);
        outlines.SetAsFirstSibling();
        cores.SetSiblingIndex(1);
        float edge = SkillNodeButton.HalfWidth;

        // the stem out of the card
        Vector2 cardEdge = container.InverseTransformPoint(root.TransformPoint(new Vector3(cardRight, 0f, 0f)));
        foreach (SkillNodeButton b in columns[0])
        {
            Vector2 at = Centre(container, b);
            SkillNodeButton target = b;
            AddPiece(outlines, cores, new Vector2(cardEdge.x, at.y), new Vector2(at.x - edge, at.y), () => !target.Locked);
        }

        for (int i = 0; i + 1 < columns.Count; i++)
        {
            List<SkillNodeButton> from = columns[i];
            List<SkillNodeButton> to = columns[i + 1];
            if (from.Count == 0 || to.Count == 0) continue;
            if (from.Count == 1 && to.Count == 1)
            {
                Vector2 a = Centre(container, from[0]);
                Vector2 b = Centre(container, to[0]);
                SkillNodeButton target = to[0];
                AddPiece(outlines, cores, new Vector2(a.x + edge, a.y), new Vector2(b.x - edge, b.y), () => !target.Locked);
                continue;
            }

            // branch or join halfway across the gap, on the row's middle line
            float midX = Mathf.Round((Centre(container, from[0]).x + edge + Centre(container, to[0]).x - edge) * 0.25f) * Px;
            List<SkillNodeButton> targets = to;
            System.Func<bool> anyOpen = () =>
            {
                foreach (SkillNodeButton t in targets) if (!t.Locked) return true;
                return false;
            };
            foreach (SkillNodeButton a in from)
            {
                Vector2 p = Centre(container, a);
                SkillNodeButton source = a;
                System.Func<bool> lit = from.Count == 1 ? anyOpen : () => source.Taken && anyOpen();
                AddPiece(outlines, cores, new Vector2(p.x + edge, p.y), new Vector2(midX, p.y), lit);
                if (Mathf.Abs(p.y) > 0.5f) AddPiece(outlines, cores, new Vector2(midX, p.y), new Vector2(midX, 0f), lit);
            }
            foreach (SkillNodeButton b in to)
            {
                Vector2 p = Centre(container, b);
                SkillNodeButton target = b;
                System.Func<bool> lit = () => !target.Locked;
                if (Mathf.Abs(p.y) > 0.5f) AddPiece(outlines, cores, new Vector2(midX, 0f), new Vector2(midX, p.y), lit);
                AddPiece(outlines, cores, new Vector2(midX, p.y), new Vector2(p.x - edge, p.y), lit);
            }
        }
    }

    // one straight piece of link (level or upright) from a to b, with its outline
    private void AddPiece(RectTransform outlines, RectTransform cores, Vector2 a, Vector2 b, System.Func<bool> lit)
    {
        SkillTreeStyle s = SkillTreeStyle.Instance;
        Segment(outlines, a, b, LinkOutline, s.outline);
        Image core = Segment(cores, a, b, LinkCore, s.linkDark).GetComponent<Image>();
        links.Add(new Link { core = core, lit = lit });
    }

    // both ends grown by half the thickness, so two pieces meeting at a corner close it
    private static RectTransform Segment(RectTransform parent, Vector2 a, Vector2 b, float thickness, Color color)
    {
        RectTransform r = NewImage("Link", parent, SkillTreeStyle.Instance.white, color, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        bool level = Mathf.Abs(a.y - b.y) < Mathf.Abs(a.x - b.x);
        float length = level ? Mathf.Abs(b.x - a.x) : Mathf.Abs(b.y - a.y);
        r.anchoredPosition = (a + b) * 0.5f;
        r.sizeDelta = level ? new Vector2(length + thickness, thickness) : new Vector2(thickness, length + thickness);
        return r;
    }

    private static Vector2 Centre(RectTransform container, SkillNodeButton button) =>
        container.InverseTransformPoint(button.Wrapper.position);

    // the left edge of the first node's frame, in the row's own space (NaN without nodes)
    private float FirstNodeLeft(RectTransform root)
    {
        if (columns.Count == 0 || columns[0].Count == 0) return float.NaN;
        Vector3 centre = root.InverseTransformPoint(columns[0][0].Wrapper.position);
        return centre.x - SkillNodeButton.HalfWidth;
    }

    // where a top aligned text's capitals start, in the row's space: 3 font px under its rect's top,
    // and they are 7 font px tall (1 font px = 2 canvas units at the native size 20)
    private static float CapTop(TMP_Text t)
    {
        RectTransform r = t.rectTransform;
        float rectTop = r.anchoredPosition.y + r.sizeDelta.y * (1f - r.pivot.y);
        return rectTop - 3f * Px * t.fontSize / 20f;
    }

    private static void SetText(TMP_Text t, float size, Color color)
    {
        InsectShowcaseStyle kit = InsectShowcaseStyle.Instance;
        if (kit.font != null) t.font = kit.font;
        t.fontSize = size;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }

    // a top aligned text whose capitals start at capTop, centred on x
    private static void PlaceText(RectTransform r, float x, float capTop, float size, float width)
    {
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(width, size * 1.4f);
        r.anchoredPosition = new Vector2(x, capTop + 3f * Px * size / 20f);
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string value, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        SetText(text, size, color);
        text.alignment = TextAlignmentOptions.Top;
        text.margin = Vector4.zero;
        text.text = value;
        return text;
    }

    private static RectTransform NewImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
        image.raycastTarget = false;
        return r;
    }

    // a layer the row's layout leaves alone, its origin on the container's centre
    private static RectTransform NewLayer(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = Vector2.zero;
        return r;
    }

    // a rect in the row's space (origin on the row's centre)
    private static void Place(RectTransform r, Rect rect)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = rect.center;
        r.sizeDelta = rect.size;
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

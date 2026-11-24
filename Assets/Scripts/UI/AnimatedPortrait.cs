using UnityEngine;
using UnityEngine.UI;

// plays a plant's idle animation on a UI image (Tommy, 2026-11-24, the skill tree's portraits): a
// hidden SpriteRenderer runs the plant's own body Animator controller (Plant.BodyAnimator; its default
// state is the idle), and every frame the image shows that renderer's current sprite. each frame is
// drawn at the same pixel scale as the plant's still icon in the box and pinned by its pivot, as the
// Visual draws it in a level, so the plant stands exactly where its icon did; whatever reaches past the
// portrait window is clipped by the window's mask. plants without a body animation keep the still icon
public class AnimatedPortrait : MonoBehaviour
{
    private Image image;
    private SpriteRenderer source;
    private Sprite shown;
    // canvas units per pixel of a sprite at the icon's pixels per unit, and where a frame's pivot goes
    private float unitsPerPixel;
    private float iconPpu;
    private Vector2 pivotPoint;

    // image: a child of the masked window, anchored on its centre. box: the area the still icon fills
    // (preserving its aspect), in the window's space. false (and nothing added) without an idle to play
    public static bool TryAttach(Image image, PlantData data, Rect box)
    {
        if (image == null || data == null || data.icon == null || data.plantPrefab == null) return false;
        Animator body = data.plantPrefab.BodyAnimator;
        RuntimeAnimatorController controller = body != null ? body.runtimeAnimatorController : null;
        if (controller == null) return false;

        AnimatedPortrait portrait = image.gameObject.AddComponent<AnimatedPortrait>();
        portrait.Setup(image, data, controller, box);
        return true;
    }

    private void Setup(Image target, PlantData data, RuntimeAnimatorController controller, Rect box)
    {
        image = target;
        image.preserveAspect = false;
        image.type = Image.Type.Simple;

        Sprite icon = data.icon;
        unitsPerPixel = Mathf.Min(box.width / icon.rect.width, box.height / icon.rect.height);
        iconPpu = icon.pixelsPerUnit;
        // the still icon would sit centred in the box at that scale; its pivot is where every frame's goes
        Vector2 iconSize = icon.rect.size * unitsPerPixel;
        pivotPoint = box.center - iconSize * 0.5f + icon.pivot * unitsPerPixel;

        // far below everything and see through: only its sprite is read
        GameObject go = new GameObject(data.plantName + " Idle (portrait)");
        go.transform.position = new Vector3(0f, -10000f, 0f);
        source = go.AddComponent<SpriteRenderer>();
        source.sprite = icon;
        source.color = Color.clear;
        Animator animator = go.AddComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = controller;

        Show(icon);
    }

    private void LateUpdate()
    {
        if (source != null && source.sprite != shown) Show(source.sprite);
    }

    private void Show(Sprite sprite)
    {
        shown = sprite;
        image.sprite = sprite;
        if (sprite == null) return;
        // a frame imported at another pixels per unit is as big in a level as its units say
        float scale = unitsPerPixel * iconPpu / sprite.pixelsPerUnit;
        RectTransform r = image.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
        r.sizeDelta = sprite.rect.size * scale;
        r.anchoredPosition = pivotPoint;
    }

    private void OnDestroy()
    {
        if (source != null) Destroy(source.gameObject);
    }
}

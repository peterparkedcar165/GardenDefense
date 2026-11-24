using UnityEngine;

// shared base for the six elemental Primers. also owns the square icon shown above the insect
// for as long as a Primer is active - since landing a second, different Primer always reacts
// immediately (removing both), an insect only ever carries one Primer at a time in practice, so
// a single icon slot is enough.
//
// OnApply/OnExpire are sealed here specifically so the icon can never go missing because a
// subclass's own override forgot to call base.OnApply()/base.OnExpire() - subclasses implement
// OnPrimerApply/OnPrimerExpire instead, which always run alongside the icon logic automatically
public abstract class ElementalDebuff : StatusEffect
{
    private static PrimerIconSet _iconSet;
    private static bool _iconSetLoaded;
    // the icon object's name per element and the Insects sorting layer, worked out once instead
    // of on every primer
    private static readonly System.Collections.Generic.Dictionary<ElementalType, string> _iconNames =
        new System.Collections.Generic.Dictionary<ElementalType, string>();
    private static int _insectsLayerId;
    private static bool _insectsLayerIdSet;

    private GameObject _icon;

    public ElementalDebuff(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.primer;
    }

    public sealed override void OnApply()
    {
        SpawnIcon();
        OnPrimerApply();

        // once the level 2 tutorial has been seen, TryShow would only turn it down a second later
        if (ProceduralLevel.CurrentConfig != null && ProceduralLevel.CurrentConfig.levelNumber == 2 && target != null
            && !(TutorialManager.instance != null && TutorialManager.instance.HasSeen(TutorialIds.Level2Primers)))
            target.StartCoroutine(DelayedPrimerTutorial());

        // Waterlily's first Water primer: her own primer tutorial (Tommy, Level3WaterPrimer)
        if (this is WaterPrimer && source is Plant plant && plant.data != null && plant.data.plantName == "Waterlily"
            && target != null && TutorialManager.instance != null && !TutorialManager.instance.HasSeen(TutorialIds.Level3WaterPrimer))
            target.StartCoroutine(DelayedWaterPrimerTutorial());
    }

    // 1 second after the primer lands, like the level 2 one, and only when no other tutorial is up
    // (a later primer tries again)
    private System.Collections.IEnumerator DelayedWaterPrimerTutorial()
    {
        yield return new UnityEngine.WaitForSeconds(1f);
        if (TutorialManager.instance == null || TutorialManager.instance.IsAnyTutorialActive) yield break;
        if (TutorialManager.instance.TryShow(TutorialIds.Level3WaterPrimer, TutorialContent.Level3WaterPrimer,
                TutorialSpeaker.Waterlily, () => GameManager.instance.SetPause(false)))
            GameManager.instance.SetPause(true);
    }

    // fires 1 second after the primer actually lands rather than instantly, so the player sees
    // the primer icon appear on the insect first before the tutorial popup covers the screen
    private System.Collections.IEnumerator DelayedPrimerTutorial()
    {
        yield return new UnityEngine.WaitForSeconds(1f);
        if (TutorialManager.instance != null
            && TutorialManager.instance.TryShow(TutorialIds.Level2Primers, TutorialContent.Level2Primers,
                TutorialSpeaker.AcornKnight, () => GameManager.instance.SetPause(false)))
        {
            GameManager.instance.SetPause(true);
        }
    }

    public sealed override void OnExpire()
    {
        if (_icon != null) Object.Destroy(_icon);
        OnPrimerExpire();
    }

    public sealed override void OnTick(float deltaTime) { }

    protected virtual void OnPrimerApply() {}
    protected virtual void OnPrimerExpire() {}

    private void SpawnIcon()
    {
        if (!(target is Insect insect)) return;

        if (!_iconSetLoaded)
        {
            _iconSet = Resources.Load<PrimerIconSet>("PrimerIconSet");
            _iconSetLoaded = true;
        }
        Sprite sprite = _iconSet != null ? _iconSet.Get(elementalType) : null;
        if (sprite == null) return;

        Transform parent = insect.visual != null ? insect.visual : insect.transform;
        if (!_iconNames.TryGetValue(elementalType, out string iconName))
        {
            iconName = "PrimerIcon_" + elementalType;
            _iconNames[elementalType] = iconName;
        }
        _icon = new GameObject(iconName);
        _icon.transform.SetParent(parent, false);
        _icon.transform.localPosition = new Vector3(0f, _iconSet.iconHeight, 0f);
        _icon.transform.localScale = Vector3.one * _iconSet.iconScale;

        SpriteRenderer sr = _icon.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (!_insectsLayerIdSet)
        {
            _insectsLayerId = SortingLayer.NameToID("Insects");
            _insectsLayerIdSet = true;
        }
        sr.sortingLayerID = _insectsLayerId;
        sr.sortingOrder = 10;
    }
}

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

    private GameObject _icon;

    public ElementalDebuff(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.primer;
    }

    public sealed override void OnApply()
    {
        SpawnIcon();
        OnPrimerApply();
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
        _icon = new GameObject("PrimerIcon_" + elementalType);
        _icon.transform.SetParent(parent, false);
        _icon.transform.localPosition = new Vector3(0f, _iconSet.iconHeight, 0f);
        _icon.transform.localScale = Vector3.one * _iconSet.iconScale;

        SpriteRenderer sr = _icon.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = SortingLayer.NameToID("Insects");
        sr.sortingOrder = 10;
    }
}

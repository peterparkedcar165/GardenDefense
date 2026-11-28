using UnityEngine;
using System.Collections;

public class LightFader : MonoBehaviour
{
    private UnityEngine.Rendering.Universal.Light2D _light;
    private float _targetIntensity;
    private GameObject _crossFadeClone;

    public void Setup(UnityEngine.Rendering.Universal.Light2D light, float targetIntensity)
    {
        _light = light;
        _targetIntensity = targetIntensity;
        _light.intensity = 0f;
    }

    public void SetupForFadeOut(UnityEngine.Rendering.Universal.Light2D light)
    {
        _light = light;
    }

    // any in-flight cross-fade's clone light only gets destroyed at the end of its coroutine,
    // so if it gets interrupted mid-flight by another fade call, the clone would otherwise be
    // orphaned permanently, stacking extra light on top of the real one
    private void CancelCrossFadeClone()
    {
        if (_crossFadeClone != null) Destroy(_crossFadeClone);
        _crossFadeClone = null;
    }

    public void FadeIn(float duration = 0.3f)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        CancelCrossFadeClone();
        StartCoroutine(FadeCoroutine(_light.intensity, _targetIntensity, duration));
    }

    public void FadeOut(float duration = 0.5f, bool destroyOnComplete = false)
    {
        StopAllCoroutines();
        CancelCrossFadeClone();
        StartCoroutine(FadeOutCoroutine(duration, destroyOnComplete));
    }

    // resizing an already-lit light: a temporary clone fades out at the old radius while the
    // main light (already switched to the new radius) fades in, both running at the same time
    public void CrossFadeTo(float outerRadius, float innerRadius, float duration = 1f)
    {
        StopAllCoroutines();
        CancelCrossFadeClone();
        // a light that isn't showing (a plant's light on a day level, or on a dark level while the
        // sun has the lights off) just takes its new size: the clone is a new, switched on light,
        // so it would flash the old radius for the length of the fade where no light should be
        if (!_light.enabled || !_light.gameObject.activeInHierarchy)
        {
            _light.pointLightOuterRadius = outerRadius;
            _light.pointLightInnerRadius = innerRadius;
            _light.intensity = _targetIntensity;
            return;
        }
        StartCoroutine(CrossFadeCoroutine(outerRadius, innerRadius, duration));
    }

    private IEnumerator FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _light.intensity = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _light.intensity = to;
    }

    private IEnumerator FadeOutCoroutine(float duration, bool destroyOnComplete)
    {
        float start = _light.intensity;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _light.intensity = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }
        _light.intensity = 0f;
        if (destroyOnComplete)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private IEnumerator CrossFadeCoroutine(float outerRadius, float innerRadius, float duration)
    {
        // clone the light exactly as it currently looks, so it can fade out at the old radius
        // while the real light (below) simultaneously fades in at the new one
        GameObject cloneObj = new GameObject("LightFadeClone");
        _crossFadeClone = cloneObj;
        cloneObj.transform.SetParent(_light.transform.parent);
        cloneObj.transform.position = _light.transform.position;

        // same place, size and turn as the light, so a drawn light's picture (ExactLight) lands on the same pixels
        cloneObj.transform.localScale = _light.transform.localScale;
        cloneObj.transform.localRotation = _light.transform.localRotation;

        var clone = cloneObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        clone.lightType = _light.lightType;
        clone.lightCookieSprite = _light.lightCookieSprite;
        clone.shadowsEnabled = _light.shadowsEnabled;
        clone.color = _light.color;
        clone.falloffIntensity = _light.falloffIntensity;
        clone.targetSortingLayers = _light.targetSortingLayers;
        clone.pointLightOuterRadius = _light.pointLightOuterRadius;
        clone.pointLightInnerRadius = _light.pointLightInnerRadius;
        float startIntensity = _light.intensity;
        clone.intensity = startIntensity;

        // a drawn light's obstacle art and body lights (ExactLight) fade out at the old size with it
        ExactLight exact = _light.GetComponent<ExactLight>();
        UnityEngine.Rendering.Universal.Light2D artClone = exact != null ? CloneExtra(exact.ArtLight, "ArtFadeClone", cloneObj.transform, startIntensity) : null;
        UnityEngine.Rendering.Universal.Light2D bodyClone = exact != null ? CloneExtra(exact.BodyLight, "BodyFadeClone", cloneObj.transform, startIntensity) : null;

        _light.pointLightOuterRadius = outerRadius;
        _light.pointLightInnerRadius = innerRadius;
        _light.intensity = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            clone.intensity = Mathf.Lerp(startIntensity, 0f, t);
            if (artClone != null) artClone.intensity = clone.intensity;
            if (bodyClone != null) bodyClone.intensity = clone.intensity;
            _light.intensity = Mathf.Lerp(0f, _targetIntensity, t);
            yield return null;
        }

        Destroy(cloneObj);
        _crossFadeClone = null;
        _light.intensity = _targetIntensity;
    }

    // a copy of one of a drawn light's own sprite lights as it looks now, under the cross fade's copy
    private static UnityEngine.Rendering.Universal.Light2D CloneExtra(UnityEngine.Rendering.Universal.Light2D source, string name, Transform parent, float intensity)
    {
        if (source == null || source.lightCookieSprite == null) return null;
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var copy = obj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        copy.lightType = source.lightType;
        copy.lightCookieSprite = source.lightCookieSprite;
        copy.shadowsEnabled = false;
        copy.color = source.color;
        copy.targetSortingLayers = source.targetSortingLayers;
        copy.intensity = intensity;
        return copy;
    }
}

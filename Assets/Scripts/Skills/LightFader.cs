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

        var clone = cloneObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        clone.lightType = _light.lightType;
        clone.color = _light.color;
        clone.falloffIntensity = _light.falloffIntensity;
        clone.targetSortingLayers = _light.targetSortingLayers;
        clone.pointLightOuterRadius = _light.pointLightOuterRadius;
        clone.pointLightInnerRadius = _light.pointLightInnerRadius;
        float startIntensity = _light.intensity;
        clone.intensity = startIntensity;

        _light.pointLightOuterRadius = outerRadius;
        _light.pointLightInnerRadius = innerRadius;
        _light.intensity = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            clone.intensity = Mathf.Lerp(startIntensity, 0f, t);
            _light.intensity = Mathf.Lerp(0f, _targetIntensity, t);
            yield return null;
        }

        Destroy(cloneObj);
        _crossFadeClone = null;
        _light.intensity = _targetIntensity;
    }
}

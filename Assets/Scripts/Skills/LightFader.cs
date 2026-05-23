using UnityEngine;
using System.Collections;

public class LightFader : MonoBehaviour
{
    private UnityEngine.Rendering.Universal.Light2D _light;
    private float _targetIntensity;

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

    public void FadeIn(float duration = 0.3f)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeCoroutine(_light.intensity, _targetIntensity, duration));
    }

    public void FadeOut(float duration = 0.5f, bool destroyOnComplete = false)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutCoroutine(duration, destroyOnComplete));
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
}

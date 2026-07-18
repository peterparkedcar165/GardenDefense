using UnityEngine;
using System.Collections;

// fades all child sprite renderers in, holds, fades out, then destroys the object.
// added at runtime by effects that want a soft appearance, like the earth pillars
public class SpriteFadeInOut : MonoBehaviour
{
    private SpriteRenderer[] renderers;
    private float[] baseAlphas;

    public void Play(float fadeIn, float hold, float fadeOut)
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseAlphas = new float[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseAlphas[i] = renderers[i].color.a;

        StartCoroutine(FadeRoutine(fadeIn, hold, fadeOut));
    }

    private IEnumerator FadeRoutine(float fadeIn, float hold, float fadeOut)
    {
        for (float t = 0f; t < fadeIn; t += Time.deltaTime)
        {
            SetAlpha(t / fadeIn);
            yield return null;
        }
        SetAlpha(1f);

        yield return new WaitForSeconds(hold);

        for (float t = 0f; t < fadeOut; t += Time.deltaTime)
        {
            SetAlpha(1f - t / fadeOut);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void SetAlpha(float factor)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = renderers[i].color;
            c.a = baseAlphas[i] * factor;
            renderers[i].color = c;
        }
    }
}

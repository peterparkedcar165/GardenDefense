using UnityEngine;

/// <summary>
/// Attach to a GameObject with a SpriteRenderer (the arrow sprite).
/// The arrow is visible and pulses during wave 0 (before the first wave starts).
/// It fades out and hides itself permanently once wave 1 begins.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class WaveStartArrow : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Minimum alpha while pulsing (0 = fully transparent).")]
    [SerializeField] private float minAlpha = 0f;

    [Tooltip("Maximum alpha while pulsing (1 = fully opaque).")]
    [SerializeField] private float maxAlpha = 1f;

    [Tooltip("How many full pulses (in → out) per second.")]
    [SerializeField] private float pulseSpeed = 1.5f;

    [Header("Fade-Out on Wave Start")]
    [Tooltip("How quickly the arrow fades out once wave 1 begins.")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    private SpriteRenderer sr;
    private bool fadingOut = false;
    private float fadeOutTimer = 0f;
    private float fadeStartAlpha = 1f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (GameManager.instance == null) return;

        // Once wave 1 has started, fade out then disable.
        if (GameManager.instance.currentWave >= 1)
        {
            if (!fadingOut)
            {
                fadingOut      = true;
                fadeOutTimer   = 0f;
                fadeStartAlpha = sr.color.a;
            }

            fadeOutTimer += Time.deltaTime;
            float t     = Mathf.Clamp01(fadeOutTimer / fadeOutDuration);
            SetAlpha(Mathf.Lerp(fadeStartAlpha, 0f, t));

            if (t >= 1f)
                gameObject.SetActive(false);

            return;
        }

        // Wave 0: pulse between minAlpha and maxAlpha using a sine wave.
        float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f; // 0 → 1
        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, pulse));
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a     = a;
        sr.color = c;
    }
}

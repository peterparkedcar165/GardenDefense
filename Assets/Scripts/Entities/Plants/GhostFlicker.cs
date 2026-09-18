using UnityEngine;

// pulses a dead-plant ghost's sprite alpha between two bounds while it's shown (Rhodiola's revive
// preview), so a revivable tile reads as flashing rather than sitting at a flat low opacity
public class GhostFlicker : MonoBehaviour
{
    private const float MinAlpha = 0.2f;
    private const float MaxAlpha = 0.5f;
    private const float Speed = 3f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sr == null) return;
        float t = (Mathf.Sin(Time.time * Speed) + 1f) * 0.5f;
        Color c = sr.color;
        c.a = Mathf.Lerp(MinAlpha, MaxAlpha, t);
        sr.color = c;
    }
}

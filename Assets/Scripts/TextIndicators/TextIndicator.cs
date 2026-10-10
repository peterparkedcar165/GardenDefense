using UnityEngine;
using TMPro;

// base class for all floating text indicators
// handles animation: drift, fade, optional shrink
// children implement Initialize() with their own parameters
public abstract class TextIndicator : MonoBehaviour
{
    protected TMP_Text tmpText;
    // optional icon sitting next to the text (e.g. SunIndicator's sun icon); fades out with the text if assigned
    [SerializeField] private SpriteRenderer icon;
    protected float horizontalDrift;
    protected float verticalSpeed;
    protected bool shrink = false;
    // downward acceleration applied to verticalSpeed every frame - 0 by default (pure constant
    // upward drift, existing behavior for every indicator that doesn't opt in), set by a
    // subclass (e.g. DamageIndicator) for a rise-then-fall arc instead
    protected float gravity = 0f;
    protected float lifetime = 0.6f;

    // overridable initial upward speed range, rolled once in Awake - a subclass (e.g.
    // DamageIndicator) can give itself a faster launch than the default float-up pace
    protected virtual float MinVerticalSpeed => 0.25f;
    protected virtual float MaxVerticalSpeed => 0.5f;

    protected virtual void Awake()
    {
        tmpText = GetComponent<TMP_Text>();

        // the spawn jitter decides which side it lands on, and horizontalDrift always pushes
        // further that same way - so this never drifts back the opposite way across center
        float spawnOffsetX = Random.Range(-0.25f, 0.25f);
        transform.position += new Vector3(spawnOffsetX, 0f, 0f);
        horizontalDrift = Mathf.Sign(spawnOffsetX) * Random.Range(0f, 0.25f);

        verticalSpeed = Random.Range(MinVerticalSpeed, MaxVerticalSpeed);
    }

    protected virtual void Update()
    {
        verticalSpeed -= gravity * Time.deltaTime;
        transform.position += new Vector3(horizontalDrift, verticalSpeed, 0f) * Time.deltaTime;
        Color c = tmpText.color;
        c.a -= (1f / lifetime) * Time.deltaTime;
        tmpText.color = c;
        if (icon != null)
        {
            Color iconColor = icon.color;
            iconColor.a = c.a;
            icon.color = iconColor;
        }
        if (shrink) transform.localScale = Vector3.one * c.a;
        if (c.a <= 0f) Destroy(gameObject);
    }

    // places the icon right after the text's current rendered width, so it tracks strings of any length
    // (e.g. "+9" vs "+102") instead of sitting at a fixed offset. call once, after the final text is set
    protected void PositionIconAfterText(float gap)
    {
        if (icon == null) return;
        tmpText.ForceMeshUpdate();
        float halfWidth = tmpText.textBounds.extents.x;
        Vector3 pos = icon.transform.localPosition;
        pos.x = halfWidth + gap;
        icon.transform.localPosition = pos;
    }
}

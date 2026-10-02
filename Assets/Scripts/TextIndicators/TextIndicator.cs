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
    private float verticalSpeed;
    protected bool shrink = false;
    private const float Lifetime = 0.6f;

    protected virtual void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        horizontalDrift = Random.Range(-0.5f, 0.5f);
        verticalSpeed   = Random.Range(0.25f, 0.5f);
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
    }

    protected virtual void Update()
    {
        transform.position += new Vector3(horizontalDrift, verticalSpeed, 0f) * Time.deltaTime;
        Color c = tmpText.color;
        c.a -= (1f / Lifetime) * Time.deltaTime;
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

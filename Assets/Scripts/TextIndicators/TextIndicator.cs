using UnityEngine;
using TMPro;

// base class for all floating text indicators
// handles animation: drift, fade, optional shrink
// children implement Initialize() with their own parameters
public abstract class TextIndicator : MonoBehaviour
{
    protected TMP_Text tmpText;
    protected float horizontalDrift;
    private float verticalSpeed;
    protected bool shrink = false;

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
        c.a -= 1f * Time.deltaTime;
        tmpText.color = c;
        if (shrink) transform.localScale = Vector3.one * c.a;
        if (c.a <= 0f) Destroy(gameObject);
    }
}

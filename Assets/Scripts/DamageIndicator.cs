using UnityEngine;
using TMPro;

public class DamageIndicator : MonoBehaviour
{
    Color color;
    private TMP_Text tmpText;
    private float horizontalDrift;
    private float verticalSpeed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        horizontalDrift = Random.Range(-0.5f, 0.5f);
        verticalSpeed = Random.Range(0.25f, 0.5f);
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += new Vector3(horizontalDrift, verticalSpeed, 0f) * Time.deltaTime;
        Color c = tmpText.color;
        c.a -= 1f *Time.deltaTime;
        tmpText.color = c;

        if (c.a <= 0f)
        {
            Destroy(gameObject);
        }
    }

    // for damage
    public void Initialize(float damage, ElementalType elementalType, bool isCrit)
    {
        if (damage <= 0.5f)
        {
            Destroy(gameObject);
            return;
        }

        switch (elementalType)
        {
            case(ElementalType.Fire):
            color = new Color(1f, 0.4f, 0f);
            break;
            case(ElementalType.Water):
            color = new Color(0.2f, 0.6f, 1f);
            break;
            case(ElementalType.Nature):
            color = new Color(0.3f, 1f, 0.2f);
            break;
            case(ElementalType.Ice):
            color = new Color(0f, 1f, 1f);
            break;
            case(ElementalType.Poison):
            color = new Color(0.6f, 0.1f, 0.8f);
            break;
            case(ElementalType.Wind):
            color = new Color(0.85f, 1f, 0.85f);
            break;
            case(ElementalType.Neutral):
            color = new Color(0.9f, 0.9f, 0.9f);
            break;
        }

        tmpText.fontSize = Mathf.Clamp(2.5f + damage * 0.025f, 2.5f, 5.5f);

        if (isCrit)
        {
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.fontSize *= 1.5f;
        } else
        {
            tmpText.fontStyle = FontStyles.Normal;
        }

        tmpText.color = color;
        int rounded = Mathf.RoundToInt(damage);

        tmpText.text = rounded.ToString();
        // Debug.Log("Damage shown: " + rounded + " at " + transform.position);

    }

    // elemental reactions
    public void Initialize(string text, Color color)
    {
        tmpText.color = color;
        tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmpText.text = text;
    }
}

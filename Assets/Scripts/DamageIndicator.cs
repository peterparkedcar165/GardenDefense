using UnityEngine;
using TMPro;

public class DamageIndicator : MonoBehaviour
{
    Color color;
    private TMP_Text tmpText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += Vector3.up * 0.1f * Time.deltaTime;
        Color c = tmpText.color;
        c.a -= 1f *Time.deltaTime;
        tmpText.color = c;

        if (c.a <= 0f)
        {
            Destroy(gameObject);
        }
    }

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
            color = new Color(0.6f, 0.95f, 1f);
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

        if (isCrit)
        {
            tmpText.fontStyle = FontStyles.Bold;
        } else
        {
            tmpText.fontStyle = FontStyles.Normal;
        }

        tmpText.color = color;
        int rounded = Mathf.RoundToInt(damage);

        tmpText.text = rounded.ToString();
        Debug.Log("Damage shown: " + rounded + " at " + transform.position);

    }
}

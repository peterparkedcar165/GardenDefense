using UnityEngine;
using TMPro;

public class DamageIndicator : TextIndicator
{
    public static void Spawn(Vector3 position, float damage, ElementalType elementalType, bool isCrit)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("DamageIndicator"), position, Quaternion.identity);
        go.GetComponent<DamageIndicator>()?.Initialize(damage, elementalType, isCrit);
    }

    public void Initialize(float damage, ElementalType elementalType, bool isCrit)
    {
        shrink = false;
        horizontalDrift = 0f;

        if (damage <= 0.5f) { Destroy(gameObject); return; }

        Color color;
        switch (elementalType)
        {
            case ElementalType.Fire:    color = new Color(1f, 0.4f, 0f);    break;
            case ElementalType.Water:   color = new Color(0.2f, 0.6f, 1f);  break;
            case ElementalType.Grass:  color = new Color(0.3f, 1f, 0.2f);  break;
            case ElementalType.Ice:     color = new Color(0f, 1f, 1f);      break;
            case ElementalType.Poison:  color = new Color(0.6f, 0.1f, 0.8f); break;
            case ElementalType.Wind:    color = new Color(0.85f, 1f, 0.85f); break;
            case ElementalType.Ground:  color = new Color(0.475f, 0.224f, 0.122f); break;
            default:                    color = new Color(0.9f, 0.9f, 0.9f); break;
        }

        // size starts at 6, damage scale with number, then clamp min, clamp max
        tmpText.fontSize = Mathf.Clamp(6f + damage * 0.00f, 5f, 8f);

        if (isCrit)
        {
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.fontSize *= 1.05f;
        }
        else
        {
            tmpText.fontStyle = FontStyles.Normal;
        }

        tmpText.color = color;
        int rounded = Mathf.RoundToInt(damage);
        tmpText.text = isCrit ? rounded + "!" : rounded.ToString();
    }
}

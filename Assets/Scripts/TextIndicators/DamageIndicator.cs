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
        shrink = true;

        if (damage <= 0.5f) { Destroy(gameObject); return; }

        Color color;
        switch (elementalType)
        {
            case ElementalType.Fire:    color = new Color(1f, 0.4f, 0f);    break;
            case ElementalType.Water:   color = new Color(0.2f, 0.6f, 1f);  break;
            case ElementalType.Nature:  color = new Color(0.3f, 1f, 0.2f);  break;
            case ElementalType.Ice:     color = new Color(0f, 1f, 1f);      break;
            case ElementalType.Poison:  color = new Color(0.6f, 0.1f, 0.8f); break;
            case ElementalType.Wind:    color = new Color(0.85f, 1f, 0.85f); break;
            default:                    color = new Color(0.9f, 0.9f, 0.9f); break;
        }

        tmpText.fontSize = Mathf.Clamp(7f + damage * 0.04f, 7f, 11f);

        if (isCrit)
        {
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.fontSize *= 1.5f;
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

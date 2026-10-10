using UnityEngine;
using TMPro;

public class DamageIndicator : TextIndicator
{
    protected override float MinVerticalSpeed => 1f;
    protected override float MaxVerticalSpeed => 1.25f;

    public static void Spawn(Vector3 position, float damage, ElementalType elementalType, bool isCrit)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("DamageIndicator"), position, Quaternion.identity);
        go.GetComponent<DamageIndicator>()?.Initialize(damage, elementalType, isCrit);
    }

    public void Initialize(float damage, ElementalType elementalType, bool isCrit)
    {
        shrink = false;
        gravity = 2f;
        lifetime = 1f;

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
            default:                    color = new Color(0.9f, 0.9f, 0.9f); break;
        }

        // size scales with damage, clamped to a 2.5-3.5 range (cap reached at 250 damage)
        tmpText.fontSize = Mathf.Clamp(2.5f + damage * 0.004f, 2.5f, 3.5f);

        if (isCrit)
        {
            tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmpText.fontSize *= 1.15f;
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

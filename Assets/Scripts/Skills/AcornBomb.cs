using UnityEngine;
using System.Collections.Generic;

public class AcornBomb : MonoBehaviour
{
    private float aoeRadius;
    private float damage;
    private Plant source;

    private Transform visual;
    private Transform shadow;
    private float fallVelocity = 0f;
    private bool hasLanded = false;
    private bool isDead = false;

    private float hp = 25f;
    private const float maxHp = 25f;
    private float lifetime;
    private float eatTickTimer = 0f;

    private GameObject healthBarInstance;
    private Transform healthBarFill;

    private const float blockRadius = 0.5f;
    private const float startHeight = 20f;
    private const float gravityAccel = 9.8f;
    private const float eatTickInterval = 1f;
    private const float stunDuration = 2f;

    private readonly List<Insect> eatingInsects = new List<Insect>();
    private static readonly DamageTag[] impactTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(float aoeRadius, float damage, float lifetime, Plant source)
    {
        this.aoeRadius = aoeRadius;
        this.damage = damage;
        this.lifetime = lifetime;
        this.source = source;

        visual = transform.Find("Visual");
        if (visual != null)
            visual.localPosition = new Vector3(0f, startHeight, 0f);

        shadow = transform.Find("Shadow");
        if (shadow != null)
            shadow.localScale = new Vector3(0f, shadow.localScale.y, 1f);

        SpawnHealthBar();
    }

    private void SpawnHealthBar()
    {
        GameObject prefab = Resources.Load<GameObject>("HealthBar");
        if (prefab == null) return;
        healthBarInstance = Instantiate(prefab, transform);
        healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.4f, 0f);
        healthBarFill = healthBarInstance.transform.Find("Fill");
        healthBarInstance.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        float ratio = Mathf.Clamp01(hp / maxHp);
        Vector3 scale = healthBarFill.localScale;
        scale.x = ratio;
        healthBarFill.localScale = scale;
        if (healthBarInstance != null)
            healthBarInstance.SetActive(true);
    }

    private void Update()
    {
        if (isDead) return;

        if (!hasLanded)
        {
            Fall();
            return;
        }

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f) { Die(); return; }

        UpdateEatingInsects();

        eatTickTimer += Time.deltaTime;
        if (eatTickTimer >= eatTickInterval)
        {
            eatTickTimer -= eatTickInterval;
            DamageFromInsects();
        }
    }

    private void Fall()
    {
        fallVelocity += gravityAccel * Time.deltaTime;
        Vector3 pos = visual.localPosition;
        pos.y -= fallVelocity * Time.deltaTime;

        if (pos.y <= 0f)
        {
            pos.y = 0f;
            visual.localPosition = pos;
            if (shadow != null)
                shadow.localScale = new Vector3(0.8f, 0.4f, 1f);
            OnLand();
        }
        else
        {
            visual.localPosition = pos;
            if (shadow != null)
            {
                float t = 1f - (pos.y / startHeight);
                shadow.localScale = new Vector3(t * 0.8f, t * 0.4f, 1f);
            }
        }
    }

    private void OnLand()
    {
        hasLanded = true;

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
            {
                insect.Damage(damage, DamageType.Physical, ElementalType.Nature, source, false, impactTags);
                insect.ApplyEffect(new StunEffect(insect, stunDuration, 1, source));
            }
        }
    }

    private void UpdateEatingInsects()
    {
        for (int i = eatingInsects.Count - 1; i >= 0; i--)
        {
            if (eatingInsects[i] == null || eatingInsects[i].gameObject == null)
                eatingInsects.RemoveAt(i);
        }

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect.isFlying) continue;
            if (eatingInsects.Contains(insect)) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= blockRadius)
            {
                eatingInsects.Add(insect);
                insect.ApplyEffect(new EatingAcornEffect(insect, 999f, 1, source));
            }
        }
    }

    private void DamageFromInsects()
    {
        for (int i = eatingInsects.Count - 1; i >= 0; i--)
        {
            Insect insect = eatingInsects[i];
            if (insect == null || insect.gameObject == null)
            {
                eatingInsects.RemoveAt(i);
                continue;
            }
            hp -= insect.attackDamage;
        }

        UpdateHealthBar();
        if (hp <= 0f) Die();
    }

    private void OnMouseEnter()
    {
        if (healthBarInstance != null)
            healthBarInstance.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (hp >= maxHp)
            healthBarInstance?.SetActive(false);
    }

    private void Die()
    {
        isDead = true;
        for (int i = eatingInsects.Count - 1; i >= 0; i--)
        {
            if (eatingInsects[i] != null)
                eatingInsects[i].RemoveEffect<EatingAcornEffect>();
        }
        eatingInsects.Clear();
        Destroy(gameObject);
    }
}

using UnityEngine;
using System.Collections.Generic;

public class AcornBomb : MonoBehaviour, IAttackable
{
    private float aoeRadius;
    private float damage;
    private Plant source;

    private Transform visual;
    private Transform shadow;
    private float fallVelocity = 0f;
    private bool hasLanded = false;
    private bool isDead = false;

    private float hp;
    private float maxHp;
    private float lifetime;

    private GameObject healthBarInstance;
    private Transform healthBarFill;

    private const float startHeight = 20f;
    private const float gravityAccel = 9.8f;
    private const float stunDuration = 2f;

    private readonly HashSet<Insect> tauntedInsects = new HashSet<Insect>();
    private float tauntTickTimer = 0f;
    private static readonly DamageTag[] impactTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(float aoeRadius, float damage, float lifetime, float health, Plant source)
    {
        this.aoeRadius = aoeRadius;
        this.damage = damage;
        this.lifetime = lifetime;
        this.source = source;
        hp = health;
        maxHp = health;

        visual = transform.Find("Visual");
        if (visual != null)
        {
            visual.localPosition = new Vector3(0f, startHeight, 0f);
            visual.localScale = new Vector3(aoeRadius, aoeRadius, 1f);
        }

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

        UpdateTaunt();
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
                shadow.localScale = new Vector3(0.8f * aoeRadius, 0.4f * aoeRadius, 1f);
            OnLand();
        }
        else
        {
            visual.localPosition = pos;
            if (shadow != null)
            {
                float t = 1f - (pos.y / startHeight);
                shadow.localScale = new Vector3(t * 0.8f * aoeRadius, t * 0.4f * aoeRadius, 1f);
            }
        }
    }

    private void OnLand()
    {
        hasLanded = true;

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
            {
                insect.Damage(damage, DamageType.Physical, ElementalType.Nature, source, false, impactTags);
                insect.ApplyEffect(new StunEffect(insect, stunDuration, 1, source));
            }
        }
    }

    private void UpdateTaunt()
    {
        tauntedInsects.RemoveWhere(i => i == null || i.gameObject == null);

        tauntTickTimer -= Time.deltaTime;
        if (tauntTickTimer > 0f) return;
        tauntTickTimer = 0.25f;

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (insect.isFlying) continue;
            TauntEffect existing = insect.GetEffect<TauntEffect>();
            if (existing != null && existing.taunter != (IAttackable)this) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoeRadius)
            {
                insect.ApplyEffect(new TauntEffect(insect, 0.5f, 1, source, this));
                tauntedInsects.Add(insect);
            }
        }
    }

    // IAttackable
    public bool ReceiveAttack(float damage, Insect attacker)
    {
        if (isDead) return false;
        float missChance = Mathf.Clamp01(-attacker.accuracy); // no evasion on the bomb; negative accuracy (blind) causes misses
        if (UnityEngine.Random.value < missChance) return false;
        hp -= damage * attacker.eatMultiplier;
        UpdateHealthBar();
        if (hp <= 0f) Die();
        return true;
    }
    public bool IsAlive => !isDead;
    public Vector3 Position => transform.position;
    public Vector3 GetApproachPoint(Vector3 _) => transform.position;

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
        foreach (Insect insect in tauntedInsects)
        {
            if (insect != null && insect.GetEffect<TauntEffect>()?.taunter == (IAttackable)this)
                insect.RemoveEffect<TauntEffect>();
        }
        tauntedInsects.Clear();
        Destroy(gameObject);
    }
}

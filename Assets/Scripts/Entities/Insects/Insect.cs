using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public abstract class Insect : Entity
{
    public static List<Insect> allInsects = new List<Insect>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        allInsects = new List<Insect>();
        SceneManager.sceneLoaded += (scene, mode) => allInsects.Clear();
    }
    public int currentWaypointIndex = 0;
    protected Transform[] waypoints;
    public bool isFlying = false;
    public static float gravity = 9.8f;

    // base and final
    public float movementSpeed, baseMovementSpeed;

    // bonus
    public float movementSpeedAdder, movementSpeedMultiplier;
    
    protected override void UpdateStats()
    {
        base.UpdateStats();
        movementSpeed = baseMovementSpeed + movementSpeedAdder + (baseMovementSpeed * movementSpeedMultiplier);
    }

    public int sunDrop;
    public int expDrop;

    private GameManager gameManager;
    private Transform aimPoint;
    public Transform visual;
    private Vector2 pathOffset;

    protected override void Awake()
    {
        base.Awake();
        allInsects.Add(this);
        // Debug.Log("SPAWNED: " + gameObject);
    }

    void OnDestroy()
    {
        allInsects.Remove(this);
        if (InsectInfoUI.instance != null && InsectInfoUI.instance.GetSelectedInsect() == this)
            InsectInfoUI.instance.HidePanel();
    }

    protected virtual void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        waypoints = PathManager.instance.waypoints;
        expDrop = sunDrop/2;
        aimPoint = transform.Find("AimPoint");
        visual = transform.Find("Visual");
        pathOffset = new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));


        if (visual != null && healthBarInstance != null)
        {
            healthBarInstance.transform.SetParent(visual);
            healthBarInstance.transform.localPosition = new Vector3(-0.475f, 0.6f, 0);
        }

        int waveNumber = GameManager.instance.currentWave;
        baseMaxHealth *= 1f + ((waveNumber-1) * 0.04f);
        UpdateStats();
        health = maxHealth;
    }

    protected override void Update()
    {
        base.Update();
        Move();
        SyncAimPoint();
    }

    protected override void OnHoverExit()
    {
        if (health >= maxHealth && healthBarInstance != null)
            healthBarInstance.SetActive(false);
    }

    private void SyncAimPoint()
    {
        if (visual != null && aimPoint != null && aimPoint.localPosition != visual.localPosition)
            aimPoint.localPosition = visual.localPosition;
    }

    protected virtual void Move()
    {
        if (isDying) return;
        if (waypoints == null) return;
        if (HasEffect<HardCrowdControl>()) return;
        if (HasEffect<BubblePrisonEffect>()) return;

        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachObjective();
            return;
        }

        Transform target = waypoints[currentWaypointIndex]; // maybe randomize?
        Vector3 targetPos = target.position + new Vector3(pathOffset.x, pathOffset.y, 0);
        Vector3 direction = (targetPos - transform.position).normalized;
        transform.position += direction * GetMoveSpeed() * Time.deltaTime;

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            currentWaypointIndex++;
        }
    }

    protected override Vector3 GetIndicatorPosition()
    {
        return visual != null ? visual.position + Vector3.up * 0.25f : base.GetIndicatorPosition();
    }

    public Vector3 GetAimPoint()
    {
        return aimPoint != null ? aimPoint.position : transform.position;
    }

    public Transform GetCurrentWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length)
        return null;

        return waypoints[currentWaypointIndex];
    }

    protected virtual void ReachObjective()
    {
        gameManager.Damage((int)baseAttackDamage);
        Destroy(gameObject);
    }
    
    protected virtual float GetMoveSpeed() => movementSpeed;

    public virtual Vector3 GetVelocity()
    {
        if (currentWaypointIndex >= waypoints.Length)
        return Vector3.zero;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 targetPos = target.position + new Vector3(pathOffset.x, pathOffset.y, 0);
        Vector3 direction = (targetPos - transform.position).normalized;
        return direction * movementSpeed;
    }

    protected bool isDying = false;

    public override void Kill(Entity source)
    {
        if (isDying) return;
        isDying = true;
        DistributeExp();
        gameManager.AddSun(sunDrop);
        allInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    public override void Kill()
    {
        if (isDying) return;
        isDying = true;
        DistributeExp();
        gameManager.AddSun(sunDrop);
        allInsects.Remove(this);
        StartCoroutine(DeathFade());
    }

    private IEnumerator DeathFade()
    {
        healthBarInstance?.SetActive(false);

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            for (int i = 0; i < renderers.Length; i++)
            {
                Color c = startColors[i];
                c.a = alpha;
                renderers[i].color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }


    // EXP MANAGING
    
    public HashSet<Plant> attackerSet = new HashSet<Plant>();

    public void RegisterAttacker(Plant source)
    {
        if (source == null) return;
        attackerSet.Add(source); // if source already exists, the hashset automatically ignores it
    }

    private void DistributeExp()
    {
        int attackerCount = attackerSet.Count;
        if (attackerCount == 0) return;

        // calculating the share per plant with a minimum of 25% obtained
        float share = Mathf.Max(0.25f, 1f / attackerCount);
        int expReward = (int)(expDrop*share);

        foreach (Plant plant in attackerSet)
        {
            plant.GainExp(expReward);
        }
    }

    void OnMouseDown()
    {
        if (SkillTargetingManager.instance != null && SkillTargetingManager.instance.IsTargeting) return;
        InsectInfoUI.instance?.ShowPanel(this);
    }



    // DESCRIPTIONS
    public virtual string GetName()
    {
        return "";
    }

    public virtual string GetDescription()
    {
        return "";
    }

    public virtual string GetPassiveDescription()
    {
        return "";
    }

    public string GetActiveEffectsString()
    {
        string negative = "";
        string positive = "";

        foreach (StatusEffect effect in activeEffects)
        {
            int mins = Mathf.FloorToInt(effect.duration / 60);
            int secs = Mathf.FloorToInt(effect.duration % 60);
            string entry = $"{effect.GetName()} ({mins}:{secs:D2})\n";

            if (effect.effectType == StatusEffect.Type.positive)
                positive += entry;
            else
                negative += entry;
        }

        return $"<b>Active Effects:</b>\n\n" +
               $"<b>Positive:</b>\n{(positive == "" ? "None" : positive)}\n" +
               $"<b>Negative:</b>\n{(negative == "" ? "None" : negative)}";
    }
}

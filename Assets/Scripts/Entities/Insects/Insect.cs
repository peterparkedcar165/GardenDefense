using UnityEngine;
using UnityEngine.SceneManagement;
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
    }

    protected override void Update()
    {
        base.Update();
        Move();
    }

    protected virtual void Move()
    {
        if (waypoints == null) return;
        // IF INSECT IS HARD CC'ED
        if (HasEffect<HardCrowdControl>())
        {
            return;
        }

        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachObjective();
            return;
        }

        Transform target = waypoints[currentWaypointIndex]; // maybe randomize?
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * movementSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypointIndex++;
        }
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

    public virtual Vector3 GetVelocity()
    {
        if (currentWaypointIndex >= waypoints.Length)
        return Vector3.zero;

        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - transform.position).normalized;
        return direction * movementSpeed;
    }

    public override void Kill(Entity source)
    {
        DistributeExp();
        gameManager.AddSun(sunDrop);
        base.Kill();
    }

    public override void Kill()
    {
        DistributeExp();
        gameManager.AddSun(sunDrop);
        base.Kill();
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

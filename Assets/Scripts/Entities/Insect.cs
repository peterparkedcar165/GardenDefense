using UnityEngine;

public abstract class Insect : Entity
{

    protected int currentWaypointIndex = 0;
    protected Transform[] waypoints;

    public float movementSpeed;

    public int sunDrop;

    protected override void Awake()
    {
        base.Awake();
    }

    protected void Start()
    {
        waypoints = PathManager.instance.waypoints;
    }

    protected override void Update()
    {
        base.Update();
        Move();
    }

    protected virtual void Move()
    {
        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachObjective();
            return;
        }

        Transform target = waypoints[currentWaypointIndex];
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * movementSpeed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypointIndex++;
        }
    }

    protected virtual void ReachObjective()
    {
        Destroy(gameObject);
    }

}

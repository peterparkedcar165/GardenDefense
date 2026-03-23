using UnityEngine;

public class PathManager : MonoBehaviour
{

    public static PathManager instance;
    public Transform[] waypoints;

    void Awake()
    {
        if (instance == null)
        instance = this;
        else
        Destroy(gameObject);
    }
}

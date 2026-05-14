using UnityEngine;

public class PersistentCanvas : MonoBehaviour
{
    public static PersistentCanvas instance;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}

using System.Collections.Generic;
using UnityEngine;

// a temporary shortcut for ground insects, left behind once an Earthworm resurfaces. entrance and
// exit are just two point markers; any ground insect that wanders near the entrance while it's
// open gets diverted through entrance -> exit -> back onto the remainder of its own path (see
// Insect.RerouteTo/GetPathFromNearest), turning burrowed for the trip so it's just as
// undetectable/unhittable as the Earthworm that dug it.
//
// the entrance stops admitting new insects once openDuration elapses, but the tunnel itself
// (and the exit) stays alive until every insect currently mid-transit has actually reached the
// exit, so nobody gets stranded mid-tunnel when it closes
public class UndergroundTunnel : MonoBehaviour
{
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private float scanInterval = 0.3f;
    [SerializeField] private float arrivalRadius = 0.3f;

    private Transform entranceMarker;
    private Transform exitMarker;
    private float openTimer;
    private bool entranceOpen = true;
    private float scanTimer;
    private readonly List<Insect> travelers = new List<Insect>();

    // placeholder colors so entrance/exit are easy to tell apart at a glance; swap for real art later
    private static readonly Color EntranceColor = new Color(0.8f, 0.2f, 0.2f, 0.7f);
    private static readonly Color ExitColor     = new Color(0.2f, 0.8f, 0.3f, 0.7f);

    private static Sprite _circleSprite;
    private static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>("Circle");
                if (sprites.Length > 0) _circleSprite = sprites[0];
            }
            return _circleSprite;
        }
    }

    public static UndergroundTunnel Create(Vector3 entrancePos, Vector3 exitPos, float openDuration, float markerSize)
    {
        GameObject obj = new GameObject("UndergroundTunnel");
        UndergroundTunnel tunnel = obj.AddComponent<UndergroundTunnel>();

        tunnel.entranceMarker = CreateMarker("TunnelEntrance", entrancePos, obj.transform, markerSize, EntranceColor);
        tunnel.exitMarker     = CreateMarker("TunnelExit",     exitPos,     obj.transform, markerSize, ExitColor);

        tunnel.openTimer = openDuration;
        return tunnel;
    }

    // a circle, sized to match the insect that dug the tunnel, tinted per entrance/exit
    private static Transform CreateMarker(string name, Vector3 position, Transform parent, float size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * size;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite;
        sr.color = color;
        sr.sortingOrder = -1;
        return obj.transform;
    }

    private void Update()
    {
        if (entranceOpen)
        {
            openTimer -= Time.deltaTime;
            if (openTimer <= 0f)
            {
                entranceOpen = false;
                if (entranceMarker != null)
                {
                    Destroy(entranceMarker.gameObject);
                    entranceMarker = null;
                }
            }
            else
            {
                scanTimer -= Time.deltaTime;
                if (scanTimer <= 0f)
                {
                    scanTimer = scanInterval;
                    ScanForEntrants();
                }
            }
        }

        for (int i = travelers.Count - 1; i >= 0; i--)
        {
            Insect insect = travelers[i];
            if (insect == null || !insect.IsAlive)
            {
                travelers.RemoveAt(i);
                continue;
            }
            if (Vector3.Distance(insect.transform.position, exitMarker.position) <= arrivalRadius)
            {
                insect.SetBurrowed(false);
                travelers.RemoveAt(i);
            }
        }

        if (!entranceOpen && travelers.Count == 0)
        {
            if (exitMarker != null) Destroy(exitMarker.gameObject);
            Destroy(gameObject);
        }
    }

    private void ScanForEntrants()
    {
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (insect.isFlying || insect.isBurrowed || insect.carriedBy != null) continue;
            if (insect is Earthworm) continue;
            if (travelers.Contains(insect)) continue;
            if (Vector3.Distance(insect.transform.position, entranceMarker.position) > pickupRadius) continue;

            Transform[] remainder = insect.GetPathFromNearest(exitMarker.position);
            Transform[] newPath = new Transform[2 + remainder.Length];
            newPath[0] = entranceMarker;
            newPath[1] = exitMarker;
            remainder.CopyTo(newPath, 2);

            insect.RerouteTo(newPath);
            insect.SetBurrowed(true);
            travelers.Add(insect);
        }
    }
}

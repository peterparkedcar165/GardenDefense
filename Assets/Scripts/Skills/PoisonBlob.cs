using UnityEngine;
using System.Collections;

public class PoisonBlob : MonoBehaviour
{
    [SerializeField] private GameObject poisonFieldPrefab;

    private Vector3 target;
    private float activeRadius;
    private float fieldDuration;
    private float damagePerSecond;
    private Plant source;
    private Transform visual;

    private const float travelSpeed = 4f;
    private const float minTravelTime = 1f;
    private const float arcPeakHeight = 1.1f;
    private const float arcRiseEnd = 0.5f;
    private const float spawnScaleDuration = 0.3f;

    public void Initialize(Vector3 target, float activeRadius, float fieldDuration, Plant source, float damagePerSecond)
    {
        this.target = target;
        this.activeRadius = activeRadius;
        this.fieldDuration = fieldDuration;
        this.damagePerSecond = damagePerSecond;
        this.source = source;
        float s = activeRadius;
        transform.localScale = new Vector3(s, s, 1f);
        visual = transform.Find("Visual");
        StartCoroutine(TravelRoutine());
        StartCoroutine(SpawnScale());
    }

    private IEnumerator SpawnScale()
    {
        Vector3 fullScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < spawnScaleDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, fullScale, elapsed / spawnScaleDuration);
            yield return null;
        }
        transform.localScale = fullScale;
    }

    private IEnumerator TravelRoutine()
    {
        float totalDistance = Vector3.Distance(transform.position, target);
        float speed = Mathf.Min(travelSpeed, totalDistance / minTravelTime);
        float startY = visual != null ? visual.localPosition.y : 0f;

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (visual != null && totalDistance > 0f)
            {
                float progress = 1f - (Vector3.Distance(transform.position, target) / totalDistance);
                float arcY;
                if (progress < arcRiseEnd)
                    arcY = Mathf.Sin(progress / arcRiseEnd * Mathf.PI * 0.5f) * arcPeakHeight;
                else
                    arcY = Mathf.Cos((progress - arcRiseEnd) / (1f - arcRiseEnd) * Mathf.PI * 0.5f) * arcPeakHeight;
                float baseY = Mathf.Lerp(startY, 0f, progress);
                Vector3 pos = visual.localPosition;
                pos.y = arcY + baseY;
                visual.localPosition = pos;
            }

            yield return null;
        }

        transform.position = target;
        if (visual != null)
            visual.localPosition = new Vector3(visual.localPosition.x, 0f, visual.localPosition.z);

        Land();
    }

    private void Land()
    {
        if (poisonFieldPrefab != null)
        {
            GameObject obj = Instantiate(poisonFieldPrefab, transform.position, Quaternion.identity);
            PoisonField field = obj.GetComponent<PoisonField>();
            if (field != null)
                field.Initialize(activeRadius, fieldDuration, source, damagePerSecond);
        }
        Destroy(gameObject);
    }
}

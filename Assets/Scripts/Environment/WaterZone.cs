using UnityEngine;

public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Insect insect = other.GetComponentInParent<Insect>();
        if (insect == null || insect.isFlying) return;

        insect.Kill(insect.lastSource);
    }
}

using UnityEngine;

// Unity's Animation Event system calls SendMessage on the exact GameObject the Animator lives
// on, never on a parent - so a charge-visual child object (like Sunflower's attack circles)
// can't call Plant.OnAttackChargeFireFrame() directly from its own Animation Event, since that
// method lives on the parent Plant, not the child itself. this relay sits on the same child
// object as the Animator and forwards the call up to whichever Plant owns it, identifying itself
// by its own Animator so overlapping charges on different circles never cross-resolve each other
public class AttackChargeAnimationRelay : MonoBehaviour
{
    [SerializeField] private Plant owner;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    // hook this up to the Animation Event instead of OnAttackChargeFireFrame directly - must stay
    // public and parameterless for Unity's Animation Event dropdown to list and call it
    public void OnAttackChargeFireFrame()
    {
        if (owner != null) owner.OnAttackChargeFireFrame(_animator);
    }
}

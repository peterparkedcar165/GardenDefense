using UnityEngine;

// marks a StatType field/array so its Inspector dropdown excludes anything already covered by
// FertilizerStatRules.AllGloballyHandled (see RestrictedStatTypeDrawer)
public class RestrictedStatTypeAttribute : PropertyAttribute
{
}

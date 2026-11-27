using UnityEngine;
using UnityEngine.UI;

// draws a ui graphic mirrored left to right where it already is: the picture turns around its own middle, whatever the
// rect's pivot, layout or preserve aspect. insect art faces left (Tommy, 2026-11-27) and the menus show insects facing
// right. the ui shaders draw both faces, so the reversed triangles still show
[RequireComponent(typeof(Graphic))]
public class MirrorGraphic : BaseMeshEffect
{
    public static void Apply(Graphic target)
    {
        if (target != null && target.GetComponent<MirrorGraphic>() == null)
            target.gameObject.AddComponent<MirrorGraphic>();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;
        UIVertex vertex = new UIVertex();
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            min = Mathf.Min(min, vertex.position.x);
            max = Mathf.Max(max, vertex.position.x);
        }
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.position.x = min + max - vertex.position.x;
            vh.SetUIVertex(vertex, i);
        }
    }
}

using UnityEngine;

public class CameraFit : MonoBehaviour
{
    public SpriteRenderer backdrop;

    void Start()
    {
        float backdropHeight = backdrop.bounds.size.y;
        float backdropWidth = backdrop.bounds.size.x;

        Camera cam = GetComponent<Camera>();


        // setting the camera's orthographic size to  half of the backdrop height. so that the camera is always including the entire backdrop
        cam.orthographicSize = backdropHeight/2;

        transform.position = new Vector3(backdrop.bounds.center.x, backdrop.bounds.center.y, transform.position.z);
    }
}

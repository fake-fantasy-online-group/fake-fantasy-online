using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official orthographic scene-camera controller: wheel zoom plus WASD/arrow pan
// and right-button drag.
public class CameraControl : MonoBehaviour
{
    public float panSpeed;
    public float zoomSpeed;
    public float minSize;
    public float maxSize;

    private Camera cam;
    private Vector3 dragOrigin;

    // 060000c9: the official default constructor is the compiler materialisation of the
    // four field initializers; the constants were read from both architectures
    // (arm64 0x01126f30, x86_64 0x01118aa0) and are 15f, 8f, 1.5f, 30f in field order.
    public CameraControl()
    {
        panSpeed = 15f;
        zoomSpeed = 8f;
        minSize = 1.5f;
        maxSize = 30f;
    }

    // 060000c7: cache the Camera living on the same GameObject.
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // 060000c8
    private void Update()
    {
        if (cam == null)
            return;

        Vector2 scroll = Input.mouseScrollDelta;
        if (Mathf.Abs(scroll.y) > 0.001f)
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll.y * zoomSpeed, minSize, maxSize);

        // 1.6.0: sample the pan scale before either input-axis call.
        float speed = panSpeed * (cam.orthographicSize / maxSize) * 2f;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h != 0f || v != 0f)
        {
            transform.position += new Vector3(h, v, 0f) * speed * Time.deltaTime;
        }

        if (Input.GetMouseButtonDown(1))
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(1))
        {
            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            // The native body reads dragOrigin before getting this transform.
            Vector3 difference = new Vector3(dragOrigin.x - world.x, dragOrigin.y - world.y, 0f);
            transform.position += difference;
        }
    }
}

}

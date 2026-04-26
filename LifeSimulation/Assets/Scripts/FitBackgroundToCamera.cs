using UnityEngine;

[ExecuteAlways]
public class FitBackgroundToCamera : MonoBehaviour
{
    public Camera targetCamera;
    public SpriteRenderer sr;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (targetCamera == null || sr == null || sr.sprite == null)
            return;

        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        Vector2 spriteSize = sr.sprite.bounds.size;

        transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );

        // Keep it centered on camera
        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            0f
        );
    }
}

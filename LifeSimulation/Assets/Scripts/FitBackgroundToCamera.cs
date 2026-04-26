// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		System Visualization
// Requirement:	Camera Background Scaling
// Author:		Robert Amborski
// Date:		04/17/2026
//
// Description:
//    Dynamically scales and positions a background sprite so it always fills
//    the camera view, regardless of resolution or zoom level.
// -----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
/// Scales a background sprite to fully match the camera viewport.
/// </summary>
/// <remarks>
/// Executes in edit and play mode to ensure background always fits the camera.
/// Useful for maintaining full-screen visuals during zoom or aspect changes.
/// </remarks>
[ExecuteAlways]
public class FitBackgroundToCamera : MonoBehaviour
{
    /// <summary> Camera used for calculating viewport size </summary>
    public Camera targetCamera;

    /// <summary> SpriteRenderer used as the background visual </summary>
    public SpriteRenderer sr;

    /// <summary>
    /// Updates background scale and position after all camera movement.
    /// </summary>
    void LateUpdate()
    {
        // Fallback to main camera if none explicitly assigned
        if (targetCamera == null)
            targetCamera = Camera.main;

        // Ensure sprite renderer reference is assigned
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        // Abort if required components are missing to prevent runtime errors
        if (targetCamera == null || sr == null || sr.sprite == null)
            return;

        // Calculate full camera dimensions in world units
        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;

        // Get original sprite size to determine scaling ratio
        Vector2 spriteSize = sr.sprite.bounds.size;

        // Scale sprite so it exactly fills the camera viewport
        transform.localScale = new Vector3(
            camWidth / spriteSize.x,
            camHeight / spriteSize.y,
            1f
        );

        // Lock background position to camera to prevent drift during movement
        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            0f
        );
    }
}

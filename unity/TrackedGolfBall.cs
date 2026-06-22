using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class TrackedGolfBall : MonoBehaviour
{
    [Header("Trail Settings")]
    [Tooltip("The TrailRenderer component on the ball.")]
    public TrailRenderer trailRenderer;
    
    [Header("Distance HUD Settings")]
    [Tooltip("The TextMeshPro (3D Text) component showing the actual distance.")]
    public TextMeshPro distanceMarkerText;
    
    [Tooltip("The GameObject representing the distance marker container. Will be unparented on launch to prevent rotation.")]
    public GameObject distanceMarker;

    private Vector3 teePosition;
    private float spawnTime;
    private Rigidbody rb;
    private bool velocityApplied = false;
    private Vector3 initialVelocity;
    
    // Neon colors replicating the WebVR version
    private static readonly Color[] TrailColors = new Color[]
    {
        new Color(1.0f, 0.2f, 0.4f),    // #ff3366 - Hot Pink
        new Color(0.2f, 1.0f, 0.4f),    // #33ff66 - Neon Green
        new Color(0.2f, 0.4f, 1.0f),    // #3366ff - Electric Blue
        new Color(1.0f, 1.0f, 0.2f),    // #ffff33 - Bright Yellow
        new Color(1.0f, 0.6f, 0.2f),    // #ff9933 - Orange
        new Color(0.8f, 0.2f, 1.0f)     // #cc33ff - Violet Purple
    };

    public void Initialize(Vector3 velocity, Vector3 startTeePosition)
    {
        initialVelocity = velocity;
        teePosition = startTeePosition;
        
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = initialVelocity;
            velocityApplied = true;
        }
        
        spawnTime = Time.time;

        // Choose a random neon color for the trail
        Color randomColor = TrailColors[Random.Range(0, TrailColors.Length)];
        
        // Setup TrailRenderer properties dynamically matching WebVR
        if (trailRenderer != null)
        {
            trailRenderer.startColor = randomColor;
            trailRenderer.endColor = new Color(randomColor.r, randomColor.g, randomColor.b, 0.0f); // Fade to transparent
            trailRenderer.startWidth = 0.24f; // 24cm total width
            trailRenderer.endWidth = 0.05f;  // Taper off nicely
        }

        // Unparent the marker immediately so that as the ball spins and rolls,
        // the text marker stays stable and doesn't rotate or orbit the ball
        if (distanceMarker != null)
        {
            distanceMarker.transform.SetParent(null);
        }
    }

    private void Start()
    {
        // Secondary velocity application in case Rigidbody initialization is delayed
        if (!velocityApplied && rb != null)
        {
            rb.linearVelocity = initialVelocity;
            velocityApplied = true;
        }
    }

    private void Update()
    {
        Vector3 ballPos = transform.position;

        // 1. Out of bounds (below map) or lifetime cleanup (20 seconds)
        if (ballPos.y < -15f || (Time.time - spawnTime) > 20f)
        {
            DestroyBall();
            return;
        }

        // 2. Update and align Distance HUD Marker
        if (distanceMarker != null && Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;

            // Horizontal distance from the Tee Box position (X and Z components only)
            float distFromTee = Vector2.Distance(
                new Vector2(ballPos.x, ballPos.z), 
                new Vector2(teePosition.x, teePosition.z)
            );

            // Update text to match WebVR format ("0.0 m")
            if (distanceMarkerText != null)
            {
                distanceMarkerText.text = distFromTee.ToString("F1") + " m";
            }

            // Find camera's right direction projected horizontally onto XZ plane
            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0.0f;
            cameraRight.Normalize();

            // Keep perceived size constant as the ball gets farther from the camera
            float distToCam = Vector3.Distance(ballPos, cameraPos);
            float scale = Mathf.Max(0.1f, distToCam * 0.18f);
            distanceMarker.transform.localScale = new Vector3(scale, scale, scale);

            // Position marker beside the ball (offset along horizontal camera right)
            float offsetAmount = scale * 1.5f;
            Vector3 markerPos = ballPos + cameraRight * offsetAmount;
            markerPos.y = ballPos.y + (scale * 0.15f); // Scale vertical float height
            distanceMarker.transform.position = markerPos;

            // Make the marker billboard towards the camera
            distanceMarker.transform.LookAt(cameraPos);
            // Rotated 180 degrees because TextMeshPro faces local Z direction (reverse of LookAt)
            distanceMarker.transform.Rotate(0, 180f, 0);
        }
    }

    private void DestroyBall()
    {
        // Ensure the unparented distance marker is destroyed along with the ball
        if (distanceMarker != null)
        {
            Destroy(distanceMarker);
        }
        Destroy(gameObject);
    }
}

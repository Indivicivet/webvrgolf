using UnityEngine;
using UnityEngine.InputSystem;

public class GolfCannon : MonoBehaviour
{
    [Header("Cannon Settings")]
    [Tooltip("Maximum velocity magnitude of the fired golf ball (default is 50).")]
    public float maxPower = 50f;

    [Tooltip("Spawn distance forward from the cannon center (keeps the ball from spawning inside the barrel).")]
    public float spawnOffset = 0.6f;
    
    [Header("Golf Ball Template")]
    [Tooltip("The TrackedGolfBall template child object. Must be in the scene hierarchy (disabled).")]
    public TrackedGolfBall ballTemplate;
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireClip;

    [Header("Input & Tracking Actions")]
    [Tooltip("Global Action Reference for the trigger button (e.g. XRI RightHand Activate).")]
    public InputActionProperty fireAction;
    
    [Tooltip("Global Action Reference for controller position tracking.")]
    public InputActionProperty positionAction;
    
    [Tooltip("Global Action Reference for controller rotation tracking.")]
    public InputActionProperty rotationAction;

    private bool wasTriggerPressedLastFrame = false;

    private void Awake()
    {
        // Deactivate the template so it doesn't show up in the scene at startup
        if (ballTemplate != null)
        {
            ballTemplate.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EnableAction(fireAction);
        EnableAction(positionAction);
        EnableAction(rotationAction);
    }

    private void OnDisable()
    {
        DisableAction(fireAction);
        DisableAction(positionAction);
        DisableAction(rotationAction);
    }

    private void EnableAction(InputActionProperty actionProperty)
    {
        if (actionProperty.action != null)
        {
            actionProperty.action.Enable();
        }
    }

    private void DisableAction(InputActionProperty actionProperty)
    {
        if (actionProperty.action != null)
        {
            actionProperty.action.Disable();
        }
    }

    private void Update()
    {
        bool isTriggerPressed = CheckTriggerInput();

        if (isTriggerPressed && !wasTriggerPressedLastFrame)
        {
            FireBall();
        }

        wasTriggerPressedLastFrame = isTriggerPressed;
    }

    private void LateUpdate()
    {
        UpdateTrackingPose();
    }

    private void UpdateTrackingPose()
    {
        if (positionAction.action != null && positionAction.action.enabled)
        {
            transform.position = positionAction.action.ReadValue<Vector3>();
        }
        if (rotationAction.action != null && rotationAction.action.enabled)
        {
            transform.rotation = rotationAction.action.ReadValue<Quaternion>();
        }
    }

    private bool CheckTriggerInput()
    {
        if (fireAction.action != null && fireAction.action.enabled)
        {
            var val = fireAction.action.ReadValue<float>();
            if (val > 0.5f) return true;
            if (fireAction.action.triggered) return true;
        }
        return false;
    }

    public void FireBall()
    {
        if (ballTemplate == null)
        {
            Debug.LogWarning("GolfCannon: Ball template is not assigned under GolfCannon script!");
            return;
        }

        if (audioSource != null && fireClip != null)
        {
            audioSource.PlayOneShot(fireClip);
        }

        // Firing direction is along the cannon's forward vector (Z-axis in Unity)
        Vector3 fireDirection = transform.forward;
        Vector3 spawnPosition = transform.position + fireDirection * spawnOffset;

        // Instantiate (duplicate) the template ball in the scene
        GameObject spawnedBallObj = Instantiate(ballTemplate.gameObject, spawnPosition, Quaternion.identity);
        spawnedBallObj.SetActive(true);

        TrackedGolfBall spawnedBall = spawnedBallObj.GetComponent<TrackedGolfBall>();
        if (spawnedBall != null)
        {
            // Replicate WebVR power formula: randomizedPower is between 40% and 100% of maxPower
            float randomizedPower = maxPower * (0.4f + Random.value * 0.6f);
            Vector3 velocity = fireDirection * randomizedPower;

            spawnedBall.Initialize(velocity, transform.position);
        }
    }
}

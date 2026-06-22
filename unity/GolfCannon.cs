using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

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

    [Header("Input Options")]
    [Tooltip("Action Reference for the trigger button (e.g. from standard XRI Default Input Actions, e.g., 'Activate').")]
    public InputActionProperty fireAction;

    [Tooltip("If checked, uses the legacy InputDevice system if the Action Reference is not set.")]
    public bool useXRNodeFallback = true;
    [Tooltip("The XR controller node to read trigger inputs from when using legacy fallback.")]
    public XRNode controllerNode = XRNode.RightHand;

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
        if (fireAction.action != null)
        {
            fireAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction.action != null)
        {
            fireAction.action.Disable();
        }
    }

    private void Update()
    {
        bool isTriggerPressed = CheckTriggerInput();

        // Edge detection: fire on the frame the trigger is pressed down
        if (isTriggerPressed && !wasTriggerPressedLastFrame)
        {
            FireBall();
        }

        wasTriggerPressedLastFrame = isTriggerPressed;
    }

    private bool CheckTriggerInput()
    {
        // 1. Try to use Action-based Input System if assigned
        if (fireAction.action != null && fireAction.action.enabled)
        {
            // Handles both float (Trigger value) and binary (Trigger button) actions
            var val = fireAction.action.ReadValue<float>();
            return val > 0.5f;
        }

        // 2. Fallback to device-based input
        if (useXRNodeFallback)
        {
            var device = InputDevices.GetDeviceAtXRNode(controllerNode);
            if (device.isValid)
            {
                if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed))
                {
                    return pressed;
                }
                if (device.TryGetFeatureValue(CommonUsages.trigger, out float triggerVal))
                {
                    return triggerVal > 0.5f;
                }
            }
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

        // Play sound if configured
        if (audioSource != null && fireClip != null)
        {
            audioSource.PlayOneShot(fireClip);
        }

        // Firing direction is along the cannon's forward vector (Z-axis in Unity)
        Vector3 fireDirection = transform.forward;
        
        // Spawn position offset slightly forward
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

            // Initialize the ball with velocity and the cannon's current position (acting as Tee Box)
            spawnedBall.Initialize(velocity, transform.position);
        }
    }
}

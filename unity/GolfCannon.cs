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
    [Tooltip("Action Reference for the trigger button (e.g., standard XRI RightHand Activate).")]
    public InputActionProperty fireAction;

    [Tooltip("The XR controller node to read inputs and tracking from.")]
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

        if (isTriggerPressed && !wasTriggerPressedLastFrame)
        {
            FireBall();
        }

        wasTriggerPressedLastFrame = isTriggerPressed;
    }

    private void LateUpdate()
    {
        // Always track the controller position and rotation in world space
        UpdateTrackingPose();
    }

    private void UpdateTrackingPose()
    {
        var device = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (device.isValid)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 pos))
            {
                transform.position = pos;
            }
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion rot))
            {
                transform.rotation = rot;
            }
        }
    }

    private bool CheckTriggerInput()
    {
        // 1. Try modern Input System Action
        if (fireAction.action != null && fireAction.action.enabled)
        {
            var val = fireAction.action.ReadValue<float>();
            if (val > 0.5f) return true;
            if (fireAction.action.triggered) return true;
        }

        // 2. Fallback to direct legacy hardware polling
        var device = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (device.isValid)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed))
            {
                return pressed;
            }
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerVal))
            {
                return triggerVal > 0.5f;
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

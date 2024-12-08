using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GlobalFogAdjuster : MonoBehaviour
{
    [Header("Fog Settings")]
    public Color fogColor = Color.gray; // The color of the fog
    public FogMode fogMode = FogMode.Linear; // Fog mode (Linear, Exponential, ExponentialSquared)
    public float fogStartDistance = 10f; // Base start distance for fog
    public float fogEndDistance = 1000f; // Base end distance for fog
    public float maxFogDensity = 0.02f; // Maximum fog density for Exponential modes
    public float minFogDensity = 0.001f; // Minimum fog density for Exponential modes

    [Header("Dynamic Fog Control")]
    public bool adjustFogBasedOnPlayer = true; // Toggle for dynamic adjustment
    public Transform playerTransform; // Reference to the player or camera transform
    public float fogAdjustmentRange = 50f; // Range for adjusting fog dynamically

    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();

        // Enable fog
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;

        AdjustFog(); // Initial adjustment
    }

    void Update()
    {
        if (adjustFogBasedOnPlayer)
        {
            AdjustFog();
        }
    }

    void AdjustFog()
    {
        if (fogMode == FogMode.Linear)
        {
            // Adjust fog start and end distances dynamically based on player position
            if (playerTransform != null)
            {
                float distanceFromOrigin = Vector3.Distance(playerTransform.position, Vector3.zero); // Example distance metric
                float adjustmentFactor = Mathf.Clamp01(distanceFromOrigin / fogAdjustmentRange);

                RenderSettings.fogStartDistance = fogStartDistance + adjustmentFactor * 50f; // Example scaling
                RenderSettings.fogEndDistance = fogEndDistance + adjustmentFactor * 100f;
            }
            else
            {
                // Fallback to default distances
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
            }
        }
        else
        {
            // Adjust fog density for Exponential modes
            if (playerTransform != null)
            {
                float distanceFromOrigin = Vector3.Distance(playerTransform.position, Vector3.zero);
                float adjustmentFactor = Mathf.Clamp01(distanceFromOrigin / fogAdjustmentRange);

                RenderSettings.fogDensity = Mathf.Lerp(minFogDensity, maxFogDensity, adjustmentFactor);
            }
            else
            {
                // Default fog density
                RenderSettings.fogDensity = minFogDensity;
            }
        }
    }
}

using Unity.Services.Core;
using Unity.Services.Analytics;
using Photon.Pun;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class AnalyticsInitializer : MonoBehaviourPunCallbacks
{
    private static bool isInitialized = false;

    private float sessionStartTime;
    private float lastSentTime = 0f;
    private float sendInterval = 10f; // Interval to send FPS analytics

    private float totalSpeed = 0f;
    private int speedSamples = 0;
    private float timeInAir = 0f;
    private float timeOnGround = 0f;
    private bool wasGrounded = true;

    public PlayerController playerController;
    private bool isTrackingSession = false; // Flag to begin tracking

    private void Update()
    {
        TrackPerformance();
        if (IsMapScene())
        {
            TrackSpeed();
            TrackAirAndGroundTime();
        }
    }
        
        private void TrackSpeed()
    {
        // Track player speed
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            float currentSpeed = rb.velocity.magnitude;
            totalSpeed += currentSpeed;
            speedSamples++;
        }
    }

        private bool IsMapScene()
    {
        // Check if the current scene is map1 or map2
        string currentSceneName = SceneManager.GetActiveScene().name;
        return currentSceneName == "map1" || currentSceneName == "map2";
    }


       private void TrackAirAndGroundTime()
    {
        // Check whether the player is on the ground (assumes an `isGrounded` variable)
        bool isGrounded = CheckIfGrounded();

        if (isGrounded)
        {
            if (!wasGrounded)
            {
                wasGrounded = true;
            }
            timeOnGround += Time.deltaTime;
        }
        else
        {
            if (wasGrounded)
            {
                wasGrounded = false;
            }
            timeInAir += Time.deltaTime;
        }
    }

        private bool CheckIfGrounded()
    {
        // Your logic for determining if the player is on the ground
        // For example a raycast or a variable from PlayerController
        return true; // Replace with your implementation
    }

private async void Start()
{
    // Find the PlayerController in the scene
    playerController = FindObjectOfType<PlayerController>();
    if (playerController == null)
    {
        Debug.LogWarning("PlayerController not found. It will be searched for on each loaded scene.");
    }

    // Keep AnalyticsInitializer between scenes
    DontDestroyOnLoad(gameObject);
    StartCoroutine(CheckPlayerController()); // Regularly check for the player

    // Subscribe to the scene loading event
    SceneManager.sceneLoaded += OnSceneLoaded;

    // Initialize Unity Services (Analytics etc.)
    if (!isInitialized)
    {
        await InitializeUnityServices();
        isInitialized = true;
    }

    // Set the session start time
    sessionStartTime = Time.time;
    LogSessionStart(); // Log the session start
}
        private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the event
    }
private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    StartCoroutine(WaitAndFindPlayerController(scene));
}

private IEnumerator WaitAndFindPlayerController(Scene scene)
{
    yield return new WaitForSeconds(6f); // Wait a moment for the scene to load

    playerController = FindObjectOfType<PlayerController>();
    if (playerController == null)
    {
        Debug.LogWarning($"PlayerController not found in scene: {scene.name}. Session tracking may be limited.");
        yield break; // Zakończ korutynę zamiast używać return
    }

    // Start tracking the session
    isTrackingSession = true;
    sessionStartTime = Time.time;

    Debug.Log($"PlayerController found in scene: {scene.name}. Session tracking started.");
}
    private IEnumerator CheckPlayerController()
    {
        while (true) // Endless loop running for the duration of the game
        {
            if (playerController == null) // Check if the player exists
            {
                Debug.LogWarning("PlayerController does not exist. Searching for a new player...");
                playerController = FindObjectOfType<PlayerController>(); // Try to find a new PlayerController

                if (playerController != null)
                {
                    Debug.Log("PlayerController found. Continuing tracking.");
                }
            }

            yield return new WaitForSeconds(1f); // Check every second
        }
    }
    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (AnalyticsService.Instance != null)
            {
                AnalyticsService.Instance.StartDataCollection();
                Debug.Log("Unity Analytics initialized and data collection started.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    // Log session start
    public void LogSessionStart()
    {
         if (!isTrackingSession) return;

        AnalyticsService.Instance.CustomData("SessionStarted", new Dictionary<string, object>
        {
            { "start_time", sessionStartTime },
            { "player_id", PhotonNetwork.LocalPlayer?.UserId }
        });

        Debug.Log($"Session started at: {sessionStartTime}");
    }

    // Track FPS performance
    private void TrackPerformance()
    {
        float currentFPS = 1.0f / Time.deltaTime;
        if (currentFPS < 30 && Time.time - lastSentTime > sendInterval)
        {
            AnalyticsService.Instance.CustomData("LowFPS", new Dictionary<string, object>
            {
                { "fps", currentFPS },
                { "time", Time.time }
            });

            Debug.Log($"Low FPS detected and event sent: {currentFPS}");
            lastSentTime = Time.time;
        }
    }

    // public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    // {
    //     LogSessionEnd(cause);
    // }

    // // Log session end
    // private void LogSessionEnd(Photon.Realtime.DisconnectCause cause)
    // {
    //     float sessionEndTime = Time.time;
    //     float sessionLength = sessionEndTime - sessionStartTime;

    //     AnalyticsService.Instance.CustomData("SessionEnded", new Dictionary<string, object>
    //     {
    //         { "session_length", sessionLength },
    //         { "end_time", sessionEndTime },
    //         { "player_id", PhotonNetwork.LocalPlayer?.UserId },
    //         { "disconnect_cause", cause.ToString() }
    //     });

    //     Debug.Log($"Session ended. Length: {sessionLength} seconds. Reason: {cause}");
    // }
    // jetpack start
    public void LogJetpackStart(Vector3 position, float time)
    {
        var eventData = new Dictionary<string, object>
        {
            { "position_x", position.x },
            { "position_y", position.y },
            { "position_z", position.z },
            { "jetpack_start_time", time }
        };

        AnalyticsService.Instance.CustomData("JetpackStart", eventData);
        Debug.Log($"JetpackStart event sent with data: {eventData}");
    }

    // jetpack end
    public void LogJetpackEnd(Vector3 position, float time)
    {
        var eventData = new Dictionary<string, object>
        {
            { "position_x", position.x },
            { "position_y", position.y },
            { "position_z", position.z },
            { "jetpack_end_time", time }
        };

        AnalyticsService.Instance.CustomData("JetpackEnd", eventData);
        Debug.Log($"JetpackEnd event sent with data: {eventData}");
    }


    // Whether the energypack is used and where
    public void LogEnergypackUsed(Vector3 position, float time)
    {
        var eventData = new Dictionary<string, object>
        {
            { "position_x", position.x },
            { "position_y", position.y },
            { "position_z", position.z },
            { "time", time }
        };

        AnalyticsService.Instance.CustomData("EnergypackUsed", eventData);
        Debug.Log($"EnergypackUsed event sent with data: {eventData}");
    }

    // Player death
    public void LogPlayerDied(Vector3 position, float health, float time)
    {
        var eventData = new Dictionary<string, object>
        {
            { "position_x", position.x },
            { "position_y", position.y },
            { "position_z", position.z },
            { "health", health },
            { "time", time }
        };

        AnalyticsService.Instance.CustomData("PlayerDied", eventData);
        Debug.Log($"PlayerDied event sent with data: {eventData}");
    }
        public void StartSessionTracking(PlayerController controller)
    {
        playerController = controller;
        sessionStartTime = Time.time;
        isTrackingSession = true;
    }

public void LogSessionEnd(string disconnectCause)
{
    if (!isTrackingSession)
    {
        Debug.LogWarning("LogSessionEnd: Session tracking is not active.");
        return;
    }

    float sessionEndTime = Time.time;
    float sessionLength = sessionEndTime - sessionStartTime;

    float averageSpeed = playerController.AverageSpeed;
    float timeInAir = playerController.TimeInAir;
    float timeOnGround = playerController.TimeOnGround;

    AnalyticsService.Instance.CustomData("SessionEnded", new Dictionary<string, object>
    {
        { "session_length", sessionLength },
        { "end_time", sessionEndTime },
        { "disconnect_cause", disconnectCause },
        { "average_speed", averageSpeed },
        { "time_in_air", timeInAir },
        { "time_on_ground", timeOnGround }
    });

    Debug.Log($"Session ended. Length: {sessionLength} seconds. Reason: {disconnectCause}");
    Debug.Log($"Average speed: {averageSpeed}, Time in air: {timeInAir}, Time on ground: {timeOnGround}");
}

}


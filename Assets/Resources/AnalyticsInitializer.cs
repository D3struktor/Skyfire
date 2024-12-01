using Unity.Services.Core;
using Unity.Services.Analytics;
using Photon.Pun;
using UnityEngine;
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

    private PlayerController playerController;
    private bool isTrackingSession = false; // Flaga do rozpoczęcia śledzenia

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
        // Śledzenie prędkości gracza
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
        // Sprawdza, czy bieżąca scena to map1 lub map2
        string currentSceneName = SceneManager.GetActiveScene().name;
        return currentSceneName == "map1" || currentSceneName == "map2";
    }


       private void TrackAirAndGroundTime()
    {
        // Sprawdzanie, czy gracz jest na ziemi (załóżmy, że masz zmienną `isGrounded`)
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
        // Twoja logika określania, czy gracz jest na ziemi
        // Na przykład raycast lub zmienna z PlayerController
        return true; // Zmień na swoją implementację
    }

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (!isInitialized)
        {
            await InitializeUnityServices();
            isInitialized = true;
        }

        // Start session tracking
        sessionStartTime = Time.time;
        LogSessionStart();
    }
        private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Odsubskrybujemy zdarzenie
    }
     private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 1 || scene.buildIndex == 2) // Indeksy scen Map1 i Map2
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController not found on Map1 or Map2!");
                return;
            }

            sessionStartTime = Time.time; // Ustaw start sesji
            isTrackingSession = true;    // Rozpocznij śledzenie
            LogSessionStart();
        }
        else
        {
            isTrackingSession = false; // Zatrzymaj śledzenie na niewłaściwych scenach
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
    //jetpack start
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

    // Jetpack koniec
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


    //Czy uzywaja energypacka i gdzie
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

    // Smierc gracza
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

    public void LogSessionEnd(string disconnectCause)
    {
        if (!isTrackingSession) return;

        float sessionEndTime = Time.time;
        float sessionLength = sessionEndTime - sessionStartTime;

        if (playerController != null)
        {
            float averageSpeed = playerController.CurrentSpeed; // Pobranie średniej prędkości
            float timeInAir = playerController.TimeInAir;       // Pobranie czasu w powietrzu
            float timeOnGround = playerController.TimeOnGround; // Pobranie czasu na ziemi

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
        else
        {
            Debug.LogError("PlayerController is missing, cannot log session stats!");
        }
    }
}

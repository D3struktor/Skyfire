using System.Collections.Generic;
using Unity.Services.Analytics;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    private static AnalyticsManager _instance;

    public static AnalyticsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("AnalyticsManager");
                _instance = obj.AddComponent<AnalyticsManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void LogCustomEvent(string eventName, Dictionary<string, object> parameters)
    {
        if (AnalyticsService.Instance != null)
        {
            AnalyticsService.Instance.CustomData(eventName, parameters);
            Debug.Log($"Event logged: {eventName} with parameters: {parameters}");
        }
        else
        {
            Debug.LogError("AnalyticsService is not initialized.");
        }
    }
}

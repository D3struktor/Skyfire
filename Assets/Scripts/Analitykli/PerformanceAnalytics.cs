using System.Collections.Generic;
using UnityEngine;

public class PerformanceAnalytics : MonoBehaviour
{
    private float lastSentTime = 0f; 
    private float sendInterval = 10f; 

    void Update()
    {
        float currentFPS = 1.0f / Time.deltaTime;
        if (currentFPS < 30 && Time.time - lastSentTime > sendInterval)
        {
            AnalyticsManager.Instance.LogCustomEvent("LowFPS", new Dictionary<string, object>
            {
                { "fps", currentFPS },
                { "time", Time.time }
            });

            Debug.Log($"Low FPS detected and event sent: {currentFPS}");
            lastSentTime = Time.time; 
        }
    }
}

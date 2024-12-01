using UnityEngine;
using System.Collections.Generic;

public class AirBoostUIAnalytics : MonoBehaviour
{
    public void OnAirBoostFullyCharged()
    {
        AnalyticsManager.Instance.LogCustomEvent("AirBoostCharged", new Dictionary<string, object>
        {
            { "time", Time.time }
        });

        Debug.Log($"Custom event 'AirBoostCharged' sent at time: {Time.time}");
    }

    public void OnAirBoostUsed()
    {
        AnalyticsManager.Instance.LogCustomEvent("AirBoostUsedFromUI", new Dictionary<string, object>
        {
            { "time", Time.time }
        });

        Debug.Log("Custom event 'AirBoostUsedFromUI' sent.");
    }
}

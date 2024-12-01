using System.Collections.Generic;
using UnityEngine;

public class JetpackAnalytics : MonoBehaviour
{
    public void UseJetpack()
    {
        Dictionary<string, object> eventData = new Dictionary<string, object>
        {
            { "position_x", transform.position.x },
            { "position_y", transform.position.y },
            { "position_z", transform.position.z },
            { "time", Time.time }
        };

        AnalyticsManager.Instance.LogCustomEvent("JetpackUsed", eventData);

        Debug.Log($"Custom event 'JetpackUsed' sent with data: {eventData}");
    }
}

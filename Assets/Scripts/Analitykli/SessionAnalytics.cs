using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class SessionAnalytics : MonoBehaviourPunCallbacks
{
    private float sessionStartTime;

    public override void OnConnectedToMaster()
    {
        sessionStartTime = Time.time;

        AnalyticsManager.Instance.LogCustomEvent("SessionStarted", new Dictionary<string, object>
        {
            { "start_time", sessionStartTime },
            { "player_id", PhotonNetwork.LocalPlayer.UserId }
        });

        Debug.Log($"Session started at: {sessionStartTime}");
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        float sessionEndTime = Time.time;
        float sessionLength = sessionEndTime - sessionStartTime;

        AnalyticsManager.Instance.LogCustomEvent("SessionEnded", new Dictionary<string, object>
        {
            { "session_length", sessionLength },
            { "end_time", sessionEndTime },
            { "player_id", PhotonNetwork.LocalPlayer.UserId },
            { "disconnect_cause", cause.ToString() }
        });

        Debug.Log($"Session ended. Length: {sessionLength} seconds. Reason: {cause}");
    }
}

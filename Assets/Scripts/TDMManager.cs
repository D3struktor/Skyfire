using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TDMManager : MonoBehaviourPunCallbacks
{
    public Color redTeamColor = Color.red;
    public Color blueTeamColor = Color.blue;

    private int redTeamCount = 0;
    private int blueTeamCount = 0;

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateTeamCounts();
        }
    }


    public bool AssignTeam()
    {

        redTeamCount = 0;
        blueTeamCount = 0;

        UpdateTeamCounts();

        bool isRedTeam = false;
        if (redTeamCount == blueTeamCount)
        {
            isRedTeam = Random.Range(0, 2) == 0;
        }
        else
        {
            isRedTeam = redTeamCount < blueTeamCount;
        }

        string assignedTeam = isRedTeam ? "Red" : "Blue";
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "Team", assignedTeam } });


        Debug.Log(isRedTeam ? "TEAM RED." : "TEAM BLUE.");
        Debug.Log($"Red Team Count: {redTeamCount}, Blue Team Count: {blueTeamCount}");
        Debug.Log($"Przypisano: {assignedTeam} <========");
        return isRedTeam;
    }



    private void UpdateTeamCounts()
    {
        redTeamCount = 0;
        blueTeamCount = 0;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("Team"))
            {
                string playerTeam = player.CustomProperties["Team"] as string;
                if (playerTeam == "Red")
                {
                    redTeamCount++;
                }
                else if (playerTeam == "Blue")
                {
                    blueTeamCount++;
                }
            }
        }

        Debug.Log($"Updated Team Counts - Red: {redTeamCount}, Blue: {blueTeamCount}");
    }

    public Color GetTeamColor(Player player)
    {
        Debug.Log("TDMManager: Returning team color for player " + player.NickName);
        string team = player.CustomProperties["Team"] as string;

        Debug.Log("TDMManager: player " + player.NickName + " is in TEAM " + team);
        return team == "Red" ? redTeamColor : blueTeamColor;
    }
}
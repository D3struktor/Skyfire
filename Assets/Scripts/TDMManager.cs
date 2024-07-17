using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TDMManager : MonoBehaviourPunCallbacks
{
    public Color redTeamColor = Color.red;
    public Color blueTeamColor = Color.blue;
    public GameObject playerControllerPrefab;

    public bool AssignTeam()
    {
        // Losowe przypisanie do drużyny
        bool isRedTeam = Random.Range(0, 2) == 0;
        string team = isRedTeam ? "Red" : "Blue";
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "Team", team } });

        Debug.Log(isRedTeam ? "TEAM RED." : "TEAM BLUE.");

        return isRedTeam;
    }

    public Color GetTeamColor()
    {
        string team = PhotonNetwork.LocalPlayer.CustomProperties["Team"] as string;
        return team == "Red" ? redTeamColor : blueTeamColor;
    }

    [PunRPC]
    public void StartTDM()
    {
        bool isRedTeam = AssignTeam();
        Color teamColor = GetTeamColor();

        GameObject playerController = PhotonNetwork.Instantiate(playerControllerPrefab.name, Vector3.zero, Quaternion.identity);
        playerController.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, teamColor.r, teamColor.g, teamColor.b);
    }
}

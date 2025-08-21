using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    private string roomName = "GlobalRoom";  // Name of the room everyone will join

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined lobby");
        JoinOrCreateRoom();
    }

    public void JoinOrCreateRoom()
    {
        Debug.Log("Attempting to join or create the room...");
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };  // Adjust max players as needed
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            Debug.Log("Loading scene for the first player.");
            PhotonNetwork.LoadLevel("SampleScene");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("A new player entered the room: " + newPlayer.NickName);
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            Debug.Log("Second player joined. Loading scene for both players.");
            PhotonNetwork.LoadLevel("SampleScene");
        }
    }
}

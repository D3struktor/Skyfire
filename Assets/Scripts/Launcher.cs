using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject PlayerListItemPrefab;
    [SerializeField] GameObject startGameButton;
    [SerializeField] GameObject startGameTDMButton;

    void Awake()
    {
        Instance = this;  
    }

    void Start()
    {
        Debug.Log("Launcher: Connecting to Master");
        PhotonNetwork.ConnectUsingSettings();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public override void OnConnectedToMaster()  
    {
        Debug.Log("Launcher: Connected to Master");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()  
    {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("Launcher: Joined Lobby");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text))
        {
            return;
        }

        Debug.Log("Launcher: Creating room " + roomNameInputField.text);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10;
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties.Add("gameStarted", false); // Domyślnie gra nie jest rozpoczęta
        roomOptions.CustomRoomProperties = roomProperties;
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "gameStarted" }; // Dodanie tej właściwości do wyświetlania w lobby

        PhotonNetwork.CreateRoom(roomNameInputField.text, roomOptions);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnJoinedRoom()
    {
        MenuManager.Instance.OpenMenu("room");
        roomNameText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
        Player[] players = PhotonNetwork.PlayerList;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        startGameTDMButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
        startGameTDMButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        MenuManager.Instance.OpenMenu("error");
        Debug.LogError("Launcher: Create room failed with message " + message);
    }

    public void StartGame()
    {
        PlayerPrefs.SetString("GameMode", "DM");
        Debug.Log("Launcher: GameMode set to DM.");

        // Ustawienie właściwości pokoju 'gameStarted'
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties["gameStarted"] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

        PhotonNetwork.LoadLevel(1);
    }

    public void StartGameTDM()
    {
        PlayerPrefs.SetString("GameMode", "TDM");
        Debug.Log("Launcher: GameMode set to TDM.");

        // Ustawienie właściwości pokoju 'gameStarted'
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
            roomProperties["gameStarted"] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }

        PhotonNetwork.LoadLevel(2);
    }

    public void LeaveRoom()
    {
        Debug.Log("Launcher: Leaving room.");
        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom(RoomInfo info)
    {
        Debug.Log("Launcher: Joining room " + info.Name);
        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        MenuManager.Instance.OpenMenu("title");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            RoomInfo roomInfo = roomList[i];

            // Jeśli pokój został usunięty z listy, pomiń go
            if (roomInfo.RemovedFromList)
            {
                continue;
            }

            // Sprawdzenie, czy pokój ma ustawioną właściwość 'gameStarted' i czy gra się rozpoczęła
            if (roomInfo.CustomProperties.ContainsKey("gameStarted") && (bool)roomInfo.CustomProperties["gameStarted"] == true)
            {
                // Jeśli gra w pokoju się rozpoczęła, pomijamy go na liście
                Debug.Log("Room " + roomInfo.Name + " is hidden because the game has already started.");
                continue;
            }

            // Dodaj pokój do listy tylko, jeśli gra się nie rozpoczęła
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomInfo);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }
}

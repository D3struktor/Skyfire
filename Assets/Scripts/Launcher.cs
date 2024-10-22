using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.UI;

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
    
    // Nowe elementy dla ustawień
    [SerializeField] GameObject settingsPanel;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] AudioMixer SFXMixer; // Declare SFXMixer here

    // Declare savedVolume and savedSFXVolume
    private float savedVolume;
    private float savedSFXVolume;

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

        // Initialize savedVolume and savedSFXVolume from PlayerPrefs
        savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f); // Default to 0.5 if not found
        savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f); // Default to 0.5 if not found

        // Load the main volume settings
        volumeSlider.value = savedVolume;
        volumeSlider.onValueChanged.AddListener(SetVolume);
        SetVolume(savedVolume); // Set the volume initially

        // Load the SFX volume settings
        sfxVolumeSlider.value = savedSFXVolume;
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        SetSFXVolume(savedSFXVolume); // Set the SFX volume initially
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
        PhotonNetwork.CreateRoom(roomNameInputField.text);
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
        PhotonNetwork.LoadLevel(1);
    }

    public void StartGameTDM()
    {
        PlayerPrefs.SetString("GameMode", "TDM");
        Debug.Log("Launcher: GameMode set to TDM.");
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
            if (roomList[i].RemovedFromList)
                continue;
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    // Funkcje dla ustawień i głośności
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20); // Zmiana wartości w AudioMixer
        PlayerPrefs.SetFloat("Volume", volume); // Zapisanie głośności
    }

    public void SetSFXVolume(float volume)
    {
        SFXMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20); // Zmiana wartości w AudioMixer dla SFX
        PlayerPrefs.SetFloat("SFXVolume", volume); // Zapisanie głośności SFX
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game pressed. Quitting application.");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Dla edytora Unity
        #else
            Application.Quit(); // Prawdziwe wyjście z gry
        #endif
    }
}

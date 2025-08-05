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
    [SerializeField] GameObject howPanel;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] AudioMixer SFXMixer;

    // Elementy do ekranu ładowania
    [SerializeField] private GameObject loadingScreen;  // Panel dla ekranu ładowania
    [SerializeField] private Image loadingImage;        // Obraz JPG na ekranie ładowania
    [SerializeField] private TMP_Text loadingText;      // Tekst wyświetlający stan ładowania
    [SerializeField] private Slider loadingSlider;      // Pasek ładowania

    // Symulowane ładowanie
    [SerializeField] private float fakeLoadingTime = 6.0f;  // Czas trwania symulowanego ładowania

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
    Debug.Log("Launcher: Host setting GameMode to DM for all players.");
    

    // RPC to set game mode for all players
    photonView.RPC("SetGameMode", RpcTarget.All, "DM");
        // Zamknij pokój i ustaw go jako niewidoczny
    if (PhotonNetwork.CurrentRoom != null)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;    // Zamknięcie pokoju - gracze nie mogą dołączyć
        PhotonNetwork.CurrentRoom.IsVisible = false; // Ukrycie pokoju z listy
        Debug.Log("Launcher: Room is now closed and hidden from the lobby.");
    }
    // Show loading screen and load the scene
    ShowLoadingScreen();
    StartCoroutine(FakeLoadingProgress(1)); // Replace with scene name if needed

}

public void StartGameTDM()
{
    Debug.Log("Launcher: Host setting GameMode to TDM for all players.");
   

    // RPC to set game mode for all players
    photonView.RPC("SetGameMode", RpcTarget.All, "TDM");

    // Zamknij pokój i ustaw go jako niewidoczny
    if (PhotonNetwork.CurrentRoom != null)
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;    // Zamknięcie pokoju - gracze nie mogą dołączyć
        PhotonNetwork.CurrentRoom.IsVisible = false; // Ukrycie pokoju z listy
        Debug.Log("Launcher: Room is now closed and hidden from the lobby.");
    }

    // Show loading screen and load the scene
    StartCoroutine(FakeLoadingProgress(2)); // Replace with scene name if needed
}


// Centralized RPC for setting game mode
[PunRPC]
public void SetGameMode(string mode)
{
    SetLocalGameMode(mode);
}

// Helper to set the game mode locally
private void SetLocalGameMode(string mode)
{
    PlayerPrefs.SetString("GameMode", mode);
    Debug.Log($"Launcher: GameMode set to {mode}.");
}



    private void ShowLoadingScreen()
    {
        // Włącz ekran ładowania
        loadingScreen.SetActive(true);

        // Zresetuj wartość paska i tekst
        if (loadingSlider != null)
        {
            loadingSlider.value = 0f;
        }

        if (loadingText != null)
        {
            loadingText.text = "Ładowanie... 0%";
        }
    }

    // Symulacja ładowania paska postępu z faktycznym ładowaniem sceny
[PunRPC]
private IEnumerator FakeLoadingProgress(int sceneIndex)
{
    float elapsedTime = 0f;

    // Rozpocznij ładowanie sceny w tle
    PhotonNetwork.LoadLevel(sceneIndex);

    // Zresetuj pasek ładowania do 0%
    loadingSlider.value = 0f;

    while (elapsedTime < fakeLoadingTime)
    {
        // Oblicz postęp na podstawie czasu
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / fakeLoadingTime);

        // Ustaw wartość paska
        loadingSlider.value = progress;

        // Aktualizacja tekstu ładowania
        if (loadingText != null)
        {
            loadingText.text = "Ładowanie... " + (progress * 100f).ToString("F0") + "%";
        }

        yield return null;  // Czekaj do następnej klatki
    }
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
        // Pomijaj pokoje, które są pełne lub usunięte z listy
        if (roomList[i].RemovedFromList || roomList[i].PlayerCount >= roomList[i].MaxPlayers)
        {
            continue;
        }

        Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
    }
}

public override void OnPlayerEnteredRoom(Player newPlayer)
{
    // Dodaj nowego gracza do listy graczy
    Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);


    //sprawdz czy maks players
        if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
    {
        PhotonNetwork.CurrentRoom.IsVisible = false; // Ukryj pokój z listy
        Debug.Log("Launcher: Room is now full and has been hidden from the lobby.");
    }

    // Sprawdź, czy tryb gry został ustawiony w CustomProperties pokoju
    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameMode"))
    {
        // Pobierz aktualny tryb gry z właściwości pokoju
        string gameMode = PhotonNetwork.CurrentRoom.CustomProperties["GameMode"].ToString();
        Debug.Log($"Launcher: Player {newPlayer.NickName} joined the room. GameMode is {gameMode}");

        // Ustaw tryb gry dla nowego gracza
        PlayerPrefs.SetString("GameMode", gameMode);

        Debug.Log($"Launcher: Updated GameMode for {newPlayer.NickName} to {gameMode}");
    }
    else
    {
        Debug.Log("Launcher: No GameMode set in room properties.");
    }
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
        public void OpenHowToPlay()
    {
        howPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        howPanel.SetActive(false);
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
    // Quit the application
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // For Unity Editor
    #else
        Application.Quit(); // For standalone builds
    #endif
}


}

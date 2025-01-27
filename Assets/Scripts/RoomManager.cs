using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using System.IO;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate RoomManager found and destroyed.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("RoomManager instance set and marked as DontDestroyOnLoad.");
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient starting the game.");
            
            // Zamknięcie pokoju i ukrycie go z listy
            if (PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                Debug.Log("Room closed and hidden from lobby.");
            }

            LoadRandomScene();
        }
        else
        {
            Debug.LogError("StartGame called but the player is not the MasterClient.");
        }
    }

    void LoadRandomScene()
    {
        int randomSceneIndex = Random.Range(1, 3); // Random.Range with 1 inclusive and 3 exclusive, so it picks 1 or 2
        Debug.Log("Loading random scene with index: " + randomSceneIndex);
        PhotonNetwork.LoadLevel(randomSceneIndex);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Debug.Log("Scene loaded: " + scene.name + " with index: " + scene.buildIndex);
        if (scene.buildIndex == 1 || scene.buildIndex == 2)
        {
            PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
        }
    }

    // Metoda wywoływana, gdy nowy gracz dołącza do pokoju
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New player joined: " + newPlayer.NickName);

        // Synchronizacja stanu gry lub wysłanie powiadomień do innych graczy
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log("Player in room: " + player.NickName);
        }

        // Wysłanie danych do nowego gracza
        photonView.RPC("SyncPlayerData", newPlayer);
    }

    // RPC do synchronizacji danych dla nowego gracza
    [PunRPC]
    public void SyncPlayerData()
    {
        Debug.Log("Synchronizing player data for new player.");
        // Dodaj kod do synchronizacji stanu gry, np. pozycje graczy, zdrowie, amunicję itp.
    }

public override void OnPlayerLeftRoom(Player otherPlayer)
{
    Debug.Log("Player left: " + otherPlayer.NickName);

    // Sprawdź, czy gra została rozpoczęta
    if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameStarted") && 
        (bool)PhotonNetwork.CurrentRoom.CustomProperties["GameStarted"])
    {
        Debug.Log("Launcher: Game has started. Room remains hidden even after player left.");
        return; // Jeśli gra jest rozpoczęta, nic więcej nie robimy
    }

    // Jeśli gra nie jest rozpoczęta, sprawdź liczbę graczy i ewentualnie pokaż pokój
    if (PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
    {
        PhotonNetwork.CurrentRoom.IsVisible = true; // Ponownie pokaż pokój
        Debug.Log("Launcher: Room is no longer full and is now visible in the lobby.");
    }
}

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created. Setting max players.");
        
        // Ustaw maksymalną liczbę graczy w pokoju
        if (PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.MaxPlayers = 8;
        }
      
    }
}

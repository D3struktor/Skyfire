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

        // Przykładowo, możesz tutaj zsynchronizować stan gry lub wysłać powiadomienia do innych graczy
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Debug.Log("Player in room: " + player.NickName);
        }

        // Możesz także wysłać dane do nowego gracza
        photonView.RPC("SyncPlayerData", newPlayer);
    }

    // RPC do synchronizacji danych dla nowego gracza
    [PunRPC]
    public void SyncPlayerData()
    {
        Debug.Log("Synchronizing player data for new player.");
        // Tutaj dodaj kod do synchronizacji stanu gry, np. pozycje graczy, zdrowie, amunicję itp.
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("Player left: " + otherPlayer.NickName);

        // Możesz tutaj obsłużyć usuwanie gracza z mapy lub inny cleanup
    }
}

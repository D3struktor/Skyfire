using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
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
}
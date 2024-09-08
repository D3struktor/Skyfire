using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    PhotonView PV;

    GameObject controller;
    int kills;
    int deaths;
    bool isAlive = true;
    public Color color;

    Camera playerCamera; // Kamera na stałe przypisana do gracza

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        if (PV == null)
        {
            Debug.LogError("PhotonView is missing from PlayerController.");
        }

        // Znajdź kamerę i przypisz ją do lokalnego gracza
        if (PV.IsMine)
        {
            playerCamera = Camera.main; // Pobieramy główną kamerę
            if (playerCamera == null)
            {
                Debug.LogError("No main camera found in the scene.");
            }
            else
            {
                playerCamera.gameObject.SetActive(true); // Aktywujemy kamerę dla lokalnego gracza
            }
        }
    }

    void Start()
    {
        if (PV.IsMine)
        {
            Debug.Log("PlayerManager: Start called for local player " + PhotonNetwork.NickName);

            if (PlayerPrefs.GetString("GameMode") == "TDM")
            {
                Debug.Log("PlayerManager: GameMode is TDM");
                StartTDMForAllPlayers();
            }
            else
            {
                Debug.Log("PlayerManager: GameMode is not TDM, creating controller");
                CreateController();
            }
        }
    }

    public void CreateController()
    {
        if (!isAlive)
            return;

        Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { PV.ViewID });

        // Obsługujemy kamerę tylko dla lokalnego gracza, ale nie niszczymy kamery po śmierci
        if (PV.IsMine && playerCamera != null)
        {
            playerCamera.transform.SetParent(null); // Ustawiamy kamerę jako niezależną (nie przypisaną do kontrolera)
            playerCamera.transform.position = controller.transform.position + new Vector3(0, 1.6f, 0); // Umieszczamy kamerę nad graczem
            playerCamera.transform.rotation = Quaternion.identity; // Zerujemy rotację kamery
        }

        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                Debug.Log("PlayerManager: TDMManager found.");

                // Pobieramy kolor od razu po stworzeniu kontrolera
                color = tdmManager.GetPlayerColor(PhotonNetwork.LocalPlayer);

                // Ustawiamy kolor natychmiast
                SetPlayerColor(controller, color);
            }
            else
            {
                Debug.LogError("PlayerManager: TDMManager not found.");
            }
        }
        else
        {
            color = new Color(Random.value, Random.value, Random.value);
            SetPlayerColor(controller, color);
        }
    }

    private void SetPlayerColor(GameObject playerController, Color color)
    {
        if (playerController != null)
        {
            // Ustawiamy kolor od razu na modelu gracza i synchronizujemy z innymi klientami
            playerController.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
            Debug.Log($"PlayerManager: Set player color to RGBA({color.r}, {color.g}, {color.b}, {color.a})");
        }
    }

    public void Die()
    {
        if (PV.IsMine && isAlive)
        {
            if (controller != null)
            {
                if (controller.GetComponent<PhotonView>() != null)
                {
                    PhotonNetwork.Destroy(controller); // Niszczenie kontrolera gracza
                }
                else
                {
                    Debug.LogWarning("PhotonView is missing from controller, skipping destroy.");
                }
            }

            StartCoroutine(RespawnAfterDelay(2f));
            isAlive = false;
        }
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        isAlive = true;
        CreateController();
    }

    public void RecordDeath(Player killer)
    {
        Debug.Log("Player died");
        if (PV.IsMine)
        {
            deaths++;
            Hashtable hash = new Hashtable();
            hash.Add("deaths", deaths);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} died. Total deaths: {deaths}.");

            // Avoid adding a kill if the killer is the same as the victim
            if (killer != null && killer != PhotonNetwork.LocalPlayer)
            {
                PlayerManager killerPM = Find(killer);
                if (killerPM != null)
                {
                    killerPM.PV.RPC(nameof(AddKill), killerPM.PV.Owner);
                    photonView.RPC("UpdateKillFeed", RpcTarget.All, killer.NickName, PhotonNetwork.LocalPlayer.NickName);
                }
            }

            isAlive = false;
        }
    }
    [PunRPC]
public void UpdateKillFeed(string killerName, string victimName)
{
    // Wywołanie funkcji dodawania wpisu do kill feeda
    KillFeedManager.Instance.AddKillFeedEntry(killerName, victimName);
}

    [PunRPC]
    public void AddKill()
    {
        if (!PV.IsMine) return;

        kills++;
        Hashtable hash = new Hashtable();
        hash.Add("kills", kills);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} got a kill. Total kills: {kills}.");
    }

    public static PlayerManager Find(Player player)
    {
        return FindObjectsOfType<PlayerManager>().ToList().SingleOrDefault(x => x.PV.Owner == player);
    }

    public void ReportKill(Player killedPlayer)
    {
        if (PV.IsMine)
        {
            var killedPlayerManager = Find(killedPlayer);
            if (killedPlayerManager != null)
            {
                killedPlayerManager.RecordDeath(PhotonNetwork.LocalPlayer);
            }
        }
    }

    void OnPhotonPlayerDisconnected(Player otherPlayer)
    {
        if (PV.IsMine)
        {
            PlayerManager pm = Find(otherPlayer);
            if (pm != null && pm.controller != null)
            {
                PhotonNetwork.Destroy(pm.controller);
            }
        }
    }

    [PunRPC]
    public void StartTDM()
    {
        TDMManager tdmManager = FindObjectOfType<TDMManager>();

        if (tdmManager != null)
        {
            // Użycie logiki przypisania koloru zamiast drużyny
            color = tdmManager.GetPlayerColor(PhotonNetwork.LocalPlayer);
            Debug.Log("Set color = " + color);
        }
        else
        {
            Debug.LogError("TDMManager not found in the scene.");
            color = Color.green;
        }

        if (PV.IsMine)
        {
            CreateController();
        }

        this.controller.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
    }

    void StartTDMForAllPlayers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartTDM", RpcTarget.AllBuffered);
        }
        else
        {
            StartTDM();
        }
    }

    public PhotonView GetPhotonView()
    {
        return PV;
    }

public void DieWithoutCountingDeath()
{
    // Sprawdzenie, czy tryb gry to TDM
    if (PlayerPrefs.GetString("GameMode") == "TDM")
    {
        if (PV.IsMine && isAlive)
        {
            if (controller != null)
            {
                PhotonNetwork.Destroy(controller); // Niszczenie kontrolera gracza
            }

            StartCoroutine(RespawnAfterDelay(2f)); // Respawn po 2 sekundach
            isAlive = false;
        }
    }
    else
    {
        Debug.Log("DieWithoutCountingDeath: This method is only available in TDM mode.");
    }
}

}

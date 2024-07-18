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

    // TDM value.
    // -1 => Not TDM match started
    // 0  => TDM started, blue team assigned
    // 1  => TDM started, red team assigned

    int isRed = -1;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
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

        // Set the color from TDMManager if in TDM mode
        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                Debug.Log("PlayerManager: TDMManager found.");

                // color = Color.red;
                if (isRed == -1)
                    Debug.Log("PlayerManager: O CHUJ NIE DZIALA.");

                controller.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
            }
            else
            {
                Debug.LogError("PlayerManager: TDMManager not found.");
            }
        }
        else
        {
            color = new Color(Random.value, Random.value, Random.value);
            controller.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
        }
    }

    public void Die()
    {
        if (PV.IsMine && isAlive)
        {
            if (controller != null)
            {
                PhotonNetwork.Destroy(controller);
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
            if (killer != null)
            {
                PlayerManager killerPM = Find(killer);
                if (killerPM != null)
                {
                    killerPM.PV.RPC(nameof(AddKill), killerPM.PV.Owner);
                }
            }

            isAlive = false; // Ensure the player is marked as not alive
        }
    }

    [PunRPC]
    public void AddKill()
    {
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
            // Clean up when other players leave
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
            bool ValRed = tdmManager.AssignTeam();

            if (ValRed)
            {
                color = Color.red;
                isRed = 1;
            }
            else
            {
                color = Color.blue;
                isRed = 0;
            }

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
            // CreateController();
            StartTDM();
        }
    }
}

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

    public GameObject controller;
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
            CreateController();

            if (PlayerPrefs.GetString("GameMode") == "TDM")
            {
                StartTDMForAllPlayers();
            }

            // AssignRandomColor();
        }
    }

    public void AssignRandomColor()
    {
        if (PV.IsMine)
        {
            color = new Color(Random.value, Random.value, Random.value);
            controller.GetComponent<PlayerController>().randomColor = color;
            PV.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered, PV.Owner, color.r, color.g, color.b);
        }
    }

    public void CreateController()
    {
        if (!isAlive)
            return;

        Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint();
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { PV.ViewID });

        // Ustaw kolor gracza
        Color cccc = Color.red;
        controller.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, 1.0f, 0.0f, 0.0f);
        controller.GetComponent<PlayerController>().playerRenderer.material.color = cccc;
        controller.GetComponent<PlayerController>().randomColor = cccc;

        // Set the color from TDMManager if in TDM mode
        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                color = tdmManager.GetTeamColor();
                controller.GetComponent<PlayerController>().randomColor = color;
                PV.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered, PV.Owner, color.r, color.g, color.b);
            }
        }
        else
        {
            controller.GetComponent<PlayerController>().randomColor = color;
            PV.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered, PV.Owner, color.r, color.g, color.b);
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
            controller.GetComponent<PlayerController>().randomColor = color;
            PV.RPC("RPC_SetPlayerColor", RpcTarget.AllBuffered, PV.Owner, color.r, color.g, color.b);
        }
        else
        {
            Debug.LogError("TDMManager not found in the scene.");
        }

        controller.GetComponent<Renderer>().material.color = color;
    }

    [PunRPC]
    public void RPC_SetPlayerColor(Player p, float r, float g, float b)
    {
        color = new Color(r, g, b);
        Debug.Log("SET PLAYER COLORITO WORKING, SET COLOR " + color + " for player " + p);
        PlayerManager player = Find(p);

        player.color = color;
        if (player.controller != null)
        {
            player.controller.GetComponent<PlayerController>().randomColor = color;
            player.controller.GetComponent<Renderer>().material.color = color;
        }
    }

    void StartTDMForAllPlayers()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartTDM", RpcTarget.All);
        }
    }
}

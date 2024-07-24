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

        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                Debug.Log("PlayerManager: TDMManager found.");

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

            // Avoid adding a kill if the killer is the same as the victim
            if (killer != null && killer != PhotonNetwork.LocalPlayer)
            {
                PlayerManager killerPM = Find(killer);
                if (killerPM != null)
                {
                    killerPM.PV.RPC(nameof(AddKill), killerPM.PV.Owner);
                }
            }

            isAlive = false;
        }
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
            StartTDM();
        }
    }
    public PhotonView GetPhotonView()
    {
        return PV;
    }
}

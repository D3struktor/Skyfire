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

        // Find the camera and assign it to the local player
        if (PV.IsMine)
        {
            playerCamera = Camera.main; // Grab the main camera
            if (playerCamera == null)
            {
                Debug.LogError("No main camera found in the scene.");
            }
            else
            {
                playerCamera.gameObject.SetActive(true); // Activate the camera for the local player
            }
        }
    }

    void Start()
    {
        if (PV.IsMine)
        {
            Debug.Log("PlayerManager: Start called for local player " + PhotonNetwork.NickName);

            FadeInOnStart fadeScript = FindObjectOfType<FadeInOnStart>();
            if (fadeScript != null)
            {
                fadeScript.gameObject.SetActive(true);
            }

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
        Debug.Log($"PlayerManager: Attempting to create controller for {PhotonNetwork.LocalPlayer.NickName} (IsMine: {PV.IsMine}, IsMasterClient: {PhotonNetwork.IsMasterClient})");

        if (!isAlive)
        {
            Debug.Log($"PlayerManager: Player {PhotonNetwork.LocalPlayer.NickName} is not alive. Skipping controller creation.");
            return;
        }

        Transform spawnpoint = null;

        // Check game mode
        string gameMode = PlayerPrefs.GetString("GameMode");
        Debug.Log($"PlayerManager: Current GameMode is {gameMode}");

        if (gameMode == "TDM")
        {
            // Use spawn system for TDM
            SpawnManagerTDM spawnManagerTDM = FindObjectOfType<SpawnManagerTDM>();
            if (spawnManagerTDM != null)
            {
                Debug.Log("PlayerManager: SpawnManagerTDM found.");

                // Get respawn point based on team
                spawnpoint = spawnManagerTDM.GetSpawnPoint(PhotonNetwork.LocalPlayer);

                if (spawnpoint != null)
                {
                Debug.Log($"PlayerManager: Spawn point found at {spawnpoint.position} for player {PhotonNetwork.LocalPlayer.NickName} in TDM mode.");
                }
                else
                {
                    Debug.LogError($"PlayerManager: No valid spawn point found for player {PhotonNetwork.LocalPlayer.NickName} in TDM mode.");
                    return;
                }
            }
            else
            {
                Debug.LogError("PlayerManager: SpawnManagerTDM not found.");
                return;
            }
        }
        else
        {
            // Standard spawning for other game modes
            spawnpoint = SpawnManager.Instance.GetSpawnpoint();

            if (spawnpoint != null)
            {
                Debug.Log($"PlayerManager: Standard spawn point found at {spawnpoint.position} for player {PhotonNetwork.LocalPlayer.NickName}.");
            }
            else
            {
                    Debug.LogError($"PlayerManager: No valid spawn point found for player {PhotonNetwork.LocalPlayer.NickName} in non-TDM mode.");
                return;
            }
        }

        // Create player controller at chosen respawn point for ALL players
        if (spawnpoint != null && controller == null)
        {
            Debug.Log($"PlayerManager: Instantiating PlayerController prefab for {PhotonNetwork.LocalPlayer.NickName} at spawn point {spawnpoint.position}.");
            controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { PV.ViewID });
        }
        else if (controller != null)
        {
            Debug.LogWarning($"PlayerManager: Controller already exists for {PhotonNetwork.LocalPlayer.NickName}, skipping creation.");
        }

        // Handle camera only for the local player (camera assigned only locally)
        if (PV.IsMine && playerCamera != null)
        {
            playerCamera.transform.SetParent(null); // Make camera independent (not attached to controller)
            playerCamera.transform.position = controller.transform.position + new Vector3(0, 1.6f, 0); // Place camera above the player
            playerCamera.transform.rotation = Quaternion.identity; // Reset camera rotation

            Debug.Log($"PlayerManager: Camera assigned and positioned for {PhotonNetwork.LocalPlayer.NickName}.");
        }

        // Assign player color in TDM mode
        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                Debug.Log("PlayerManager: TDMManager found.");

                // Get team immediately after creating controller
                SpawnpointTDM.TeamColor playerTeam = tdmManager.GetPlayerTeam(PhotonNetwork.LocalPlayer);

                // Set color based on team
                if (playerTeam == SpawnpointTDM.TeamColor.Red)
                {
                    color = Color.red;
                }
                else if (playerTeam == SpawnpointTDM.TeamColor.Blue)
                {
                    color = Color.blue;
                }

                Debug.Log($"PlayerManager: Player {PhotonNetwork.LocalPlayer.NickName} assigned color {color} based on team {playerTeam}.");

                // Set player color immediately
                SetPlayerColor(controller, color);
            }
            else
            {
                Debug.LogError("PlayerManager: TDMManager not found.");
            }
        }
    }

    private void SetPlayerColor(GameObject playerController, Color color)
    {
        if (playerController != null)
        {
            // Immediately set the player model color and synchronize with other clients
            playerController.GetComponent<PhotonView>().RPC("SetPlayerColor", RpcTarget.AllBuffered, color.r, color.g, color.b);
            Debug.Log($"PlayerManager: Set player color to RGBA({color.r}, {color.g}, {color.b}, {color.a})");
        }
    }

public void Die()
{
    Debug.Log($"Die() called for {PhotonNetwork.LocalPlayer.NickName} (IsMine: {PV.IsMine}, IsAlive: {isAlive})");

    if (!PV.IsMine || !isAlive)
    {
        Debug.LogWarning("Die() called but PV.IsMine == false or player already dead.");
        return;
    }

    if (controller != null)
    {
        PhotonView controllerPV = controller.GetComponent<PhotonView>();
        if (controllerPV != null && controllerPV.IsMine)
        {
            PhotonNetwork.Destroy(controller);
            Debug.Log("Kontroler gracza zniszczony.");
        }
        else
        {
            Debug.LogWarning("Kontroler nie należy do lokalnego gracza, pomijam zniszczenie.");
        }
    }
    else
    {
        Debug.LogWarning("Controller jest null, nie można go zniszczyć.");
    }

    isAlive = false; // Aktualizacja stanu lokalnego gracza
    StartCoroutine(RespawnAfterDelay(2f)); // Rozpoczęcie respawnu
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

        // Check that the player did not kill themselves or a teammate
        if (killer != null && killer != PhotonNetwork.LocalPlayer)
        {
            TDMManager tdmManager = FindObjectOfType<TDMManager>();
            if (tdmManager != null)
            {
                // Get the victim's and killer's teams
                SpawnpointTDM.TeamColor victimTeam = tdmManager.GetPlayerTeam(PhotonNetwork.LocalPlayer);
                SpawnpointTDM.TeamColor killerTeam = tdmManager.GetPlayerTeam(killer);

                // Check if the killer is on the same team as the victim (team kill)
                if (victimTeam == killerTeam)
                {
                    Debug.Log($"PlayerManager: {killer.NickName} killed a teammate {PhotonNetwork.LocalPlayer.NickName}. Removing a kill.");

                    // Remove a kill point from the killer in case of a team kill
                    PlayerManager teamKillPM = Find(killer);
                    if (teamKillPM != null)
                    {
                        teamKillPM.PV.RPC(nameof(RemoveKill), teamKillPM.PV.Owner);
                    }
                    return; // Do not count a normal kill because it's a team kill
                }

                // Increase the killer's team kill count
                tdmManager.AddKillToTeam(killerTeam);
            }

            // Award the kill to the killer (if not a team kill)
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
public void RemoveKill()
{
    if (!PV.IsMine) return;

    kills--;
    Hashtable hash = new Hashtable();
    hash.Add("kills", kills);
    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName} lost a kill. Total kills: {kills}.");
}



    [PunRPC]
    public void UpdateKillFeed(string killerName, string victimName)
    {
        // Call the function that adds an entry to the kill feed
        KillFeedManager.Instance.AddKillFeedEntry(killerName, victimName);
    }

 [PunRPC]
public void AddKill()
{
    Debug.Log($"AddKill called for {PhotonNetwork.LocalPlayer.NickName}");
    if (!PV.IsMine) return;

    kills++;
    Hashtable hash = new Hashtable { { "kills", kills } };
    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
    Debug.Log($"Updated kills for {PhotonNetwork.LocalPlayer.NickName}: {kills}");
}


 public static PlayerManager Find(Player player)
{
    Debug.Log($"Searching for PlayerManager for player {player.NickName}");
    var manager = FindObjectsOfType<PlayerManager>().ToList().SingleOrDefault(x => x.PV.Owner == player);
    if (manager == null)
    {
        Debug.LogError($"PlayerManager not found for {player.NickName}");
    }
    return manager;
}

public void ReportKill(Player killedPlayer)
{
    Debug.Log("ReportKill called");
    if (PV.IsMine)
    {
        var killedPlayerManager = Find(killedPlayer);
        if (killedPlayerManager != null)
        {
            Debug.Log($"Found PlayerManager for {killedPlayer.NickName}, invoking RecordDeath");
            killedPlayerManager.RecordDeath(PhotonNetwork.LocalPlayer);
        }
        else
        {
            Debug.LogError($"PlayerManager not found for {killedPlayer.NickName}");
        }
    }
    else
    {
        Debug.LogWarning("ReportKill called but PV.IsMine == false");
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
            // Retrieve team and set color based on it
            SpawnpointTDM.TeamColor playerTeam = tdmManager.GetPlayerTeam(PhotonNetwork.LocalPlayer);

            // Set color based on team
            if (playerTeam == SpawnpointTDM.TeamColor.Red)
            {
                color = Color.red;
            }
            else if (playerTeam == SpawnpointTDM.TeamColor.Blue)
            {
                color = Color.blue;
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

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TDMManager : MonoBehaviourPunCallbacks
{
    private int nextTeamIndex = 0; // Index for alternating team assignment
    private PhotonView pv;

    // Added kill counters for teams
    public int redTeamKills = 0;
    public int blueTeamKills = 0;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("[TDMManager] PhotonView is null in Awake! Ensure PhotonView is assigned to TDMManager object.");
        }
        else
        {
            Debug.Log("[TDMManager] PhotonView initialized correctly.");
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[TDMManager] Master Client assigning teams.");
            AssignTeamsToAllPlayers();
        }
        else
        {
            Debug.Log("[TDMManager] Not Master Client, waiting.");
        }
    }

    public void AssignTeamsToAllPlayers()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("PlayerTeam"))
            {
                AssignTeamToPlayer(player); // Assign team (Red or Blue)
            }
            else
            {
                Debug.Log($"[TDMManager] Player {player.NickName} already has team ({player.CustomProperties["PlayerTeam"]}).");
            }
        }
    }

    public void AssignTeamToPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogError("[TDMManager] Player is null!");
            return;
        }

        // Alternate assignment of Red and Blue teams
        SpawnpointTDM.TeamColor assignedTeam = nextTeamIndex % 2 == 0 ? SpawnpointTDM.TeamColor.Red : SpawnpointTDM.TeamColor.Blue;
        nextTeamIndex++;

        // Assign team to player's CustomProperties
        Hashtable playerProperties = new Hashtable { { "PlayerTeam", assignedTeam } };
        player.SetCustomProperties(playerProperties); // Synchronize team

        Debug.Log($"[TDMManager] Player {player.NickName} assigned team: {assignedTeam}");

        // Sync team to all clients
        pv.RPC("SyncPlayerTeam", RpcTarget.AllBuffered, player.ActorNumber, (int)assignedTeam);
    }

    [PunRPC]
    public void SyncPlayerTeam(int actorNumber, int team)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        if (player != null)
        {
            // Synchronize team on all clients
            SpawnpointTDM.TeamColor teamColor = (SpawnpointTDM.TeamColor)team;
            Hashtable playerProperties = new Hashtable { { "PlayerTeam", teamColor } };
            player.SetCustomProperties(playerProperties);

            Debug.Log($"[TDMManager] Synced team for player {player.NickName}: {teamColor}");
        }
        else
        {
            Debug.LogError($"[TDMManager] Player with ActorNumber {actorNumber} not found.");
        }
    }

    public SpawnpointTDM.TeamColor GetPlayerTeam(Player player)
    {
        if (player.CustomProperties.TryGetValue("PlayerTeam", out object teamObj))
        {
            return (SpawnpointTDM.TeamColor)teamObj;
        }

        // Return default team if player has none
        return SpawnpointTDM.TeamColor.Red;
    }

    // Methods for updating team kills
    public void AddKillToTeam(SpawnpointTDM.TeamColor team)
    {
        if (team == SpawnpointTDM.TeamColor.Red)
        {
            redTeamKills++;
        }
        else if (team == SpawnpointTDM.TeamColor.Blue)
        {
            blueTeamKills++;
        }

        // Synchronize scores across all clients
        pv.RPC("SyncTeamKills", RpcTarget.AllBuffered, redTeamKills, blueTeamKills);
    }

    [PunRPC]
    public void SyncTeamKills(int redKills, int blueKills)
    {
        redTeamKills = redKills;
        blueTeamKills = blueKills;

        Debug.Log($"[TDMManager] Synced kills: Red Team: {redTeamKills}, Blue Team: {blueTeamKills}");
    }
}

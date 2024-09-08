using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TDMManager : MonoBehaviourPunCallbacks
{
    private int nextTeamIndex = 0; // Indeks do przydzielania drużyn z naprzemiennym podziałem
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("[TDMManager] PhotonView jest null w Awake! Upewnij się, że PhotonView jest przypisany do obiektu TDMManager.");
        }
        else
        {
            Debug.Log("[TDMManager] PhotonView poprawnie zainicjalizowany.");
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[TDMManager] Master Client przypisuje drużyny.");
            AssignTeamsToAllPlayers();
        }
        else
        {
            Debug.Log("[TDMManager] Nie jestem Master Clientem, czekamy.");
        }
    }

    public void AssignTeamsToAllPlayers()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("PlayerTeam"))
            {
                AssignTeamToPlayer(player); // Przypisujemy drużynę (Red lub Blue)
            }
            else
            {
                Debug.Log($"[TDMManager] Gracz {player.NickName} już ma przypisaną drużynę ({player.CustomProperties["PlayerTeam"]}).");
            }
        }
    }

    public void AssignTeamToPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogError("[TDMManager] Gracz jest null!");
            return;
        }

        // Naprzemienne przypisywanie drużyn Red i Blue
        SpawnpointTDM.TeamColor assignedTeam = nextTeamIndex % 2 == 0 ? SpawnpointTDM.TeamColor.Red : SpawnpointTDM.TeamColor.Blue;
        nextTeamIndex++;

        // Przypisujemy drużynę do CustomProperties gracza
        Hashtable playerProperties = new Hashtable { { "PlayerTeam", assignedTeam } };
        player.SetCustomProperties(playerProperties); // Synchronizujemy drużynę

        Debug.Log($"[TDMManager] Gracz {player.NickName} otrzymał drużynę: {assignedTeam}");

        // Synchronizujemy drużynę do wszystkich klientów
        pv.RPC("SyncPlayerTeam", RpcTarget.AllBuffered, player.ActorNumber, (int)assignedTeam);
    }

    [PunRPC]
    public void SyncPlayerTeam(int actorNumber, int team)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        if (player != null)
        {
            // Synchronizujemy drużynę na wszystkich klientach
            SpawnpointTDM.TeamColor teamColor = (SpawnpointTDM.TeamColor)team;
            Hashtable playerProperties = new Hashtable { { "PlayerTeam", teamColor } };
            player.SetCustomProperties(playerProperties);

            Debug.Log($"[TDMManager] Zsynchronizowano drużynę gracza {player.NickName}: {teamColor}");
        }
        else
        {
            Debug.LogError($"[TDMManager] Nie znaleziono gracza z ActorNumber: {actorNumber}.");
        }
    }

    public SpawnpointTDM.TeamColor GetPlayerTeam(Player player)
    {
        if (player.CustomProperties.TryGetValue("PlayerTeam", out object teamObj))
        {
            return (SpawnpointTDM.TeamColor)teamObj;
        }

        // Zwracamy domyślną drużynę, jeśli gracz nie ma przypisanej drużyny
        return SpawnpointTDM.TeamColor.Red;
    }
}

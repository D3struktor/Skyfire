using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TDMManager : MonoBehaviourPunCallbacks
{
    public Color[] playerColors = new Color[20]; // Tablica z kolorami dla maksymalnie 20 graczy
    private int nextColorIndex = 0; // Indeks dla przydzielania kolejnych kolorów z listy
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

        // Zainicjalizuj kolory dla 20 graczy (możesz tutaj zmienić kolory na dowolne)
        DefinePlayerColors();
    }

    private void DefinePlayerColors()
    {
        // Dodajemy naprzemienne kolory dla graczy
        for (int i = 0; i < playerColors.Length; i++)
        {
            if (i % 2 == 0)
            {
                playerColors[i] = Color.red; // Parzyste indeksy na czerwono
            }
            else
            {
                playerColors[i] = Color.blue; // Nieparzyste na niebiesko
            }
        }
    }

    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[TDMManager] Master Client przypisuje kolory.");
            AssignColorsToAllPlayers();
        }
        else
        {
            Debug.Log("[TDMManager] Nie jestem Master Clientem, czekamy.");
        }
    }

    public void AssignColorsToAllPlayers()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("PlayerColor"))
            {
                AssignColorToPlayer(player); // Przypisujemy sztywny kolor z tablicy
            }
            else
            {
                Debug.Log($"[TDMManager] Gracz {player.NickName} już ma przypisany kolor ({player.CustomProperties["PlayerColor"]}).");
            }
        }
    }

    public void AssignColorToPlayer(Player player)
    {
        if (player == null)
        {
            Debug.LogError("[TDMManager] Gracz jest null!");
            return;
        }

        // Pobieramy kolor z listy, bazując na indeksie
        Color assignedColor = playerColors[nextColorIndex];
        nextColorIndex = (nextColorIndex + 1) % playerColors.Length; // Zapętlamy indeks po 20 graczach

        // Przypisujemy kolor do CustomProperties gracza
        Hashtable playerProperties = new Hashtable { { "PlayerColor", new Vector3(assignedColor.r, assignedColor.g, assignedColor.b) } };
        player.SetCustomProperties(playerProperties); // Synchronizujemy kolor

        Debug.Log($"[TDMManager] Gracz {player.NickName} otrzymał kolor: {assignedColor}");

        // Synchronizujemy kolor do wszystkich klientów
        pv.RPC("SyncPlayerColor", RpcTarget.AllBuffered, player.ActorNumber, assignedColor.r, assignedColor.g, assignedColor.b);
    }

    [PunRPC]
    public void SyncPlayerColor(int actorNumber, float r, float g, float b)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        if (player != null)
        {
            // Synchronizujemy kolor na wszystkich klientach
            Hashtable playerProperties = new Hashtable { { "PlayerColor", new Vector3(r, g, b) } };
            player.SetCustomProperties(playerProperties);

            Debug.Log($"[TDMManager] Zsynchronizowano kolor gracza {player.NickName}: {new Color(r, g, b)}");
        }
        else
        {
            Debug.LogError($"[TDMManager] Nie znaleziono gracza z ActorNumber: {actorNumber}.");
        }
    }

    public Color GetPlayerColor(Player player)
    {
        if (player.CustomProperties.TryGetValue("PlayerColor", out object colorObj))
        {
            Vector3 colorVector = (Vector3)colorObj;
            return new Color(colorVector.x, colorVector.y, colorVector.z);
        }

        return Color.white; // Kolor domyślny, jeśli gracz nie ma przypisanego koloru
    }
}

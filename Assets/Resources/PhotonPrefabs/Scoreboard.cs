using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

public class Scoreboard : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform container;
    [SerializeField] GameObject scoreboardItemPrefab;
    [SerializeField] CanvasGroup canvasGroup;

    Dictionary<Player, ScoreboardItem> scoreboardItems = new Dictionary<Player, ScoreboardItem>();

    void Start()
    {
        AddHeader();
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddScoreboardItem(player);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        AddScoreboardItem(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RemoveScoreboardItem(otherPlayer);
    }

    void AddScoreboardItem(Player player)
    {
        if (!scoreboardItems.ContainsKey(player)) // Check if the item already exists
        {
            ScoreboardItem item = Instantiate(scoreboardItemPrefab, container).GetComponent<ScoreboardItem>();
            item.Initialize(player);
            scoreboardItems[player] = item;
            Debug.Log($"Added scoreboard item for player {player.NickName}");
        }
    }

    void RemoveScoreboardItem(Player player)
    {
        if (scoreboardItems.ContainsKey(player)) // Check if the item exists
        {
            Destroy(scoreboardItems[player].gameObject);
            scoreboardItems.Remove(player);
            Debug.Log($"Removed scoreboard item for player {player.NickName}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            canvasGroup.alpha = 1;
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            canvasGroup.alpha = 0;
        }
    }
void AddHeader()
{
    // Sprawdzamy, czy prefab jest przypisany
    if (scoreboardItemPrefab == null || container == null)
    {
        Debug.LogError("ScoreboardItem prefab or container is not assigned!");
        return;
    }

    // Tworzymy nagłówek
    GameObject header = Instantiate(scoreboardItemPrefab, container);
    ScoreboardItem headerItem = header.GetComponent<ScoreboardItem>();

    // Ustawiamy dane dla nagłówka
    headerItem.Initialize(null, true); // Ustaw flagę 'isHeader' na true

    // Opcjonalnie dostosowujemy wygląd nagłówka
    headerItem.background.color = new Color(0, 0, 0, 0); // Transparentne tło
}



}

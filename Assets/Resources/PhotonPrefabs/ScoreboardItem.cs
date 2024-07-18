using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.UI;

public class ScoreboardItem : MonoBehaviourPunCallbacks
{
    public TMP_Text usernameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;
    public Image background; // Referencja do komponentu Image tła

    Player player;

    public void Initialize(Player player)
    {
        this.player = player;

        usernameText.text = player.NickName;
        UpdateStats();
        SetBackgroundColor();
    }

    void UpdateStats()
    {
        if (player.CustomProperties.TryGetValue("kills", out object kills))
        {
            killsText.text = kills.ToString();
        }
        else
        {
            killsText.text = "0"; // Ensure we have a default value
        }

        if (player.CustomProperties.TryGetValue("deaths", out object deaths))
        {
            deathsText.text = deaths.ToString();
        }
        else
        {
            deathsText.text = "0"; // Ensure we have a default value
        }

        Debug.Log($"Updated stats for {player.NickName}: Kills = {killsText.text}, Deaths = {deathsText.text}");
    }

    void SetBackgroundColor()
    {
        if (player.CustomProperties.TryGetValue("Team", out object team))
        {
            if (team.ToString() == "Red")
            {
                background.color = Color.red;
            }
            else if (team.ToString() == "Blue")
            {
                background.color = Color.blue;
            }
            else
            {
                background.color = new Color(0,0,0,0.25f); // Default color if no team
            }
        }
        else
        {
            background.color = new Color(0,0,0,0.25f); // Default color if no team
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == player)
        {
            if (changedProps.ContainsKey("kills") || changedProps.ContainsKey("deaths"))
            {
                UpdateStats();
            }

            if (changedProps.ContainsKey("Team"))
            {
                SetBackgroundColor();
            }
        }
    }
}

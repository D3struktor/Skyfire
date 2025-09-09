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
    public Image background; // Reference to the Image component for background
    public bool isHeader = false; // Flag indicating whether this is a header

    Player player;
    TDMManager tdmManager;

public void Initialize(Player player, bool isHeader = false)
{
    this.isHeader = isHeader;

    if (isHeader)
    {
        usernameText.text = "Nickname";
        killsText.text = "Kills";
        deathsText.text = "Deaths";
        Debug.Log($"[ScoreboardItem] Header set: {usernameText.text}, {killsText.text}, {deathsText.text}");
        return;
    }


    this.player = player;
    usernameText.text = player.NickName;
    UpdateStats();
    SetBackgroundColor();
}


void Start()
{

    
    // Find the TDMManager instance in the scene
    tdmManager = FindObjectOfType<TDMManager>();

    if (tdmManager == null)
    {
        Debug.LogWarning("[ScoreboardItem] TDMManager not found in the scene.");
    }

    if (!isHeader) // Clear stats ONLY for players, not for the header
    {
        ClearStats();
    }
    // Add delay before setting color and updating stats
    StartCoroutine(WaitAndSetColor(1.5f));
}

void ClearStats()
{
    // Reset displayed values for kills and deaths
    killsText.text = "0";
    deathsText.text = "0";
    Debug.Log("Statystyki zostały zresetowane.");
}


    IEnumerator WaitAndSetColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        SetBackgroundColor(); // Set background color after delay
    }


    void UpdateStats()
    {
        if (isHeader) return; // Ignore if this is the header
        if (player.CustomProperties.TryGetValue("kills", out object kills))
        {
            killsText.text = kills.ToString();
        }
        else
        {
            killsText.text = "0"; // Default if "kills" is missing
        }

        if (player.CustomProperties.TryGetValue("deaths", out object deaths))
        {
            deathsText.text = deaths.ToString();
        }
        else
        {
            deathsText.text = "0"; // Default if "deaths" is missing
        }

        Debug.Log($"[ScoreboardItem] Updated stats for {player.NickName}: Kills = {killsText.text}, Deaths = {deathsText.text}");
    }

void SetBackgroundColor()
{
    if (tdmManager != null && player.CustomProperties.TryGetValue("PlayerTeam", out object teamObj))
    {
        // Retrieve the player's team from CustomProperties
        SpawnpointTDM.TeamColor playerTeam = (SpawnpointTDM.TeamColor)teamObj;

        // Set color based on team
        Color playerColor = playerTeam == SpawnpointTDM.TeamColor.Red ? Color.red : Color.blue;
        background.color = playerColor;

        Debug.Log($"[ScoreboardItem] Set background color for {player.NickName} in team {playerTeam}: {playerColor}");
    }
    else
    {
        // Default color if team not found
        background.color = new Color(0, 0, 0, 0.25f); // Default color
        Debug.Log($"[ScoreboardItem] No color assigned for {player.NickName}, using default background color.");

        // Retry fetching the team after a short delay
        StartCoroutine(WaitAndSetColor());
    }
}


IEnumerator WaitAndSetColor()
{
    // Wait briefly (e.g., 0.5 seconds) and try again
    yield return new WaitForSeconds(0.5f);

    if (player.CustomProperties.TryGetValue("PlayerColor", out object colorObj))
    {
        Vector3 colorVector = (Vector3)colorObj;
        Color playerColor = new Color(colorVector.x, colorVector.y, colorVector.z);
        background.color = playerColor;

        Debug.Log($"[ScoreboardItem] Set background color again for {player.NickName}: {playerColor}");
    }
    else
    {
        Debug.Log($"[ScoreboardItem] Could not set background color again for {player.NickName}, kept default color.");
    }
}


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (isHeader) return; // Ignore if this is the header
        if (targetPlayer == player)
        {
            if (changedProps.ContainsKey("kills") || changedProps.ContainsKey("deaths"))
            {
                UpdateStats();
            }

            if (changedProps.ContainsKey("PlayerColor"))
            {
                SetBackgroundColor();
            }
        }
    }
}

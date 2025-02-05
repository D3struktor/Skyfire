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
    public bool isHeader = false; // Flaga określająca, czy to nagłówek

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
        Debug.Log($"[ScoreboardItem] Nagłówek ustawiony: {usernameText.text}, {killsText.text}, {deathsText.text}");
        return;
    }


    this.player = player;
    usernameText.text = player.NickName;
    UpdateStats();
    SetBackgroundColor();
}


void Start()
{

    
    // Znajdź instancję TDMManager na scenie
    tdmManager = FindObjectOfType<TDMManager>();

    if (tdmManager == null)
    {
        Debug.LogWarning("[ScoreboardItem] TDMManager not found in the scene.");
    }

    if (!isHeader) // Wyczyszczone statystyki TYLKO dla graczy, nie dla nagłówka
    {
        ClearStats();
    }
    // Dodaj opóźnienie 5 sekund przed ustawieniem koloru i aktualizacją statystyk
    StartCoroutine(WaitAndSetColor(1.5f));
}

void ClearStats()
{
    // Resetowanie wyświetlanych wartości dla zabójstw i zgonów
    killsText.text = "0";
    deathsText.text = "0";
    Debug.Log("Statystyki zostały zresetowane.");
}


    IEnumerator WaitAndSetColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        SetBackgroundColor(); // Ustaw kolor tła po 5 sekundach
    }


    void UpdateStats()
    {
        if (isHeader) return; // Ignoruj, jeśli to nagłówek
        if (player.CustomProperties.TryGetValue("kills", out object kills))
        {
            killsText.text = kills.ToString();
        }
        else
        {
            killsText.text = "0"; // Default value if "kills" is missing
        }

        if (player.CustomProperties.TryGetValue("deaths", out object deaths))
        {
            deathsText.text = deaths.ToString();
        }
        else
        {
            deathsText.text = "0"; // Default value if "deaths" is missing
        }

        Debug.Log($"[ScoreboardItem] Zaktualizowano statystyki dla {player.NickName}: Kills = {killsText.text}, Deaths = {deathsText.text}");
    }

void SetBackgroundColor()
{
    if (tdmManager != null && player.CustomProperties.TryGetValue("PlayerTeam", out object teamObj))
    {
        // Pobieramy drużynę gracza z CustomProperties
        SpawnpointTDM.TeamColor playerTeam = (SpawnpointTDM.TeamColor)teamObj;

        // Ustawiamy kolor w zależności od drużyny
        Color playerColor = playerTeam == SpawnpointTDM.TeamColor.Red ? Color.red : Color.blue;
        background.color = playerColor;

        Debug.Log($"[ScoreboardItem] Ustawiono kolor tła dla {player.NickName} w drużynie {playerTeam}: {playerColor}");
    }
    else
    {
        // Kolor domyślny, jeśli nie znaleziono drużyny
        background.color = new Color(0, 0, 0, 0.25f); // Kolor domyślny
        Debug.Log($"[ScoreboardItem] Brak przypisanego koloru dla gracza {player.NickName}, ustawiono domyślny kolor tła.");

        // Ponowna próba pobrania drużyny po krótkim czasie
        StartCoroutine(WaitAndSetColor());
    }
}


IEnumerator WaitAndSetColor()
{
    // Poczekaj chwilę (np. 0.5 sekundy) i spróbuj ponownie
    yield return new WaitForSeconds(0.5f);

    if (player.CustomProperties.TryGetValue("PlayerColor", out object colorObj))
    {
        Vector3 colorVector = (Vector3)colorObj;
        Color playerColor = new Color(colorVector.x, colorVector.y, colorVector.z);
        background.color = playerColor;

        Debug.Log($"[ScoreboardItem] Ponownie ustawiono kolor tła dla {player.NickName}: {playerColor}");
    }
    else
    {
        Debug.Log($"[ScoreboardItem] Ponownie nie udało się ustawić koloru tła dla {player.NickName}, zachowano domyślny kolor.");
    }
}


    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (isHeader) return; // Ignoruj, jeśli to nagłówek
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

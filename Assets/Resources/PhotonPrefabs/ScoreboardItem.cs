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
    TDMManager tdmManager;

    public void Initialize(Player player)
    {
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
        Debug.LogError("[ScoreboardItem] TDMManager not found in the scene.");
    }

    // Wyczyść początkowe statystyki
    ClearStats();

    // Dodaj opóźnienie 5 sekund przed ustawieniem koloru i aktualizacją statystyk
    StartCoroutine(WaitAndSetColor(5f));
}

void ClearStats()
{
    // Resetowanie wyświetlanych wartości dla zabójstw i zgonów
    killsText.text = "0";
    deathsText.text = "0";
}


    IEnumerator WaitAndSetColor(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        SetBackgroundColor(); // Ustaw kolor tła po 5 sekundach
    }


    void UpdateStats()
    {
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
        if (tdmManager != null && player.CustomProperties.TryGetValue("PlayerColor", out object colorObj))
        {
            // Pobierz kolor gracza z CustomProperties
            Vector3 colorVector = (Vector3)colorObj;
            Color playerColor = new Color(colorVector.x, colorVector.y, colorVector.z);
            background.color = playerColor;

            Debug.Log($"[ScoreboardItem] Ustawiono kolor tła dla {player.NickName}: {playerColor}");
        }
        else
        {
            background.color = new Color(0, 0, 0, 0.25f); // Kolor domyślny
            Debug.LogWarning($"[ScoreboardItem] Brak przypisanego koloru dla gracza {player.NickName}, ustawiono domyślny kolor tła.");
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

            if (changedProps.ContainsKey("PlayerColor"))
            {
                SetBackgroundColor();
            }
        }
    }
}

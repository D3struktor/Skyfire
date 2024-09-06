using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;

public class TimerManager : MonoBehaviourPunCallbacks
{
    public float matchDuration = 30f;
    public float bufferTime = 10f;
    public string menuSceneName = "Menu"; // Nazwa sceny menu
    // public GameObject scoreboard; // Referencja do scoreboardu UI
    public CanvasGroup scoreboardCanvasGroup; 

    private float currentTime;
    private bool isMatchActive = false;
    private TMP_Text timerText;

    void Start()
    {
        Debug.Log("TimerManager started");
        StartCoroutine(StartMatch());
    }

    IEnumerator StartMatch()
    {
        while (true)
        {
            yield return StartCoroutine(MatchCountdown());
            yield return StartCoroutine(BufferCountdown());
            yield return StartCoroutine(EndMatchAndLoadMenu());
        }
    }

    IEnumerator MatchCountdown()
    {
        Debug.Log("Match countdown started");
        isMatchActive = true;
        currentTime = matchDuration;
        FindTimerText();

        while (currentTime > 0)
        {
            UpdateTimerUI();
            yield return new WaitForSeconds(1f);
            currentTime--;
            Debug.Log("Match time remaining: " + currentTime);
        }

        isMatchActive = false;
        Debug.Log("Match ended!");
        EndMatch();
    }

    IEnumerator BufferCountdown()
    {
        Debug.Log("Buffer countdown started");
        currentTime = bufferTime;

        while (currentTime > 0)
        {
            UpdateTimerUI("Match ended, time remaining");
            yield return new WaitForSeconds(1f);
            currentTime--;
            Debug.Log("Buffer time remaining: " + currentTime);
        }

        Debug.Log("Buffer time ended!");
    }

    void UpdateTimerUI(string TextTemplate = "Time")
    {
        if (timerText != null)
        {
            timerText.text = TextTemplate + ": " + currentTime.ToString("F0");
            // Debug.Log("Timer updated: " + timerText.text);
        }
    }

void FindTimerText()
{
    if (timerText == null)
    {
        GameObject timerTextObject = GameObject.Find("TimerText"); // Zmień "TimerText" na dokładną nazwę obiektu w scenie
        if (timerTextObject != null)
        {
            timerText = timerTextObject.GetComponent<TMP_Text>();
            if (timerText != null)
            {
                Debug.Log("Timer Text found: " + timerText.name);
            }
            else
            {
                Debug.LogWarning("TMP_Text component not found on the object!");
            }
        }
        else
        {
            Debug.LogWarning("Timer Text object not found!");
        }
    }
}


    void EndMatch()
    {
        // Zatrzymaj ruch wszystkich graczy
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            player.EnableMovement(false);
        }

        // Wyświetl scoreboard
        if (scoreboardCanvasGroup != null)
        {
            scoreboardCanvasGroup.alpha = 1;
        }
        else
        {
            Debug.LogWarning("Scoreboard not assigned in TimerManager");
        }
    }

    IEnumerator EndMatchAndLoadMenu()
    {
        // Rozłącz z Photon
        PhotonNetwork.Disconnect();
        Debug.Log("Disconnecting from Photon...");

        // Czekaj aż zostaniesz rozłączony
        while (PhotonNetwork.IsConnected)
        {
            yield return null;
        }

        Debug.Log("Disconnected from Photon. Loading menu scene...");
        // Resetuj PhotonView ID
        ResetRoomManagerPhotonView();

        // Załaduj scenę menu
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);

        // Ustawienie kursora
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ResetRoomManagerPhotonView()
    {
        RoomManager roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            PhotonView photonView = roomManager.GetComponent<PhotonView>();
            if (photonView != null)
            {
                Debug.Log("Resetting RoomManager PhotonView ID");
                photonView.ViewID = 0;
            }
        }
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log("Disconnected from Photon: " + cause.ToString());
    }
}

using UnityEngine;
using Photon.Pun;
using TMPro;
using System.Collections;

public class TimerManager : MonoBehaviourPunCallbacks
{
    public float matchDuration = 30f;
    public float bufferTime = 10f;
    public string menuSceneName = "Menu"; // Nazwa sceny menu
    public CanvasGroup scoreboardCanvasGroup;

    private float currentTime;
    private bool isMatchActive = false;
    private TMP_Text timerText;

    public static bool isWarmup = false; // Flaga sygnalizująca, czy trwa warmup

    void Start()
    {
        Debug.Log("TimerManager started");

        // Sprawdzenie czy tryb gry to TDM
        if (PlayerPrefs.GetString("GameMode") == "TDM")
        {
            Debug.Log("Game mode is TDM, starting warmup.");
            StartCoroutine(WarmupPhase()); // Rozpoczynamy warmup
        }
        else
        {
            StartCoroutine(StartMatch());
        }
    }

    IEnumerator WarmupPhase()
    {
        isWarmup = true; // Warmup się zaczyna
        FindTimerText();
        currentTime = 1f; 

        while (currentTime > 0)
        {
            UpdateTimerUI("Warmup");
            yield return new WaitForSeconds(1f);
            currentTime--;
        }

        Debug.Log("Warmup finished, killing all players.");
        KillAllPlayersAtStart(); // Zabijamy wszystkich graczy bez liczenia śmierci do statystyk
        isWarmup = false; // Warmup zakończony
        StartCoroutine(StartMatch()); // Rozpoczynamy mecz po warmupie
    }

    void KillAllPlayersAtStart()
    {
        PlayerManager[] players = FindObjectsOfType<PlayerManager>();
        foreach (PlayerManager player in players)
        {
            if (player != null)
            {
                player.DieWithoutCountingDeath(); // Zabijamy gracza, ale nie liczymy śmierci
            }
        }
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
            UpdateTimerUI("Match ended");
            yield return new WaitForSeconds(1f);
            currentTime--;
            Debug.Log("Buffer time remaining: " + currentTime);
        }

        Debug.Log("Buffer time ended!");
    }

    void UpdateTimerUI(string textTemplate = "Time")
    {
        if (timerText != null)
        {
            timerText.text = textTemplate + ": " + currentTime.ToString("F0");
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
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (PlayerController player in players)
        {
            player.EnableMovement(false);
        }

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
        PhotonNetwork.Disconnect();
        Debug.Log("Disconnecting from Photon...");

        while (PhotonNetwork.IsConnected)
        {
            yield return null;
        }

        Debug.Log("Disconnected from Photon. Loading menu scene...");
        ResetRoomManagerPhotonView();

        UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneName);

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

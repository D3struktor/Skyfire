using UnityEngine;
using TMPro;  // Add this for TextMesh Pro
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using ExitGames.Client.Photon;  // Ensure you are using Photon Hashtable
using Hashtable = ExitGames.Client.Photon.Hashtable;  // Explicitly specify Photon Hashtable

public class TimerManager : MonoBehaviourPunCallbacks
{
    public static TimerManager Instance { get; private set; }

    [SerializeField] private TMP_Text timerText; // Reference to UI Text to display the timer
    [SerializeField] private GameObject scoreboardPrefab; // Reference to the Scoreboard prefab

    private float matchDuration = 60f; // Match duration in seconds
    private float timeRemaining;
    private bool matchStarted = false;
    private GameObject scoreboardUI; // Instance of the Scoreboard

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("TimerManager Instance created.");
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Duplicate TimerManager Instance destroyed.");
        }
    }

    public void SetTimerText(TMP_Text text)
    {
        timerText = text;
        Debug.Log("TimerText has been set in TimerManager.");
    }

    void Start()
    {
        if (timerText == null)
        {
            Debug.LogError("TimerText is not assigned!");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("This client is the MasterClient. Starting the match...");
            StartCoroutine(StartMatch());
        }
        else
        {
            Debug.Log("This client is not the MasterClient. Waiting for match to start...");
        }
    }

    IEnumerator StartMatch()
    {
        if (matchStarted) yield break; // Prevent multiple starts
        matchStarted = true;

        Debug.Log("Starting match in TimerManager...");

        // Wait for all players to be ready (optional)
        yield return new WaitForSeconds(3); // Wait for 3 seconds before starting the match

        Debug.Log("Calling RPC_StartMatch on all clients.");
        photonView.RPC("RPC_StartMatch", RpcTarget.AllBuffered, PhotonNetwork.Time);
    }

    [PunRPC]
    void RPC_StartMatch(double startTime)
    {
        Debug.Log("RPC_StartMatch called.");
        timeRemaining = matchDuration;
        matchStarted = true;
        StartCoroutine(UpdateTimer(startTime));
    }

    IEnumerator UpdateTimer(double startTime)
    {
        while (timeRemaining > 0)
        {
            double elapsed = PhotonNetwork.Time - startTime;
            timeRemaining = matchDuration - (float)elapsed;

            if (timerText != null)
            {
                timerText.text = FormatTime(timeRemaining);
            }

            yield return null;
        }

        EndMatch();
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void EndMatch()
    {
        Debug.Log("Match ended.");
        matchStarted = false;

        StartCoroutine(ShowScoreboardAndLoadMap());
    }

    IEnumerator ShowScoreboardAndLoadMap()
    {
        ShowScoreboard(); // Display the scoreboard
        DestroyAllPlayerControllers(); // Destroy all player controllers

        yield return new WaitForSeconds(5); // Wait for 5 seconds

        if (PhotonNetwork.IsMasterClient)
        {
            // Choose a random map between "Map1" and "Map2"
            string randomMap = (Random.Range(0, 2) == 0) ? "Map1" : "Map2";
            Debug.Log("Loading random map: " + randomMap);
            ClearStatistics(); // Clear player statistics before loading new map
            PhotonNetwork.LoadLevel(randomMap);
        }
    }

    void ShowScoreboard()
    {
        // Instantiate the scoreboard UI
        scoreboardUI = Instantiate(scoreboardPrefab);

        // Gather and display player statistics
        string stats = "Match Statistics\n";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int kills = player.CustomProperties.ContainsKey("kills") ? (int)player.CustomProperties["kills"] : 0;
            int deaths = player.CustomProperties.ContainsKey("deaths") ? (int)player.CustomProperties["deaths"] : 0;
            stats += $"{player.NickName}: Kills - {kills}, Deaths - {deaths}\n";
        }

        // Set the statistics text in the scoreboard
        TMP_Text statsText = scoreboardUI.GetComponentInChildren<TMP_Text>();
        if (statsText != null)
        {
            statsText.text = stats;
        }
    }

    void DestroyAllPlayerControllers()
    {
        foreach (GameObject playerController in GameObject.FindGameObjectsWithTag("PlayerController"))
        {
            PhotonNetwork.Destroy(playerController);
        }

        Debug.Log("All player controllers destroyed.");
    }

    void ClearStatistics()
    {
        // Reset kills and deaths for all players
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            Hashtable hash = new Hashtable();
            hash["kills"] = 0;
            hash["deaths"] = 0;
            player.SetCustomProperties(hash);
        }

        Debug.Log("Player statistics cleared.");
    }
}

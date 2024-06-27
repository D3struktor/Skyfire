using UnityEngine;
using TMPro;  // Add this for TextMesh Pro
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class TimerManager : MonoBehaviourPunCallbacks
{
    public static TimerManager Instance { get; private set; }

    [SerializeField] private TMP_Text timerText; // Reference to UI Text to display the timer
    private float matchDuration = 60f; // Match duration in seconds
    private float timeRemaining;
    private bool matchStarted = false;

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
        PhotonNetwork.LoadLevel("Menu"); // Load the main menu scene
    }
}

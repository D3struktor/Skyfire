using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    public static bool isPaused = false; // Czy gra jest w stanie pauzy
    public GameObject pauseMenuUI;       // Panel menu pauzy

    private PlayerController playerController;  // Referencja do PlayerController

    void Start()
    {
        // Znajdź PlayerController na początku gry
        if (PhotonNetwork.LocalPlayer != null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        // Upewnij się, że menu pauzy jest wyłączone na początku
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuUI is not assigned!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Ukryj menu pauzy
        }

        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked; // Ukryj kursor
        Cursor.visible = false;

        // Odblokuj ruch gracza
        if (playerController != null)
        {
            playerController.EnableMovement(true); // Włącz ruch gracza
        }
    }

    void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);  // Pokaż menu pauzy
        }

        isPaused = true;
        Cursor.lockState = CursorLockMode.None;  // Pokaż kursor
        Cursor.visible = true;

        // Zablokuj ruch gracza
        if (playerController != null)
        {
            playerController.EnableMovement(false); // Zablokuj ruch gracza
        }
    }

    // Wyjdź z pokoju i wróć do lobby
    public void ExitToLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();  // Wyjdź z pokoju
        }
    }

    // Callback, gdy gracz opuści pokój
    public override void OnLeftRoom()
    {
        // Przejdź do sceny Lobby, gdy pokój zostanie opuszczony
        SceneManager.LoadScene("Menu");
    }

    // Wyjdź z gry
    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();  // Wyjdź z gry
        #endif
    }
}

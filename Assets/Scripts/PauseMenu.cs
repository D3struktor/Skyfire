using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class PauseMenu : MonoBehaviourPunCallbacks
{
    public static bool isPaused = false;
    public GameObject pauseMenuUI;

    // Suwaki głośności
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer SFXMixer;

    private void Start()
    {
        // Upewnij się, że menu pauzy jest wyłączone na początku
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuUI is not assigned!");
        }

        // Inicjalizacja suwaków głośności z zapisanymi ustawieniami
        InitializeVolumeSliders();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Unpause();
            }
            else
            {
                Pause();
            }
        }
    }

    private void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Pokaż menu pauzy
        }

        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Unpause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Ukryj menu pauzy
        }

        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeVolumeSliders()
    {
        // Pobierz zapisaną głośność i ustaw suwaki
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // Ustawienie wartości suwaków i nasłuchiwanie zmian
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = savedSFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }
    }

    // Metoda do ustawiania głównej głośności
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20); // Ustaw głośność w AudioMixer
        PlayerPrefs.SetFloat("Volume", volume); // Zapisz głośność
    }

    // Metoda do ustawiania głośności efektów dźwiękowych
    public void SetSFXVolume(float volume)
    {
        SFXMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20); // Ustaw głośność SFX w AudioMixer
        PlayerPrefs.SetFloat("SFXVolume", volume); // Zapisz głośność SFX
    }

    public void ExitToLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game pressed. Quitting application.");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Dla edytora Unity
        #else
            Application.Quit(); // Prawdziwe wyjście z gry
        #endif
    }
}

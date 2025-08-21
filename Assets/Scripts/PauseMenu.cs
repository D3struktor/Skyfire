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

    // Volume sliders
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer SFXMixer;

    private void Start()
    {
        // Ensure pause menu is disabled at start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        else
        {
            Debug.LogError("PauseMenuUI is not assigned!");
        }

        // Initialize volume sliders with saved settings
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
            pauseMenuUI.SetActive(true); // Show pause menu
        }

        isPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Unpause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Hide pause menu
        }

        isPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeVolumeSliders()
    {
        // Get saved volume and set sliders
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        // Set slider values and listen for changes
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

    // Method for setting master volume
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20); // Set volume in AudioMixer
        PlayerPrefs.SetFloat("Volume", volume); // Save volume
    }

    // Method for setting sound effects volume
    public void SetSFXVolume(float volume)
    {
        SFXMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20); // Set SFX volume in AudioMixer
        PlayerPrefs.SetFloat("SFXVolume", volume); // Save SFX volume
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

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // For Unity Editor
        #else
            Application.Quit(); // For standalone builds
        #endif
    }

}

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioClip buttonClickSound;  // Dźwięk kliknięcia
    public float buttonClickVolume = 0.25f;  // Głośność dźwięku kliknięcia, zmień wartość na pożądaną

    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton pattern to make sure there's only one AudioManager
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // Utrzymuje AudioManager między scenami
        }
        else
        {
            Destroy(gameObject);  // Usuwa duplikaty AudioManagera
        }

        // Dodaje AudioSource dynamicznie, jeśli go nie ma
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }

    // Metoda odtwarzania dźwięku kliknięcia przycisku
    public void PlayButtonClickSound()
    {
        audioSource.volume = buttonClickVolume;  // Ustawia głośność dźwięku kliknięcia
        audioSource.PlayOneShot(buttonClickSound);
    }
}

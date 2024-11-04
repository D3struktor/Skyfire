using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class MedalDisplay : MonoBehaviour
{
    [SerializeField] private GameObject discShooterMedal;
    [SerializeField] private GameObject grenadeMedal;
    [SerializeField] private AudioClip discShooterMedalSound;
    [SerializeField] private AudioClip grenadeMedalSound;
    [SerializeField] private AudioMixerGroup sfxMixerGroup; // Mikser SFX
    private AudioSource audioSource;

    void Start()
    {
        // Inicjalizacja AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = sfxMixerGroup; // Przypisanie miksera SFX

        // Ukrycie medali na początku
        discShooterMedal.SetActive(false);
        grenadeMedal.SetActive(false);
    }

    public void ShowDiscShooterMedal()
    {
        discShooterMedal.SetActive(true);
        
        // Odtwarzanie dźwięku dla DiscShooterMedal
        if (discShooterMedalSound != null)
        {
            audioSource.PlayOneShot(discShooterMedalSound);
        }

        // Ukrycie medalu po 2 sekundach z efektem fade
        Invoke(nameof(HideDiscShooterMedal), 2f);
    }

    public void ShowGrenadeMedal()
    {
        grenadeMedal.SetActive(true);
        
        // Odtwarzanie dźwięku dla GrenadeMedal
        if (grenadeMedalSound != null)
        {
            audioSource.PlayOneShot(grenadeMedalSound);
        }

        // Ukrycie medalu po 2 sekundach z efektem fade
        Invoke(nameof(HideGrenadeMedal), 2f);
    }

    private void HideDiscShooterMedal()
    {
        StartCoroutine(FadeOut(discShooterMedal));
    }

    private void HideGrenadeMedal()
    {
        StartCoroutine(FadeOut(grenadeMedal));
    }

    private IEnumerator FadeOut(GameObject medal)
    {
        CanvasGroup canvasGroup = medal.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = medal.AddComponent<CanvasGroup>();
        }
        for (float t = 1f; t > 0; t -= Time.deltaTime / 2f)
        {
            canvasGroup.alpha = t;
            yield return null;
        }
        medal.SetActive(false);
    }
}

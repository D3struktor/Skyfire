using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeInOnStart : MonoBehaviour
{
    [SerializeField] private Image fadePanel;  // Panel image that darkens the screen
    [SerializeField] private float fadeDuration = 2f;  // Duration of fade effect
    [SerializeField] private float initialDelay = 1f;  // Time the screen stays fully black

    private void Start()
    {
        // Set initial state to fully black (alpha = 1)
        fadePanel.color = new Color(0, 0, 0, 1);

        // Start fading after the initial delay
        StartCoroutine(FadeIn());
    }

    // Coroutine to smoothly decrease alpha
    private IEnumerator FadeIn()
    {
        // Wait for the specified time with full black screen
        yield return new WaitForSeconds(initialDelay);

        float timeElapsed = 0f;

        // While the panel alpha is greater than 0, decrease it gradually
        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alphaValue = Mathf.Lerp(1, 0, timeElapsed / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, alphaValue);
            yield return null;
        }

        // Finally set alpha to 0 so the panel disappears completely
        fadePanel.color = new Color(0, 0, 0, 0);
        fadePanel.gameObject.SetActive(false);  // Disable panel after fade
    }
}

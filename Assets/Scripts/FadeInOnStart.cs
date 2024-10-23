using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeInOnStart : MonoBehaviour
{
    [SerializeField] private Image fadePanel;  // Obraz panelu, który przyciemnia ekran
    [SerializeField] private float fadeDuration = 2f;  // Czas trwania efektu wygaszania
    [SerializeField] private float initialDelay = 1f;  // Czas, przez jaki ekran jest całkowicie czarny

    private void Start()
    {
        // Ustaw początkowy stan na całkowicie czarny (alpha = 1)
        fadePanel.color = new Color(0, 0, 0, 1);

        // Rozpocznij wygaszanie po początkowym opóźnieniu
        StartCoroutine(FadeIn());
    }

    // Korutyna do płynnego zmniejszania alpha
    private IEnumerator FadeIn()
    {
        // Odczekaj ustalony czas z pełnym czarnym ekranem
        yield return new WaitForSeconds(initialDelay);

        float timeElapsed = 0f;

        // Dopóki alpha panelu jest większe niż 0, zmniejszaj ją stopniowo
        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alphaValue = Mathf.Lerp(1, 0, timeElapsed / fadeDuration);
            fadePanel.color = new Color(0, 0, 0, alphaValue);
            yield return null;
        }

        // Na koniec ustaw alpha na 0, aby panel całkowicie zniknął
        fadePanel.color = new Color(0, 0, 0, 0);
        fadePanel.gameObject.SetActive(false);  // Wyłącz panel po wygaszeniu
    }
}

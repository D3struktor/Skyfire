using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public Image crosshairImage; // Celownik w UI
    private Chaingun chaingun; // Odniesienie do skryptu Chaingun
    public float maxSpread = 50f; // Maksymalny rozrzut celownika
    public float minSpread = 10f; // Minimalny rozrzut celownika

    private RectTransform crosshairRectTransform;

    void Start()
    {
        crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
        crosshairImage.enabled = false; // Ukryj celownik na starcie
    }

    void Update()
    {
        UpdateCrosshair();
    }

    void UpdateCrosshair()
    {
        if (chaingun == null)
        {
            crosshairImage.enabled = false; // Ukryj celownik, jeśli chaingun nie jest wyekwipowany
            
            return;
        }

        crosshairImage.enabled = true;
     

        float heatRatio = chaingun.GetComponent<CoolingSystem>().currentHeat / chaingun.GetComponent<CoolingSystem>().maxHeat;
        float currentSpread = Mathf.Lerp(minSpread, maxSpread, heatRatio);

        crosshairRectTransform.sizeDelta = new Vector2(currentSpread, currentSpread);
    }

    public void SetChaingun(Chaingun newChaingun)
    {
        chaingun = newChaingun;
        crosshairImage.enabled = chaingun != null; // Pokaż celownik, jeśli chaingun jest ustawiony
    }
}

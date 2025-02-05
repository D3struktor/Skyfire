using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Obraz przycisku (komponent Image) – zostanie pobrany automatycznie, jeśli nie ustawiony.")]
    public Image targetImage;
    
    [Tooltip("Kolor przycisku w stanie normalnym.")]
    public Color normalColor = Color.white;
    
    [Tooltip("Kolor przycisku po najechaniu")]
    public Color hoverColor = new Color(1f, 1f, 1f, 1f);

    private void Awake()
    {
        // Pobranie komponentu Image, jeśli nie został ustawiony w Inspectorze
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    // Metoda wywoływana, gdy kursor wchodzi na obszar przycisku
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetImage.color = hoverColor;
    }

    // Metoda wywoływana, gdy kursor opuszcza obszar przycisku
    public void OnPointerExit(PointerEventData eventData)
    {
        targetImage.color = normalColor;
    }
}

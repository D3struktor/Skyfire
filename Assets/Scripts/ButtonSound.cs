using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerClickHandler
{
    // Play the button click sound when the button is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        // Play sound through the AudioManager
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayButtonClickSound();
        }
    }
}

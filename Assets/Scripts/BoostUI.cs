using UnityEngine;
using UnityEngine.UI;

public class BoostUI : MonoBehaviour
{
    public Image boostIcon; // Ikona boosta
    public AudioSource rechargeSound; // AudioSource dla dźwięku końca ładowania

    private float boostCooldown = 12.0f; // Czas ładowania w sekundach
    private float boostTimer = 0f;
    private bool isBoostAvailable = true;

    private Color defaultColor = Color.white; // Kolor początkowy
    private Color chargedColor = new Color(0.5f, 0.8f, 1f); // Jasno niebieski kolor dla naładowanej ikony

    void Start()
    {
        boostIcon.color = defaultColor; // Ustawienie początkowego koloru
    }

    void Update()
    {
        // Aktywacja boosta po wciśnięciu "E", gdy jest gotowy
        if (Input.GetKeyDown(KeyCode.E) && isBoostAvailable)
        {
            ActivateBoost();
        }

        // Aktualizacja ładowania ikony
        if (!isBoostAvailable)
        {
            boostTimer += Time.deltaTime;
            boostIcon.fillAmount = boostTimer / boostCooldown;

            // Gdy ładowanie zakończone
            if (boostTimer >= boostCooldown)
            {
                isBoostAvailable = true;
                boostTimer = 0f;
                boostIcon.fillAmount = 1f; // Ikona w pełni wypełniona
                boostIcon.color = chargedColor; // Zmiana koloru na jasno niebieski
                rechargeSound.Play(); // Odtwórz dźwięk ładowania
            }
        }
    }

    void ActivateBoost()
    {
        // Uruchomienie boosta, reset wypełnienia ikony i koloru
        isBoostAvailable = false;
        boostIcon.fillAmount = 0f;
        boostIcon.color = defaultColor; // Powrót do domyślnego koloru
    }
}

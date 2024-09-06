using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    public TextMeshProUGUI ammoText; // TextMeshPro do wyświetlania amunicji
    private PlayerAmmoManager playerAmmoManager; // Zarządzanie amunicją gracza
    private string currentWeapon;

    void Start()
    {
        // Znajdź PlayerAmmoManager przypisany do lokalnego gracza
        playerAmmoManager = FindObjectOfType<PlayerAmmoManager>();
        if (playerAmmoManager == null)
        {
            Debug.LogError("Brak komponentu PlayerAmmoManager.");
        }

        UpdateAmmoDisplay();
    }

    void Update()
    {
        UpdateAmmoDisplay(); // Zaktualizuj wyświetlanie amunicji w każdej klatce
    }

    public void SetCurrentWeapon(string weaponType)
    {
        currentWeapon = weaponType;
        UpdateAmmoDisplay(); // Zaktualizuj wyświetlanie amunicji przy zmianie broni
    }

    void UpdateAmmoDisplay()
    {
        if (playerAmmoManager != null && !string.IsNullOrEmpty(currentWeapon))
        {
            int currentAmmo = playerAmmoManager.GetAmmo(currentWeapon);
            ammoText.text = "Ammo: " + currentAmmo;
        }
        else
        {
            ammoText.text = "Ammo: 0";
        }
    }
}

using UnityEngine;

public class WeaponAmmo : MonoBehaviour
{
    // Początkowa maksymalna ilość amunicji dla danej broni
    public int maxAmmo = 10; 
    private int currentAmmo; // Aktualna ilość amunicji

    void Start()
    {
        // Inicjalizacja amunicji na początkową wartość
        currentAmmo = maxAmmo;
    }

    // Funkcja do użycia amunicji przy strzale
    public bool UseAmmo()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            return true; // Amunicja dostępna, strzał możliwy
        }
        return false; // Brak amunicji
    }

    // Funkcja do resetowania amunicji (np. po śmierci gracza)
    public void ResetAmmo()
    {
        currentAmmo = maxAmmo;
    }

    // Zwraca aktualną ilość amunicji
    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    // Ustawia aktualną ilość amunicji (przydatne po odrodzeniu lub innej akcji)
    public void SetCurrentAmmo(int ammo)
    {
        currentAmmo = ammo;
    }
}

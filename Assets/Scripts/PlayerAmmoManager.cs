using UnityEngine;

public class PlayerAmmoManager : MonoBehaviour
{
    public int discShooterAmmo = 10; // Ilość amunicji dla DiscShooter
    public int grenadeLauncherAmmo = 15; // Ilość amunicji dla GranadeLauncher
    public int chaingunAmmo = 100; // Ilość amunicji dla Chaingun

    // Pobiera amunicję dla danej broni
    public int GetAmmo(string weaponType)
    {
        switch (weaponType)
        {
            case "DiscShooter":
                return discShooterAmmo;
            case "GrenadeLauncher":
                return grenadeLauncherAmmo;
            case "Chaingun":
                return chaingunAmmo;
            default:
                return 0;
        }
    }

    // Zmniejsza amunicję o 1 po strzale, jeśli jest dostępna
    public bool UseAmmo(string weaponType)
    {
        switch (weaponType)
        {
            case "DiscShooter":
                if (discShooterAmmo > 0)
                {
                    discShooterAmmo--;
                    return true;
                }
                break;
            case "GrenadeLauncher":
                if (grenadeLauncherAmmo > 0)
                {
                    grenadeLauncherAmmo--;
                    return true;
                }
                break;
            case "Chaingun":
                if (chaingunAmmo > 0)
                {
                    chaingunAmmo--;
                    return true;
                }
                break;
        }
        return false; // Brak amunicji
    }

    // Resetuje amunicję po śmierci gracza lub odrodzeniu
    public void ResetAmmo()
    {
        discShooterAmmo = 10;
        grenadeLauncherAmmo = 15;
        chaingunAmmo = 100;
    }
}

using UnityEngine;

public class PlayerAmmoManager : MonoBehaviour
{
    public int discShooterAmmo = 10; // Ilość amunicji dla DiscShooter
    public int grenadeLauncherAmmo = 15; // Ilość amunicji dla GranadeLauncher
    public int chaingunAmmo = 100; // Ilość amunicji dla Chaingun

    // Maksymalne wartości amunicji
    private int maxDiscShooterAmmo = 10;
    private int maxGrenadeLauncherAmmo = 15;
    private int maxChaingunAmmo = 100;

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
        discShooterAmmo = maxDiscShooterAmmo;
        grenadeLauncherAmmo = maxGrenadeLauncherAmmo;
        chaingunAmmo = maxChaingunAmmo;
    }

    // Odnawia amunicję dla wszystkich broni, ale nie przekracza maksymalnych wartości
    public void RestoreAmmo(float percentage)
    {
        // Odnawiamy 30% maksymalnej amunicji dla każdej broni, ale nie przekraczamy maksymalnej wartości
        discShooterAmmo = Mathf.Min(discShooterAmmo + Mathf.RoundToInt(maxDiscShooterAmmo * percentage), maxDiscShooterAmmo);
        grenadeLauncherAmmo = Mathf.Min(grenadeLauncherAmmo + Mathf.RoundToInt(maxGrenadeLauncherAmmo * percentage), maxGrenadeLauncherAmmo);
        chaingunAmmo = Mathf.Min(chaingunAmmo + Mathf.RoundToInt(maxChaingunAmmo * percentage), maxChaingunAmmo);

        Debug.Log("Amunicja odnowiona: DiscShooter: " + discShooterAmmo + "/" + maxDiscShooterAmmo +
                  ", GrenadeLauncher: " + grenadeLauncherAmmo + "/" + maxGrenadeLauncherAmmo +
                  ", Chaingun: " + chaingunAmmo + "/" + maxChaingunAmmo);
    }
}

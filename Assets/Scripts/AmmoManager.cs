using UnityEngine;

public class AmmoManager : MonoBehaviour
{
    public int discShooterInitialAmmo = 10;
    public int grenadeLauncherInitialAmmo = 15;
    public int chaingunInitialAmmo = 100;

    private int discShooterAmmo;
    private int grenadeLauncherAmmo;
    private int chaingunAmmo;

    void Start()
    {
        ResetAmmo();
    }

    public void ResetAmmo()
    {
        discShooterAmmo = discShooterInitialAmmo;
        grenadeLauncherAmmo = grenadeLauncherInitialAmmo;
        chaingunAmmo = chaingunInitialAmmo;
    }

    public int GetAmmo(string weaponType)
    {
        switch (weaponType)
        {
            case "DiscShooter":
                Debug.Log("DiscShooter Ammo: " + discShooterAmmo); // Logowanie
                return discShooterAmmo;
            case "GrenadeLauncher":
                Debug.Log("GrenadeLauncher Ammo: " + grenadeLauncherAmmo); // Logowanie
                return grenadeLauncherAmmo;
            case "Chaingun":
                Debug.Log("Chaingun Ammo: " + chaingunAmmo); // Logowanie
                return chaingunAmmo;
            default:
                return 0;
        }
    }

    public bool UseAmmo(string weaponType)
    {
        switch (weaponType)
        {
            case "DiscShooter":
                if (discShooterAmmo > 0)
                {
                    discShooterAmmo--;
                    Debug.Log("DiscShooter Ammo Used, Remaining: " + discShooterAmmo); // Logowanie
                    return true;
                }
                break;
            case "GrenadeLauncher":
                if (grenadeLauncherAmmo > 0)
                {
                    grenadeLauncherAmmo--;
                    Debug.Log("GrenadeLauncher Ammo Used, Remaining: " + grenadeLauncherAmmo); // Logowanie
                    return true;
                }
                break;
            case "Chaingun":
                if (chaingunAmmo > 0)
                {
                    chaingunAmmo--;
                    Debug.Log("Chaingun Ammo Used, Remaining: " + chaingunAmmo); // Logowanie
                    return true;
                }
                break;
        }
        return false;
    }
}

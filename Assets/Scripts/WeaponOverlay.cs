using UnityEngine;
using UnityEngine.UI;

public class WeaponOverlay : MonoBehaviour
{
    public Image[] weaponIcons; // Ikony broni w kolejności
    private int activeWeaponIndex = 0; // Domyślnie pierwsza broń jest aktywna

    private Color inactiveColor = new Color(1, 1, 1, 0.3f); // 30% alfa dla nieaktywnych broni
    private Color activeColor = Color.white; // Pełna widoczność dla aktywnej broni

    void Start()
    {
        SetActiveWeapon(0); // Ustawienie pierwszej broni jako aktywnej na start
    }

    void Update()
    {
        HandleWeaponSwitch(); // Sprawdzaj zmiany broni przy każdym odświeżeniu
    }

    private void HandleWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetActiveWeapon(0); // Zmień na broń 1
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetActiveWeapon(1); // Zmień na broń 2
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetActiveWeapon(2); // Zmień na broń 3
        }
    }

    public void SetActiveWeapon(int index)
    {
        if (index < 0 || index >= weaponIcons.Length) return; // Sprawdzenie poprawności indeksu

        activeWeaponIndex = index;
        UpdateWeaponIcons();
    }

    private void UpdateWeaponIcons()
    {
        for (int i = 0; i < weaponIcons.Length; i++)
        {
            if (i == activeWeaponIndex)
            {
                // Ustawienie pełnej widoczności dla aktywnej broni
                weaponIcons[i].color = activeColor;
            }
            else
            {
                // Ustawienie 30% alfa dla nieaktywnych broni
                weaponIcons[i].color = inactiveColor;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

public class BoostUI : MonoBehaviour
{
    public Image boostIcon; // Boost icon
    public AudioSource rechargeSound; // AudioSource for recharge sound

    private float boostCooldown = 12.0f; // Recharge time in seconds
    private float boostTimer = 0f;
    private bool isBoostAvailable = true;

    private Color defaultColor = Color.white; // Initial color
    private Color chargedColor = new Color(0.5f, 0.8f, 1f); // Light blue for fully charged icon

    void Start()
    {
        boostIcon.color = defaultColor; // Set initial color
    }

    void Update()
    {
        // Activate boost when "E" is pressed and it is ready
        if (Input.GetKeyDown(KeyCode.E) && isBoostAvailable)
        {
            ActivateBoost();
        }

        // Update icon charging
        if (!isBoostAvailable)
        {
            boostTimer += Time.deltaTime;
            boostIcon.fillAmount = boostTimer / boostCooldown;

            // When charging is complete
            if (boostTimer >= boostCooldown)
            {
                isBoostAvailable = true;
                boostTimer = 0f;
                boostIcon.fillAmount = 1f; // Icon fully filled
                boostIcon.color = chargedColor; // Change color to light blue
                rechargeSound.Play(); // Play recharge sound
            }
        }
    }

    void ActivateBoost()
    {
        // Activate boost, reset icon fill and color
        isBoostAvailable = false;
        boostIcon.fillAmount = 0f;
        boostIcon.color = defaultColor; // Return to default color
    }
}

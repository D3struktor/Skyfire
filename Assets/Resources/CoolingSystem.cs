using UnityEngine;

public class CoolingSystem : MonoBehaviour
{
    public float maxHeat = 100f;
    public float heatIncreaseRate = 10f;
    public float coolRate = 5f;
    public float movementCoolingMultiplier = 1.5f;

    public float currentHeat { get; set; } // Dodanie publicznego settera

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        currentHeat = 0f;
        Debug.Log("CoolingSystem started, initial heat: " + currentHeat);
    }

    void Update()
    {
        if (currentHeat > 0)
        {
            float coolingRate = coolRate;

            if (playerController != null)
            {
                float playerSpeed = playerController.GetPlayerSpeed();
                Debug.Log("Player speed: " + playerSpeed);

                if (playerSpeed > 0)
                {
                    coolingRate *= movementCoolingMultiplier;
                }
            }

            Debug.Log("Cooling rate before adjustment: " + coolRate);
            Debug.Log("Cooling rate after adjustment: " + coolingRate);
            
            currentHeat -= coolingRate * Time.deltaTime;
            currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);

            Debug.Log("Current heat after cooling: " + currentHeat);
        }
    }

    public void IncreaseHeat()
    {
        currentHeat += heatIncreaseRate;
        currentHeat = Mathf.Clamp(currentHeat, 0, maxHeat);
        Debug.Log("Heat increased, current heat: " + currentHeat);
    }
}

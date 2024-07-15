using UnityEngine;
using Photon.Pun;

public class CoolingSystem : MonoBehaviourPunCallbacks
{
    public float coolingRate = 5f;
    public float heatingRate = 10f;
    public float maxHeat = 100f;
    public float currentHeat = 0f;
    public float movementCoolingBonus = 2f;

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController not found on parent object.");
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            HandleCooling();
        }
    }

    public void IncreaseHeat()
    {
        currentHeat += heatingRate * Time.deltaTime;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);
    }

    private void HandleCooling()
    {
        float coolingModifier = playerController != null && playerController.GetPlayerSpeed() > 0 ? movementCoolingBonus : 1f;
        currentHeat -= coolingRate * coolingModifier * Time.deltaTime;
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        // Debugowanie currentHeat
        Debug.Log("Current Heat (CoolingSystem): " + currentHeat);
    }
}

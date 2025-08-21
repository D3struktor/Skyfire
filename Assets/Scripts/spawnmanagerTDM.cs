using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class SpawnManagerTDM : MonoBehaviour
{
    private SpawnpointTDM[] spawnpoints; // All available spawn points in TDM
    private TDMManager tdmManager;

    void Start()
    {
        // Get all SpawnpointTDM components from child objects
        spawnpoints = GetComponentsInChildren<SpawnpointTDM>();

        // Find TDMManager in the scene
        tdmManager = FindObjectOfType<TDMManager>();

        if (tdmManager == null)
        {
            Debug.LogError("TDMManager not found in the scene!");
        }
    }

    // Find appropriate spawn point based on player's team
    public Transform GetSpawnPoint(Player player)
    {
        // Get player's team (TeamColor) from TDMManager
        SpawnpointTDM.TeamColor playerTeam = tdmManager.GetPlayerTeam(player); // Use TeamColor, not Color

        // Filter spawn points based on player's team (Red or Blue)
        var availableSpawnPoints = spawnpoints.Where(sp => sp.teamColor == playerTeam).ToArray();

        if (availableSpawnPoints.Length == 0)
        {
            Debug.LogError($"No respawn points found for team: {playerTeam}");
            return null;
        }

        // Choose a random respawn point from available ones
        int randomIndex = Random.Range(0, availableSpawnPoints.Length);
        return availableSpawnPoints[randomIndex].transform;
    }

    // Respawn player at appropriate point
    public void RespawnPlayer(Player player)
    {
        Transform spawnPoint = GetSpawnPoint(player);

        if (spawnPoint != null)
        {
            // Spawn the player at chosen respawn point
            PhotonNetwork.Instantiate("PlayerPrefab", spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("No available respawn points. Cannot respawn player.");
        }
    }
}

using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class SpawnManagerTDM : MonoBehaviour
{
    private SpawnpointTDM[] spawnpoints; // Wszystkie dostępne punkty spawn w TDM
    private TDMManager tdmManager;

    void Start()
    {
        // Pobierz wszystkie komponenty typu SpawnpointTDM z obiektów dzieci
        spawnpoints = GetComponentsInChildren<SpawnpointTDM>();

        // Znajdź TDMManager w scenie
        tdmManager = FindObjectOfType<TDMManager>();

        if (tdmManager == null)
        {
            Debug.LogError("TDMManager nie został znaleziony w scenie!");
        }
    }

    // Funkcja do znalezienia odpowiedniego punktu spawn na podstawie drużyny gracza
    public Transform GetSpawnPoint(Player player)
    {
        // Pobierz drużynę gracza (TeamColor) z TDMManager
        SpawnpointTDM.TeamColor playerTeam = tdmManager.GetPlayerTeam(player); // Używamy TeamColor, a nie Color

        // Filtruj punkty respawn zależnie od drużyny gracza (Red lub Blue)
        var availableSpawnPoints = spawnpoints.Where(sp => sp.teamColor == playerTeam).ToArray();

        if (availableSpawnPoints.Length == 0)
        {
            Debug.LogError($"Nie znaleziono punktów respawn dla drużyny: {playerTeam}");
            return null;
        }

        // Wybierz losowy punkt respawn z dostępnych
        int randomIndex = Random.Range(0, availableSpawnPoints.Length);
        return availableSpawnPoints[randomIndex].transform;
    }

    // Respawn gracza w odpowiednim punkcie
    public void RespawnPlayer(Player player)
    {
        Transform spawnPoint = GetSpawnPoint(player);

        if (spawnPoint != null)
        {
            // Tworzymy gracza w wybranym punkcie respawn
            PhotonNetwork.Instantiate("PlayerPrefab", spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError("Brak dostępnych punktów respawn. Nie można odrodzić gracza.");
        }
    }
}

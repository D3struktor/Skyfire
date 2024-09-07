using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // TextMeshPro for better text rendering
using Photon.Pun;

public class KillFeedManager : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform feedContainer;      // Gdzie będą dodawane nowe wpisy
    [SerializeField] GameObject killFeedItemPrefab; // Prefab dla pojedynczego wpisu
    [SerializeField] float entryLifetime = 5f;     // Jak długo wpis będzie widoczny (np. 5 sekund)

    public static KillFeedManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Funkcja, która wywoływana jest po zabójstwie
    public void AddKillFeedEntry(string killerName, string victimName)
    {
        // Instancjonowanie nowego wpisu w panelu kill feeda
        GameObject entry = Instantiate(killFeedItemPrefab, feedContainer);

        // Pobieranie komponentu TextMeshPro do aktualizacji tekstu
        TMP_Text entryText = entry.GetComponent<TMP_Text>();
        entryText.text = $"{killerName} killed {victimName}";

        // Automatyczne usunięcie wpisu po pewnym czasie
        StartCoroutine(RemoveAfterDelay(entry, entryLifetime));
    }

    // Korutyna, która usuwa wpis po określonym czasie
    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(entry);
    }
}

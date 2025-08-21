using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // TextMeshPro for better text rendering
using Photon.Pun;

public class KillFeedManager : MonoBehaviourPunCallbacks
{
    [SerializeField] Transform feedContainer;      // Container for new entries
    [SerializeField] GameObject killFeedItemPrefab; // Prefab for a single entry
    [SerializeField] float entryLifetime = 5f;     // How long an entry remains visible (e.g., 5 seconds)

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

    // Function called after a kill
    public void AddKillFeedEntry(string killerName, string victimName)
    {
        // Instantiate new entry in the kill feed panel
        GameObject entry = Instantiate(killFeedItemPrefab, feedContainer);

        // Get TextMeshPro component to update text
        TMP_Text entryText = entry.GetComponent<TMP_Text>();
        entryText.text = $"{killerName} killed {victimName}";

        // Automatically remove entry after some time
        StartCoroutine(RemoveAfterDelay(entry, entryLifetime));
    }

    // Coroutine that removes entry after a delay
    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(entry);
    }
}

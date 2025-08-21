using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TeamKillDisplay : MonoBehaviour
{
    public TextMeshProUGUI redTeamText;        // Text for the red team (TextMesh Pro)
    public TextMeshProUGUI blueTeamText;       // Text for the blue team (TextMesh Pro)
    public Image redTeamBackground;            // Background for the red team text
    public Image blueTeamBackground;           // Background for the blue team text

    private TDMManager tdmManager;

    void Start()
    {
        tdmManager = FindObjectOfType<TDMManager>();
        if (tdmManager == null)
        {
            Debug.LogError("TDMManager not found in the scene!");
        }

        // Optionally set background colors
        redTeamBackground.color = new Color(1f, 0f, 0f, 0.5f);  // Red, semi-transparent
        blueTeamBackground.color = new Color(0f, 0f, 1f, 0.5f); // Blue, semi-transparent
    }

    void Update()
    {
        if (tdmManager != null)
        {
            // Update the red and blue team frag counts
            redTeamText.text = $"{tdmManager.redTeamKills}";
            blueTeamText.text = $"{tdmManager.blueTeamKills}";
        }
    }
}

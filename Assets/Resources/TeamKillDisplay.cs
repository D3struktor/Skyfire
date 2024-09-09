using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TeamKillDisplay : MonoBehaviour
{
    public TextMeshProUGUI redTeamText;        // Tekst dla drużyny czerwonej (TextMesh Pro)
    public TextMeshProUGUI blueTeamText;       // Tekst dla drużyny niebieskiej (TextMesh Pro)
    public Image redTeamBackground;            // Tło dla tekstu czerwonej drużyny
    public Image blueTeamBackground;           // Tło dla tekstu niebieskiej drużyny

    private TDMManager tdmManager;

    void Start()
    {
        tdmManager = FindObjectOfType<TDMManager>();
        if (tdmManager == null)
        {
            Debug.LogError("TDMManager not found in the scene!");
        }

        // Opcjonalnie można ustawić kolory tła
        redTeamBackground.color = new Color(1f, 0f, 0f, 0.5f);  // Czerwony, półprzezroczysty
        blueTeamBackground.color = new Color(0f, 0f, 1f, 0.5f); // Niebieski, półprzezroczysty
    }

    void Update()
    {
        if (tdmManager != null)
        {
            // Aktualizacja liczby fragów drużyny czerwonej i niebieskiej
            redTeamText.text = $"{tdmManager.redTeamKills}";
            blueTeamText.text = $"{tdmManager.blueTeamKills}";
        }
    }
}

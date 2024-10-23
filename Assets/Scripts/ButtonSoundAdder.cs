using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class ButtonSoundAdder : MonoBehaviour
{
    [MenuItem("Tools/Add ButtonSound To All Buttons")]
    public static void AddButtonSoundToAllButtons()
    {
        // Find all Button components in the scene
        Button[] buttons = FindObjectsOfType<Button>();

        foreach (Button button in buttons)
        {
            // Add ButtonSound script to each button if it's not already added
            if (button.GetComponent<ButtonSound>() == null)
            {
                button.gameObject.AddComponent<ButtonSound>();
            }
        }

        Debug.Log("Added ButtonSound to " + buttons.Length + " buttons.");
    }
}

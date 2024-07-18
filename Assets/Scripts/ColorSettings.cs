using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorSettings", menuName = "Settings/ColorSettings")]
public class ColorSettings : ScriptableObject
{
    public List<ColorEntry> colors;

    [System.Serializable]
    public class ColorEntry
    {
        public int id;
        public Color color;
    }

    public Color GetColorById(int id)
    {
        foreach (var colorEntry in colors)
        {
            if (colorEntry.id == id)
            {
                return colorEntry.color;
            }
        }
        return Color.white; // Default color if ID not found
    }

    public int GetIdByColor(Color color)
    {
        foreach (var colorEntry in colors)
        {
            if (colorEntry.color == color)
            {
                return colorEntry.id;
            }
        }
        return -1; // Return -1 if color not found
    }
}

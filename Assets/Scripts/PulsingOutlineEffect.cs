using UnityEngine;
using UnityEngine.UI;

public class PulsingOutlineEffect : MonoBehaviour
{
    public Image outlineImage; // Przypisz duplikat obrazu broni z konturem
    public float pulseSpeed = 1.0f;
    public Color outlineColor = Color.black;

    void Start()
    {
        outlineImage.color = outlineColor;
    }

    void Update()
    {
        float alpha = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
        outlineImage.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
    }
}

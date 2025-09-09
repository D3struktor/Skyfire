using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnpointTDM : MonoBehaviour
{
    // Enum for team colors
    public enum TeamColor
    {
        Red,
        Blue
    }

    // Public enum variable that will be shown as a dropdown in the Unity editor
    [SerializeField] public TeamColor teamColor; 

    [SerializeField] private GameObject graphics;

    void Awake()
    {
        graphics.SetActive(false);
    }
}

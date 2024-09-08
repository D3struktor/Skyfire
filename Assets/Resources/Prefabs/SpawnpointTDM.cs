using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnpointTDM : MonoBehaviour
{
    // Enum dla kolorów drużyn
    public enum TeamColor
    {
        Red,
        Blue
    }

    // Publiczna zmienna typu enum, która będzie wyświetlana jako rozwijana lista w edytorze Unity
    [SerializeField] public TeamColor teamColor; 

    [SerializeField] private GameObject graphics;

    void Awake()
    {
        graphics.SetActive(false);
    }
}

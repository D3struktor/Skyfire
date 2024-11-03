using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float lifetime = 5f; // Czas życia ragdolla w sekundach

    void Start()
    {
        // Zniszcz ten obiekt po upływie czasu określonego w `lifetime`
        Destroy(gameObject, lifetime);
    }
}

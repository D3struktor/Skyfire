using UnityEngine;

public class ParallaxClouds : MonoBehaviour
{
    public Vector2 offsetMultiplier = new Vector2(0.1f, 0.1f);  // Multiplier for parallax movement
    public float smoothTime = 0.5f;  // Smoothness of the movement

    private Vector3 velocity = Vector3.zero;
    private Vector3 startPosition;

    void Start()
    {
        // Zapisanie początkowej pozycji obiektu
        startPosition = transform.position;
    }

    void Update()
    {
        // Pobierz pozycję kursora w jednostkach Viewport (0 do 1)
        Vector2 offset = Camera.main.ScreenToViewportPoint(Input.mousePosition);

        // Przesuwaj chmury w zależności od pozycji kursora z płynnością
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            startPosition + (Vector3)(offset * offsetMultiplier), 
            ref velocity, 
            smoothTime
        );
    }
}

using UnityEngine;

public class FpsControllerInput : MonoBehaviour
{
    // Pobierz osie ruchu (Vertical dla przód/tył i Horizontal dla prawo/lewo)
    public Vector2 GetMoveAxis()
    {
        return new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")).normalized;
    }

    // Pobierz osie myszy (X dla obrót w poziomie, Y dla obrót w pionie)
    public Vector2 GetMouseAxis()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    // Sprawdzenie, czy wciśnięto klawisz sprintu
    public bool GetSprint()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    // Sprawdzenie, czy wciśnięto klawisz skoku
    public bool Jump()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}

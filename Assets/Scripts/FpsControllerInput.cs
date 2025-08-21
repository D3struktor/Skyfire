using UnityEngine;

public class FpsControllerInput : MonoBehaviour
{
    // Get movement axes (Vertical for forward/backward and Horizontal for right/left)
    public Vector2 GetMoveAxis()
    {
        return new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")).normalized;
    }

    // Get mouse axes (X for horizontal rotation, Y for vertical rotation)
    public Vector2 GetMouseAxis()
    {
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    // Check whether the sprint key is pressed
    public bool GetSprint()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    // Check whether the jump key is pressed
    public bool Jump()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}

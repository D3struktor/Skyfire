using UnityEngine;
using Photon.Pun;

public class TextureVisibility : MonoBehaviour
{
    public float visibilityDistance = 5.0f; // Maximum distance at which the texture is visible
    public float transitionSpeed = 2.0f; // Speed of the transition effect
    private Renderer objectRenderer; // Reference to the object's renderer
    private Transform player; // Reference to the local player
    private Material objectMaterial; // Reference to the object's material
    private Color originalColor; // Original color of the material
    private float targetAlpha; // Target alpha for the material

    private void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("Renderer component not found on the object.");
            return;
        }

        objectMaterial = objectRenderer.material;
        if (objectMaterial == null)
        {
            // Debug.LogError("Material component not found on the object.");
            return;
        }

        // Ensure the material uses a shader that supports transparency
        objectMaterial.shader = Shader.Find("Standard");
        objectMaterial.SetFloat("_Mode", 2);
        objectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        objectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        objectMaterial.SetInt("_ZWrite", 0);
        objectMaterial.DisableKeyword("_ALPHATEST_ON");
        objectMaterial.EnableKeyword("_ALPHABLEND_ON");
        objectMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        objectMaterial.renderQueue = 3000;

        originalColor = objectMaterial.color;
        targetAlpha = originalColor.a;

        // Find the local player using Photon
        FindLocalPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            FindLocalPlayer();
            if (player == null)
            {
                // Debug.LogError("Local player not found.");
                return;
            }
        }

        if (objectRenderer == null || objectMaterial == null)
        {
            // Debug.LogError("Renderer or material component is missing.");
            return;
        }

        // Get the closest point on the bounds of the renderer to the player
        Vector3 closestPoint = objectRenderer.bounds.ClosestPoint(player.position);
        float distance = Vector3.Distance(player.position, closestPoint);

        // Debug.Log("Player position: " + player.position);
        // Debug.Log("Closest point on texture: " + closestPoint);
        // Debug.Log("Distance to player: " + distance);

        if (distance <= visibilityDistance)
        {
            targetAlpha = 0.2f; // Fully visible
        }
        else
        {
            targetAlpha = 0.0f; // Fully hidden
        }

        // Smoothly transition the alpha value
        float currentAlpha = Mathf.Lerp(objectMaterial.color.a, targetAlpha, Time.deltaTime * transitionSpeed);
        
        // Update color to be more red as it becomes more visible
        float redComponent = Mathf.Lerp(originalColor.r, 1.0f, currentAlpha);
        float greenComponent = Mathf.Lerp(originalColor.g, 0.0f, currentAlpha);
        float blueComponent = Mathf.Lerp(originalColor.b, 0.0f, currentAlpha);

        Color newColor = new Color(redComponent, greenComponent, blueComponent, currentAlpha);
        objectMaterial.color = newColor;
    }

    private void FindLocalPlayer()
    {
        foreach (var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (playerObj.GetComponent<PhotonView>().IsMine)
            {
                player = playerObj.transform;
                // Debug.Log("Local player found: " + player.name);
                break;
            }
        }
    }
}

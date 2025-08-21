using UnityEngine;
using UnityEngine.UI;

public class TriangleCorner : MaskableGraphic
{
    public float size = 20f;
    public Color triangleColor = Color.white;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Define triangle vertices
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = triangleColor;

        vertex.position = new Vector3(0, size); // top left
        vh.AddVert(vertex);

        vertex.position = new Vector3(size, size); // top right
        vh.AddVert(vertex);

        vertex.position = new Vector3(0, 0); // bottom left
        vh.AddVert(vertex);

        // Add triangle indices
        vh.AddTriangle(0, 1, 2);
    }
}

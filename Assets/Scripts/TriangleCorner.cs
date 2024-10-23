using UnityEngine;
using UnityEngine.UI;

public class TriangleCorner : MaskableGraphic
{
    public float size = 20f;
    public Color triangleColor = Color.white;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        // Definiowanie wierzchołków trójkąta
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = triangleColor;

        vertex.position = new Vector3(0, size); // lewy górny
        vh.AddVert(vertex);

        vertex.position = new Vector3(size, size); // prawy górny
        vh.AddVert(vertex);

        vertex.position = new Vector3(0, 0); // lewy dolny
        vh.AddVert(vertex);

        // Dodanie indeksów trójkąta
        vh.AddTriangle(0, 1, 2);
    }
}

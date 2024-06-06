using UnityEngine;

public class HexBarrierGenerator : MonoBehaviour
{
    public GameObject hexBarrierPrefab; // Prefabrykat heksagonalnej bariery
    public float barrierHeight = 5f; // Wysokość bariery
    public float hexScale = 1f; // Skala heksagonów
    private Terrain terrain; // Odniesienie do terenu

    void Start()
    {
        terrain = FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            GenerateHexBarrier();
        }
        else
        {
            Debug.LogError("Terrain not found in the scene.");
        }
    }

    void GenerateHexBarrier()
    {
        float terrainWidth = terrain.terrainData.size.x;
        float terrainHeight = terrain.terrainData.size.z;
        float hexWidth = 1.732f * hexScale; // Szerokość heksagonu
        float hexHeight = 2f * hexScale; // Wysokość heksagonu

        Transform parentTransform = new GameObject("HexBarrier").transform;
        parentTransform.parent = terrain.transform;

        // Generowanie bariery wokół mapy
        GenerateBarrierRow(0, terrainWidth, hexWidth, parentTransform);
        GenerateBarrierRow(terrainHeight, terrainWidth, hexWidth, parentTransform);
        GenerateBarrierColumn(0, terrainHeight, hexHeight, parentTransform);
        GenerateBarrierColumn(terrainWidth, terrainHeight, hexHeight, parentTransform);
    }

    void GenerateBarrierRow(float zPosition, float terrainWidth, float hexWidth, Transform parentTransform)
    {
        for (float x = 0; x <= terrainWidth; x += hexWidth)
        {
            Vector3 pos = terrain.transform.position + new Vector3(x, 0, zPosition);
            Instantiate(hexBarrierPrefab, pos, Quaternion.identity, parentTransform);
        }
    }

    void GenerateBarrierColumn(float xPosition, float terrainHeight, float hexHeight, Transform parentTransform)
    {
        for (float z = 0; z <= terrainHeight; z += hexHeight)
        {
            Vector3 pos = terrain.transform.position + new Vector3(xPosition, 0, z);
            Instantiate(hexBarrierPrefab, pos, Quaternion.identity, parentTransform);
        }
    }
}

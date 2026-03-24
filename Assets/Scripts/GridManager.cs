using UnityEngine;

public class GridManager : MonoBehaviour
{

public int rows, columns;
public GameObject tilePrefab;

void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                Instantiate(tilePrefab, position,Quaternion.identity, transform);
            }
        }
    }
}

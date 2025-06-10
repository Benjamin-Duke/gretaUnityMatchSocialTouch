using UnityEngine;

public class ChessboardGenerator : MonoBehaviour
{
    [Header("Dimensions du plateau")]
    public int width = 8;
    public int height = 8;

    [Header("Paramètres des cases")]
    public float tileSize = 1f;
    public Material whiteMaterial;
    public Material blackMaterial;

    [Header("Centrage")]
    public Transform centerTarget;

    // Propriétés publiques pour que TetraSnap puisse les utiliser
    public Vector3 BoardStartOffset { get; private set; }
    public Vector3 BoardCenter { get; private set; }

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        // 1. D'abord placer le parent à la bonne position
        if (centerTarget != null)
        {
            this.transform.position = centerTarget.position;
        }

        float totalWidth = width * tileSize;
        float totalHeight = height * tileSize;

        // 2. Calculer l'offset en LOCAL (par rapport au parent)
        Vector3 startOffset = new Vector3(-totalWidth / 2f + tileSize / 2f, 0, -totalHeight / 2f + tileSize / 2f);
        
        // Stocker les informations pour TetraSnap
        BoardStartOffset = startOffset;
        BoardCenter = this.transform.position;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tile.transform.localScale = Vector3.one * tileSize;
                tile.transform.rotation = Quaternion.Euler(90, 0, 0);
                tile.name = $"Tile_{x}_{z}"; // Pour debug

                // 3. Calculer la position LOCAL de la tile (par rapport au parent)
                Vector3 tileLocalPos = startOffset + new Vector3(x * tileSize, 0, z * tileSize);
                
                // 4. Assigner le parent AVANT de définir la position
                tile.transform.parent = this.transform;
                
                // 5. Utiliser localPosition au lieu de position
                tile.transform.localPosition = tileLocalPos;

                bool isWhite = (x + z) % 2 == 0;
                Material chosenMaterial = isWhite ? whiteMaterial : blackMaterial;
                tile.GetComponent<Renderer>().material = chosenMaterial;
            }
        }
    }

    // Méthodes utilitaires pour TetraSnap
    public Vector3 GetGridPosition(int gridX, int gridZ)
    {
        Vector3 localPos = BoardStartOffset + new Vector3(gridX * tileSize, 0, gridZ * tileSize);
        return this.transform.TransformPoint(localPos);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = this.transform.InverseTransformPoint(worldPos);
        Vector3 relativeToStart = localPos - BoardStartOffset;
        
        int gridX = Mathf.RoundToInt(relativeToStart.x / tileSize);
        int gridZ = Mathf.RoundToInt(relativeToStart.z / tileSize);
        
        return new Vector2Int(gridX, gridZ);
    }

    public bool IsValidGridPosition(int gridX, int gridZ)
    {
        return gridX >= 0 && gridX < width && gridZ >= 0 && gridZ < height;
    }

    // Pour debug - afficher la grille dans l'éditeur
    // void OnDrawGizmos()
    // {
    //     if (Application.isPlaying)
    //     {
    //         Gizmos.color = Color.green;
    //         for (int x = 0; x < width; x++)
    //         {
    //             for (int z = 0; z < height; z++)
    //             {
    //                 Vector3 pos = GetGridPosition(x, z);
    //                 pos.y += 0.01f; // Légèrement au-dessus
    //                 Gizmos.DrawWireCube(pos, Vector3.one * tileSize * 0.8f);
    //             }
    //         }
    //     }
    // }
}
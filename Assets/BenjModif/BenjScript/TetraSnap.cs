using UnityEngine;
using System.Collections.Generic;

public class SimpleTetraminoSnap : MonoBehaviour
{
    [Header("Configuration")]
    public float tileSize = 1f; 
    public Transform chessboard; 

    [Header("Visual")]
    public Material previewGoodMaterial; 
    public Material previewBadMaterial;

    private LeapGrabObject grabSystem;
    private List<GameObject> previewCubes = new List<GameObject>();

    void Start()
    {
        grabSystem = FindObjectOfType<LeapGrabObject>();

        if (chessboard == null)
        {
            ChessboardGenerator board = FindObjectOfType<ChessboardGenerator>();
            if (board != null) chessboard = board.transform;
        }
    }

    void Update()
    {
        ClearPreview();

        if (grabSystem != null && grabSystem.GrabbedObject != null)
        {
            GameObject tetramino = grabSystem.GrabbedObject;
            if (tetramino.CompareTag("iii"))
            {
                ShowPreview(tetramino);
            }
        }

        // if (IsBoardFull()) {
        //     Debug.Log("Plateau rempli - plus assez d'espace libre !");
        // }
    }

    void ShowPreview(GameObject tetramino)
    {
        Vector3 snapPos = GetSnapPosition(tetramino.transform.position);

        List<Vector3> blockPositions = GetTetraminoBlockPositions(tetramino, snapPos);

        bool isValid = IsPlacementValid(blockPositions, tetramino);

        Material previewMat = isValid ? previewGoodMaterial : previewBadMaterial;

        foreach (Vector3 pos in blockPositions)
        {
            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.transform.position = pos + Vector3.up * 0.01f; 
            preview.transform.localScale = Vector3.one * tileSize;

            preview.GetComponent<Renderer>().material = previewMat;

            Destroy(preview.GetComponent<Collider>());

            previewCubes.Add(preview);
        }
    }

    public Vector3 GetSnapPosition(Vector3 worldPos)
    {
        if (chessboard == null) return worldPos;

        ChessboardGenerator board = chessboard.GetComponent<ChessboardGenerator>();
        Vector3 localPos = chessboard.InverseTransformPoint(worldPos);

        Vector3 offset = board != null ? board.BoardStartOffset : Vector3.zero;
        Vector3 relative = localPos - offset;

        int gridX = Mathf.RoundToInt(relative.x / tileSize);
        int gridZ = Mathf.RoundToInt(relative.z / tileSize);

        Vector3 snappedLocal = offset + new Vector3(gridX * tileSize, localPos.y, gridZ * tileSize);
        return chessboard.TransformPoint(snappedLocal);
    }

    public List<Vector3> GetTetraminoBlockPositions(GameObject tetramino, Vector3 snapCenter)
    {
        List<Vector3> positions = new List<Vector3>();

        foreach (Transform child in tetramino.transform)
        {
            Vector3 relativePos = child.position - tetramino.transform.position;

            Vector3 snappedRelative = new Vector3(
                Mathf.Round(relativePos.x / tileSize) * tileSize,
                0,
                Mathf.Round(relativePos.z / tileSize) * tileSize
            );

            Vector3 finalPos = snapCenter + snappedRelative;
            finalPos.y = chessboard.position.y;

            positions.Add(finalPos);
        }

        return positions;
    }

    public bool IsPlacementValid(List<Vector3> blockPositions, GameObject excludeTetramino)
    {
        foreach (Vector3 pos in blockPositions)
        {
            if (!IsOnBoard(pos)) return false;

            if (IsPositionOccupied(pos, excludeTetramino)) return false;
        }
        return true;
    }

    bool IsOnBoard(Vector3 worldPos)
    {
        if (chessboard == null) return true;

        ChessboardGenerator board = chessboard.GetComponent<ChessboardGenerator>();
        if (board == null) return true;

        Vector3 localPos = chessboard.InverseTransformPoint(worldPos);

        float halfWidth = (board.width * tileSize) / 2f;
        float halfHeight = (board.height * tileSize) / 2f;

        return localPos.x >= -halfWidth && localPos.x <= halfWidth &&
            localPos.z >= -halfHeight && localPos.z <= halfHeight;
    }

    bool IsPositionOccupied(Vector3 worldPos, GameObject excludeTetramino)
    {
        Collider[] colliders = Physics.OverlapSphere(worldPos, tileSize * 0.4f);

        foreach (Collider col in colliders)
        {
            if (excludeTetramino != null &&
                (col.gameObject == excludeTetramino || col.transform.IsChildOf(excludeTetramino.transform)))
            {
                continue;
            }

            if (col.CompareTag("iii"))
            {
                return true; // Quelque chose d'autre occupe déjà cette position
            }
        }
        return false;
    }

    // VERSION OPTIMISÉE : Vérifier s'il reste assez d'espace contigu avec grille booléenne
    public bool IsBoardFull()
    {
        if (chessboard == null) return false;
        ChessboardGenerator board = chessboard.GetComponent<ChessboardGenerator>();
        if (board == null) return false;

        int largestFreeArea = GetLargestFreeArea(board);

        if (largestFreeArea < 4)
        {
            Debug.Log($"Plus grande zone libre: {largestFreeArea} cases - plateau probablement plein");
            return true;
        }

        Debug.Log($"Plus grande zone libre: {largestFreeArea} cases - plateau pas plein");
        return false;
    }

    private int GetLargestFreeArea(ChessboardGenerator board)
    {
        bool[,] visited = new bool[board.width, board.height];
        bool[,] occupied = GetOccupationGrid(board);

        int maxArea = 0;

        for (int x = 0; x < board.width; x++)
        {
            for (int z = 0; z < board.height; z++)
            {
                if (!visited[x, z] && !occupied[x, z])
                {
                    int area = FloodFillFreeArea(board, x, z, visited, occupied);
                    maxArea = Mathf.Max(maxArea, area);
                }
            }
        }

        return maxArea;
    }

    private int FloodFillFreeArea(ChessboardGenerator board, int x, int z, bool[,] visited, bool[,] occupied)
    {
        if (x < 0 || x >= board.width || z < 0 || z >= board.height) return 0;
        if (visited[x, z]) return 0;
        if (occupied[x, z]) return 0;

        visited[x, z] = true;

        int area = 1;
        area += FloodFillFreeArea(board, x + 1, z, visited, occupied);
        area += FloodFillFreeArea(board, x - 1, z, visited, occupied);
        area += FloodFillFreeArea(board, x, z + 1, visited, occupied);
        area += FloodFillFreeArea(board, x, z - 1, visited, occupied);

        return area;
    }

    // Création d'une grille d'occupation à partir des blocs existants
    private bool[,] GetOccupationGrid(ChessboardGenerator board)
    {
        bool[,] grid = new bool[board.width, board.height];

        GameObject[] allBlocks = GameObject.FindGameObjectsWithTag("iii");

        Vector3 offset = board.BoardStartOffset;

        foreach (GameObject block in allBlocks)
        {
            foreach (Transform child in block.transform)
            {
                Vector3 localPos = chessboard.InverseTransformPoint(child.position);

                int x = Mathf.RoundToInt((localPos.x - offset.x) / tileSize);
                int z = Mathf.RoundToInt((localPos.z - offset.z) / tileSize);

                if (x >= 0 && x < board.width && z >= 0 && z < board.height)
                {
                    grid[x, z] = true;
                }
            }
        }

        return grid;
    }

    void ClearPreview()
    {
        foreach (GameObject preview in previewCubes)
        {
            if (preview != null) DestroyImmediate(preview);
        }
        previewCubes.Clear();
    }

    public void SnapToGrid(GameObject tetramino)
    {
        if (tetramino == null || !tetramino.CompareTag("iii")) return;

        Vector3 snapPos = GetSnapPosition(tetramino.transform.position);

        List<Vector3> newPositions = GetTetraminoBlockPositions(tetramino, snapPos);

        Vector3 totalOffset = Vector3.zero;
        int i = 0;
        foreach (Transform child in tetramino.transform)
        {
            Vector3 currentPos = child.position;
            Vector3 targetPos = newPositions[i++];
            totalOffset += (targetPos - currentPos);
        }

        Vector3 averageOffset = totalOffset / tetramino.transform.childCount;

        tetramino.transform.position += averageOffset;

        Vector3 euler = tetramino.transform.eulerAngles;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        tetramino.transform.eulerAngles = euler;

        i = 0;
        foreach (Transform child in tetramino.transform)
        {
            child.position = newPositions[i++];
        }

        if (IsBoardFull())
        {
            Debug.Log("Plateau rempli - plus assez d'espace libre !");
        }
    }

    void OnDrawGizmos()
    {
        if (chessboard == null) return;
        ChessboardGenerator board = chessboard.GetComponent<ChessboardGenerator>();
        int width = board != null ? board.width : 8;
        int height = board != null ? board.height : 8;

        Gizmos.color = Color.red;
        float halfWidth = (width * tileSize) / 2f;
        float halfHeight = (height * tileSize) / 2f;
        Vector3 origin = chessboard.position;

        for (int x = -width / 2; x <= width / 2; x++)
        {
            Vector3 start = origin + new Vector3(x * tileSize, 0, -halfHeight);
            Vector3 end = origin + new Vector3(x * tileSize, 0, halfHeight);
            Gizmos.DrawLine(start, end);
        }
        for (int z = -height / 2; z <= height / 2; z++)
        {
            Vector3 start = origin + new Vector3(-halfWidth, 0, z * tileSize);
            Vector3 end = origin + new Vector3(halfWidth, 0, z * tileSize);
            Gizmos.DrawLine(start, end);
        }
    }
}
